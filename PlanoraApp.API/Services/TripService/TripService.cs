using MongoDB.Driver;
using PlanoraApp.API.Infrastructure;
using PlanoraApp.API.Services.TripService.Models;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using PlanoraApp.AI.Models;

namespace PlanoraApp.API.Services.TripService
{
    public interface ITripService
    {
        Task<List<TripDocument>> GetUserTripsAsync(string userId);
        Task<TripDocument> CreateTripAsync(TripDocument trip);
        Task<TripDocument?> GetTripByIdAsync(string id);
        Task UpdateTripDataAsync(string id, string generatedJson);
    }

    public class TripService : ITripService
    {
        private readonly IMongoCollection<TripDocument> _trips;
        private readonly ServiceBusClient? _serviceBusClient;
        
        // Static in-memory fallback for aggressive corporate/school network firewalls blocking MongoDB port 27017
        private static readonly List<TripDocument> _inMemoryFallbackDb = new();

        public TripService(CosmosDbClient cosmosDbClient, IServiceProvider serviceProvider)
        {
            _trips = cosmosDbClient.GetCollection<TripDocument>("trips");
            _serviceBusClient = serviceProvider.GetService<ServiceBusClient>();
        }

        public async Task<List<TripDocument>> GetUserTripsAsync(string userId)
        {
            try 
            {
                var dbTrips = await _trips.Find(t => t.UserId == userId).ToListAsync();
                return dbTrips.Concat(_inMemoryFallbackDb.Where(t => t.UserId == userId && dbTrips.All(db => db.Id != t.Id))).ToList();
            } 
            catch 
            {
                return _inMemoryFallbackDb.Where(t => t.UserId == userId).ToList();
            }
        }

        public async Task<TripDocument> CreateTripAsync(TripDocument trip)
        {
            trip.Id ??= MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            trip.CreatedAt = DateTime.UtcNow;
            trip.Status = "pending";

            try
            {
                await _trips.InsertOneAsync(trip);
                Console.WriteLine($"[TripService] Trip {trip.Id} saved to MongoDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TripService] MongoDB insert failed (using fast local memory fallback): {ex.Message}");
                _inMemoryFallbackDb.Add(trip);
            }

            
            if (_serviceBusClient != null)
            {
                try
                {
                    var sender = _serviceBusClient.CreateSender("ai-generation-jobs");
                    var request = new TripGenerationRequest
                    {
                        TripId = trip.Id!,
                        Destination = trip.Destination?.City ?? "",
                        StartDate = trip.StartDate,
                        EndDate = trip.EndDate,
                        BudgetTier = trip.BudgetTier ?? "mid",
                        GroupType = trip.GroupType ?? "solo",
                        Themes = trip.Themes ?? new List<string>(),
                        TravelMode = trip.TravelMode,
                        Origin = trip.Origin?.City
                    };
                    
                    var message = new ServiceBusMessage(JsonSerializer.Serialize(request));
                    await sender.SendMessageAsync(message);
                    Console.WriteLine($"[TripService] Queued AI job for trip {trip.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TripService] Service Bus send failed (trip still saved): {ex.Message}");
                }
            }

            return trip;
        }

        public async Task<TripDocument?> GetTripByIdAsync(string id)
        {
            try 
            {
                var dbTrip = await _trips.Find(t => t.Id == id).FirstOrDefaultAsync();
                return dbTrip ?? _inMemoryFallbackDb.FirstOrDefault(t => t.Id == id);
            }
            catch
            {
                return _inMemoryFallbackDb.FirstOrDefault(t => t.Id == id);
            }
        }

        public async Task UpdateTripDataAsync(string id, string generatedJson)
        {
            Console.WriteLine($"[TripService] UpdateTripDataAsync called for trip {id}");
            Console.WriteLine($"[TripService] GeneratedJson length: {generatedJson?.Length ?? 0}");
            Console.WriteLine($"[TripService] GeneratedJson preview: {generatedJson?.Substring(0, Math.Min(200, generatedJson?.Length ?? 0))}");
            
            // Validate JSON before saving
            if (string.IsNullOrWhiteSpace(generatedJson))
            {
                Console.WriteLine($"[TripService] ERROR: Generated JSON is null or empty");
                return;
            }
            
            try
            {
                // Try to parse the JSON to ensure it's valid
                var parsed = JsonSerializer.Deserialize<object>(generatedJson);
                if (parsed == null)
                {
                    Console.WriteLine($"[TripService] ERROR: Generated JSON is invalid (parsed to null)");
                    return;
                }
                
                var update = Builders<TripDocument>.Update
                    .Set(t => t.Status, "completed")
                    .Set(t => t.GeneratedItinerary, generatedJson);

                await _trips.UpdateOneAsync(t => t.Id == id, update);
                Console.WriteLine($"[TripService] Updated trip {id} in MongoDB.");
            }
            catch
            {
                // Update in-memory if DB fails
                Console.WriteLine($"[TripService] MongoDB update failed, checking in-memory. Total in-memory trips: {_inMemoryFallbackDb.Count}");
                foreach (var t in _inMemoryFallbackDb)
                {
                    Console.WriteLine($"[TripService]   In-memory trip: Id={t.Id}, Status={t.Status}");
                }
                
                var trip = _inMemoryFallbackDb.FirstOrDefault(t => t.Id == id);
                if (trip != null)
                {
                    trip.Status = "completed";
                    trip.GeneratedItinerary = generatedJson;
                    Console.WriteLine($"[TripService] Updated trip {id} in-memory successfully.");
                }
                else
                {
                    Console.WriteLine($"[TripService] WARNING: Trip {id} NOT FOUND in in-memory DB!");
                }
            }
        }
    }
}
