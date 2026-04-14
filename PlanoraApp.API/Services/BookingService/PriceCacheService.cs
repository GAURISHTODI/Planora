using StackExchange.Redis;
using System.Text.Json;

namespace PlanoraApp.API.Services.BookingService
{
    public interface IPriceCacheService
    {
        Task SetIndicativePricesAsync(string tripId, string category, object data);
        Task<T?> GetIndicativePricesAsync<T>(string tripId, string category);
    }

    public class PriceCacheService : IPriceCacheService
    {
        private readonly IConnectionMultiplexer? _redis;

        public PriceCacheService(IConfiguration configuration)
        {
            var configStr = configuration["AzureRedis:ConnectionString"];
            if (!string.IsNullOrEmpty(configStr) && !configStr.Contains("PASTE_"))
            {
                _redis = ConnectionMultiplexer.Connect(configStr);
            }
        }

        public async Task SetIndicativePricesAsync(string tripId, string category, object data)
        {
            if (_redis == null) return;
            var db = _redis.GetDatabase();
            var serialized = JsonSerializer.Serialize(data);
            
            // Requirements: 15-minute TTL per PROJECT_BIBLE
            await db.StringSetAsync($"booking:{category}:{tripId}", serialized, TimeSpan.FromMinutes(15));
        }

        public async Task<T?> GetIndicativePricesAsync<T>(string tripId, string category)
        {
            if (_redis == null) return default;
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync($"booking:{category}:{tripId}");
            
            if (value.IsNullOrEmpty) return default;
            
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
    }
}
