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
            Console.WriteLine($"[ORCHESTRATOR] STARTING TRIP GENERATION");
            Console.WriteLine($"TripID: {request.TripId} | Dest: {request.Destination}");
            Console.WriteLine($"========================================\n");
            
            // Critical Delay: Wait 4 seconds to ensure the React Native client has had time  
            // to connect to the SignalR Hub. Otherwise, rapid generation misses the client!
            await Task.Delay(4000);

            var totalDays = (request.EndDate - request.StartDate).Days + 1;
            if (totalDays <= 0) totalDays = 1;

            await NotifyAsync(request.TripId, "Research", 10, "Researching top attractions and POIs...");
            var rawData = await _researcher.SearchPlacesAsync(request.Destination, "Top Attractions and Hotels");
            
            await NotifyAsync(request.TripId, "Route Optimiser", 30, "Clustering places into daily geographic zones...");
            var clusteredData = await _router.ClusterPlacesIntoDaysAsync(rawData, totalDays);
            
            await NotifyAsync(request.TripId, "Tour Injector", 50, "Integrating official tour packages...");
            var injectedData = await _tourInjector.InjectSponsoredToursAsync(clusteredData, request.Destination);
            
            await NotifyAsync(request.TripId, "Budget Agent", 60, $"Filtering for {request.BudgetTier} tier options...");
            var finalData = await _budgeter.PruneByBudgetAsync(injectedData, request.BudgetTier);
            
            await NotifyAsync(request.TripId, "Day Assembly", 75, "Scheduling day hour-by-hour with meal gaps...");
            var baseItineraryJson = await _assembler.AssembleDaysAsync(finalData, totalDays, request.TravelMode, request.Origin, request.GroupType);

            await NotifyAsync(request.TripId, "Complete", 100, "Trip Generated Successfully!");
            Console.WriteLine($"\n[ORCHESTRATOR] COMPLETE: Returning generated itinerary back to DB...\n");

            return baseItineraryJson;
        }
    }
}
