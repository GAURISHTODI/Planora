using Microsoft.SemanticKernel;
using PlanoraApp.AI.Models;

namespace PlanoraApp.AI.Agents
{
    public interface IAgentNotifier
    {
        Task SendStatusAsync(string tripId, string agentName, int progressPct, string message);
    }

    public interface IOrchestratorAgent
    {
        Task<string> ProcessTripRequestAsync(TripGenerationRequest request);
    }

    public class OrchestratorAgent : IOrchestratorAgent
    {
        private readonly Kernel _kernel;
        private readonly IResearchAgent _researcher;
        private readonly IRouteOptimiserAgent _router;
        private readonly ITourPackageInjector _tourInjector;
        private readonly IBudgetAgent _budgeter;
        private readonly IDayAssemblyAgent _assembler;
        private readonly IPersonalisationAgent _personaliser;
        private readonly IAgentNotifier? _notifier;

        public OrchestratorAgent(
            Kernel kernel,
            IResearchAgent researcher,
            IRouteOptimiserAgent router,
            ITourPackageInjector tourInjector,
            IBudgetAgent budgeter,
            IDayAssemblyAgent assembler,
            IPersonalisationAgent personaliser,
            IAgentNotifier? notifier = null)
        {
            _kernel = kernel;
            _researcher = researcher;
            _router = router;
            _tourInjector = tourInjector;
            _budgeter = budgeter;
            _assembler = assembler;
            _personaliser = personaliser;
            _notifier = notifier;
        }

        private async Task NotifyAsync(string tripId, string task, int pct, string msg)
        {
            if (_notifier != null) await _notifier.SendStatusAsync(tripId, task, pct, msg);
        }

        public async Task<string> ProcessTripRequestAsync(TripGenerationRequest request)
        {
            Console.WriteLine($"\n========================================");
            Console.WriteLine($"[ORCHESTRATOR] STARTING LLM-FIRST TRIP GENERATION");
            Console.WriteLine($"TripID: {request.TripId} | Dest: {request.Destination}");
            Console.WriteLine($"========================================\n");
            
            // Critical Delay: Wait 4 seconds to ensure the React Native client has had time  
            // to connect to the SignalR Hub. Otherwise, rapid generation misses the client!
            await Task.Delay(4000);

            var totalDays = (request.EndDate - request.StartDate).Days + 1;
            if (totalDays <= 0) totalDays = 1;

            var destName = string.IsNullOrEmpty(request.Destination) ? "the destination" : request.Destination;

            await NotifyAsync(request.TripId, "AI Brainstorming", 20, $"Designing optimal {totalDays}-day itinerary for {destName}...");
            
            // 1. Single-Shot LLM Generation
            var baseItineraryJson = await _assembler.AssembleDaysAsync(request, totalDays);
            
            await NotifyAsync(request.TripId, "Mapping & Images", 75, "Fetching real coordinates and high-res photos...");

            // 2. Hydrate exact chosen places via Google Places
            var hydratedJson = await _researcher.HydrateItineraryAsync(baseItineraryJson, destName);

            await NotifyAsync(request.TripId, "Complete", 100, "Trip Generated Successfully!");
            Console.WriteLine($"\n[ORCHESTRATOR] COMPLETE: Returning generated itinerary back to DB...\n");

            return hydratedJson;
        }
    }
}
