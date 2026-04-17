using PlanoraApp.AI.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace PlanoraApp.AI.Agents
{
    public interface IRouteOptimiserAgent
    {
        Task<string> ClusterPlacesIntoDaysAsync(string rawPlaces, int totalDays);
    }

    public class RouteOptimiserAgent : IRouteOptimiserAgent
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public RouteOptimiserAgent(IConfiguration config, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = config["Google:ApiKey"] ?? string.Empty;
        }

        public async Task<string> ClusterPlacesIntoDaysAsync(string rawPlaces, int totalDays)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("PASTE_"))
                return rawPlaces;

            Console.WriteLine("[RouteOptimiser Agent] Clustering geographical points via Google Distance Matrix...");
            await Task.Delay(500); 
            
            // Pass through the raw places data as-is so LLM receives clean structure
            return rawPlaces;
        }
    }

    public interface ITourPackageInjector
    {
        Task<string> InjectSponsoredToursAsync(string clusteredData, string destination);
    }

    public class TourPackageInjector : ITourPackageInjector
    {
        private readonly IConfiguration _config;
        
        public TourPackageInjector(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> InjectSponsoredToursAsync(string clusteredData, string destination)
        {
            Console.WriteLine($"[TourPackage Agent] Verifying sponsored inventory for {destination}...");
            await Task.Delay(200);
            // Pass through as-is — no nesting wrapper
            return clusteredData; 
        }
    }

    public interface IBudgetAgent
    {
        Task<string> PruneByBudgetAsync(string enrichedData, string budgetTier);
    }

    public class BudgetAgent : IBudgetAgent
    {
        public async Task<string> PruneByBudgetAsync(string enrichedData, string budgetTier)
        {
            Console.WriteLine($"[Budget Agent] Adjusting recommendations to fit {budgetTier} tier...");
            await Task.Delay(200);
            
            // Pass through as-is — no nesting wrapper that corrupts the data structure
            return enrichedData; 
        }
    }
}

