using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using PlanoraApp.AI.Agents;
using PlanoraApp.AI.Models;
using PlanoraApp.API.Hubs;

namespace PlanoraApp.API.Workers
{
    public class SignalRAgentNotifier : IAgentNotifier
    {
        private readonly IHubContext<AgentHub> _hubContext;

        public SignalRAgentNotifier(IHubContext<AgentHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendStatusAsync(string tripId, string agentName, int progressPct, string message)
        {
            var payload = new { Agent = agentName, Progress = progressPct, Message = message };
            await _hubContext.Clients.Group($"trip_{tripId}").SendAsync("ReceiveAgentUpdate", payload);
            Console.WriteLine($"[SignalR] -> trip_{tripId}: {message}");
        }
    }

    public class AiJobWorker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private ServiceBusClient? _client;
        private ServiceBusProcessor? _processor;

        public AiJobWorker(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connectionString = _configuration["AzureServiceBus:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("PASTE_"))
            {
                Console.WriteLine("[AiJobWorker] Service Bus not configured. Skipping background worker.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try 
                {
                    _client = new ServiceBusClient(connectionString);
                    _processor = _client.CreateProcessor("ai-generation-jobs", new ServiceBusProcessorOptions());

                    _processor.ProcessMessageAsync += MessageHandler;
                    _processor.ProcessErrorAsync += ErrorHandler;

                    await _processor.StartProcessingAsync(stoppingToken);
                    Console.WriteLine("[AiJobWorker] Service Bus connected and processing.");

                    // Stay in this loop while processing
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(5000, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AiJobWorker] Fatal ServiceBus Error: {ex.Message}");
                    Console.WriteLine("Worker will retry in 30 seconds... (API remains available)");
                    
                    // Clean up before retry
                    if (_processor != null) await _processor.DisposeAsync();
                    if (_client != null) await _client.DisposeAsync();
                    
                    try { await Task.Delay(30000, stoppingToken); } catch { break; }
                }
            }
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IOrchestratorAgent>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AgentHub>>();
                var notifier = new SignalRAgentNotifier(hubContext);

                var kernel = scope.ServiceProvider.GetRequiredService<Microsoft.SemanticKernel.Kernel>();
                var researcher = scope.ServiceProvider.GetRequiredService<IResearchAgent>();
                var router = scope.ServiceProvider.GetRequiredService<IRouteOptimiserAgent>();
                var injector = scope.ServiceProvider.GetRequiredService<ITourPackageInjector>();
                var budgeter = scope.ServiceProvider.GetRequiredService<IBudgetAgent>();
                var assembler = scope.ServiceProvider.GetRequiredService<IDayAssemblyAgent>();
                var personaliser = scope.ServiceProvider.GetRequiredService<IPersonalisationAgent>();

                var localOrchestrator = new OrchestratorAgent(kernel, researcher, router, injector, budgeter, assembler, personaliser, notifier);

                var body = args.Message.Body.ToString();
                var request = JsonSerializer.Deserialize<TripGenerationRequest>(body);

                var tripService = scope.ServiceProvider.GetRequiredService<PlanoraApp.API.Services.TripService.ITripService>();

                if (request != null)
                {
                    var generatedJson = await localOrchestrator.ProcessTripRequestAsync(request);
                    await tripService.UpdateTripDataAsync(request.TripId, generatedJson);
                }

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiJobWorker] Message processing explicitly failed: {ex.Message}");
            }
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"[AiJobWorker] Error: {args.Exception.Message}");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync(stoppingToken);
                await _processor.DisposeAsync();
            }
            if (_client != null) await _client.DisposeAsync();
            await base.StopAsync(stoppingToken);
        }
    }
}
