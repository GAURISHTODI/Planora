using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;


namespace PlanoraApp.AI.Agents
{
    public interface IResearchAgent
    {
        Task<string> SearchPlacesAsync(string destination, string query);
    }

    public class ResearchAgent : IResearchAgent
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ResearchAgent(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Google:ApiKey"] ?? string.Empty;
        }

        public async Task<string> SearchPlacesAsync(string destination, string query)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("PASTE_")) 
            {
                Console.WriteLine("[ResearchAgent] Google API Key is missing. Returning mock data.");
                return "[]";
            }

            Console.WriteLine($"[ResearchAgent] Calling Google Places for {query} in {destination}...");
            
            var requestBody = new 
            {
                textQuery = $"{query} in {destination}",
                languageCode = "en"
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:searchText");
            requestMessage.Headers.Add("X-Goog-Api-Key", _apiKey);
            requestMessage.Headers.Add("X-Goog-FieldMask", "places.displayName,places.formattedAddress,places.location,places.types,places.rating,places.userRatingCount,places.websiteUri,places.photos");
            
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ResearchAgent] External API failed: {error}");
                return "[]";
            }

            var content = await response.Content.ReadAsStringAsync();
            
            // Post-process: inject imageUrl into each place using the first photo reference
            content = InjectPhotoUrls(content);
            
            return content;
        }

        /// <summary>
        /// Parses the Google Places API response and constructs real photo media URLs
        /// from each place's photos[].name field so the LLM doesn't have to guess them.
        /// </summary>
        private string InjectPhotoUrls(string rawJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                if (!doc.RootElement.TryGetProperty("places", out var places))
                    return rawJson;

                var enrichedPlaces = new List<Dictionary<string, object>>();

                foreach (var place in places.EnumerateArray())
                {
                    var dict = new Dictionary<string, object>();

                    // Copy all existing properties
                    foreach (var prop in place.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value;
                    }

                    // Construct imageUrl from the first photo's resource name
                    if (place.TryGetProperty("photos", out var photos) && photos.GetArrayLength() > 0)
                    {
                        var firstPhoto = photos[0];
                        if (firstPhoto.TryGetProperty("name", out var photoName))
                        {
                            var photoResourceName = photoName.GetString();
                            if (!string.IsNullOrEmpty(photoResourceName))
                            {
                                var imageUrl = $"https://places.googleapis.com/v1/{photoResourceName}/media?key={_apiKey}&maxWidthPx=800&maxHeightPx=400";
                                dict["imageUrl"] = imageUrl;
                                Console.WriteLine($"[ResearchAgent]   Photo URL injected for place");
                            }
                        }
                    }

                    enrichedPlaces.Add(dict);
                }

                var result = JsonSerializer.Serialize(new { places = enrichedPlaces });
                Console.WriteLine($"[ResearchAgent] Enriched {enrichedPlaces.Count} places with photo URLs");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResearchAgent] Photo URL injection failed (non-critical): {ex.Message}");
                return rawJson; // Graceful fallback: return original data
            }
        }
    }
}
