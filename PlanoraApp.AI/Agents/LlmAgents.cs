using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PlanoraApp.AI.Agents
{
    public interface IDayAssemblyAgent
    {
        Task<string> AssembleDaysAsync(PlanoraApp.AI.Models.TripGenerationRequest request, int totalDays);
    }

    public class DayAssemblyAgent : IDayAssemblyAgent
    {
        private readonly string _apiKey;
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(180) };

        public DayAssemblyAgent(string geminiApiKey)
        {
            _apiKey = geminiApiKey;
        }

        public async Task<string> AssembleDaysAsync(PlanoraApp.AI.Models.TripGenerationRequest request, int totalDays)
        {
            Console.WriteLine("[DayAssembly Agent] Calling Gemini REST API to write structured JSON itinerary...");

            var dest = string.IsNullOrEmpty(request.Destination) ? "the destination" : request.Destination;
            var budget = string.IsNullOrEmpty(request.BudgetTier) ? "mid" : request.BudgetTier;
            var themes = (request.Themes != null && request.Themes.Any()) ? string.Join(", ", request.Themes) : "general sightseeing";
            var groupType = string.IsNullOrEmpty(request.GroupType) ? "general" : request.GroupType;
            var paxCount = request.PaxCount;

            // Build intelligent prompt based on travel mode
            var travelModeInstruction = "";
            if (request.TravelMode == "flightIncluded")
            {
                var origin = string.IsNullOrEmpty(request.Origin) ? "their city" : request.Origin;
                travelModeInstruction = $@"
TRAVEL MODE: Flight Included
- Day 1 is a TRAVEL DAY with arrival. Assume user lands in morning and needs hotel check-in.
- Day {totalDays} is a DEPARTURE DAY. Schedule light activities near airport/railway station.
- Adjust first/last day schedules accordingly - no heavy activities on arrival/departure days.
- User flies from {origin} - consider jet lag for long flights.";
            }
            else if (request.TravelMode == "travelOnly")
            {
                travelModeInstruction = $@"
TRAVEL MODE: Travel Only
- All {totalDays} days are for exploration and activities.
- No travel days to account for - full itinerary each day.
- User arranges their own transportation separately.";
            }

            var prompt = $@"You are an expert travel planner with deep knowledge of logistics, geography, and traveler psychology. Create an OPTIMAL itinerary for {totalDays} days targeting {dest}.

TRIP PROFILE:
- Destination: {dest}
- Budget: {budget} tier
- Traveler Profile: {groupType} (Total {paxCount} travelers)
- Core Themes/Interests: {themes}

{travelModeInstruction}

INTELLIGENT PLANNING RULES:
1. NO DUPLICATES: NEVER repeat a location. Ensure completely unique locations logic.
2. GEOGRAPHICAL CLUSTERING: Group activities by proximity to minimize travel time. Never criss-cross the city.
3. BUDGET LOGIC: For {budget} tier, recommend appropriate restaurants and avoid places completely misaligned with this budget. 
4. TIMING LOGIC: Include 15-45 mins between locations based on city traffic. Detail specific hour-by-hour timeblocks.

REQUIRED TONE AND PERSONALISATION:
- Tone must be highly tailored for a '{groupType}' traveler profile. Write appealing, immersive descriptions.

BOOKING TYPE CLASSIFICATION:
- BOOKING_TYPE_A ('A'): Flights, hotels, cabs, generic activities, dining - use aggregators
- BOOKING_TYPE_B ('B'): Monuments, museums, shows with official ticketing - use direct website
- BOOKING_TYPE_C ('C'): Free parks, public spaces, gate-only entries - no booking needed

IMAGE HANDLING:
- Output ""imageUrl"": null for ALL activities. This will be automatically populated later by our backend.

Return ONLY valid JSON following this exact schema:
{{
  ""itinerary"": [
    {{
      ""day"": 1,
      ""date"": ""Day 1"",
      ""activities"": [
        {{
          ""time"": ""09:00 AM"",
          ""title"": ""Activity/Place Name"",
          ""description"": ""Why this activity at this time + what to expect"",
          ""location"": ""Use the exact recognizable Google Maps location name (e.g. 'Eiffel Tower, Paris')"",
          ""latitude"": 0.0,
          ""longitude"": 0.0,
          ""imageUrl"": null,
          ""booking_type"": ""A|B|C"",
          ""booking_url"": null,
          ""official_website"": null,
          ""requires_advance_booking"": true/false,
          ""is_free"": false,
          ""entry_fee_approx"": ""$18 or Free"",
          ""opening_hours"": ""09:00-19:00"",
          ""booking_notes"": ""Book 2 weeks in advance or null""
        }}
      ]
    }}
  ]
}}";

            try
            {
                HttpResponseMessage? response = null;
                string? responseBody = null;

                // Try models with exponential backoff for rate limits
                var models = SemanticKernelSetup.PreferredGeminiModels;
                int retryCount = 0;
                const int maxRetries = 3;

                // Retry logic with model failover and exponential backoff
                foreach (var model in models)
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
                    Console.WriteLine($"[DayAssembly Agent] Trying model: {model}");

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.5,
                            maxOutputTokens = 65536,
                            responseMimeType = "application/json"
                        }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Exponential backoff retry for same model on 429
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        response = await _http.PostAsync(url, content);
                        responseBody = await response.Content.ReadAsStringAsync();

                        Console.WriteLine($"[DayAssembly Agent] {model} HTTP Status: {response.StatusCode} (attempt {attempt + 1}/{maxRetries})");

                        if (response.IsSuccessStatusCode)
                        {
                            retryCount = 0; // Reset retry count on success
                            break;
                        }

                        // If 429 (quota) or 503 (unavailable), use exponential backoff
                        if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                        {
                            var delay = (int)Math.Pow(2, attempt) * 1000; // 1s, 2s, 4s
                            Console.WriteLine($"[DayAssembly Agent] Rate limited ({response.StatusCode}), waiting {delay}ms before retry...");
                            await Task.Delay(delay);
                            continue;
                        }
                        else
                        {
                            break; // Non-retryable error (400, 403, 404, etc.)
                        }
                    }

                    if (response!.IsSuccessStatusCode) break;

                    // If all retries failed for this model, try next model
                    if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                    {
                        Console.WriteLine($"[DayAssembly Agent] {model} still unavailable after retries, trying next model...");
                        await Task.Delay(2000); // Extra delay before trying next model
                        continue;
                    }
                    else
                    {
                        break; // Non-retryable error
                    }
                }

                if (!response!.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DayAssembly Agent] Gemini Error Response:\n{responseBody}");
                    return BuildFallbackItinerary(totalDays, $"Gemini returned {response.StatusCode}");
                }

                // Parse the Gemini response to extract the text
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var textResult = parts[0].GetProperty("text").GetString() ?? "{}";
                        // Strip markdown code fences if Gemini wrapped the JSON
                        textResult = CleanJsonResponse(textResult);
                        
                        // Validate that the JSON is complete and not truncated
                        if (!IsValidJson(textResult))
                        {
                            Console.WriteLine($"[DayAssembly Agent] WARNING: Generated JSON appears incomplete/truncated");
                            return BuildFallbackItinerary(totalDays, "Generated JSON was incomplete");
                        }
                        
                        Console.WriteLine($"[DayAssembly Agent] SUCCESS! Got {textResult.Length} chars from Gemini.");
                        return textResult;
                    }
                }

                Console.WriteLine($"[DayAssembly Agent] Could not parse Gemini response structure.");
                return BuildFallbackItinerary(totalDays, "Unexpected Gemini response format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[DayAssembly Agent] SEVERE Error:\n{ex}\n");
                return BuildFallbackItinerary(totalDays, ex.Message);
            }
        }

        private static string CleanJsonResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "{}";
            raw = raw.Trim();
            // Strip ```json ... ``` or ``` ... ```
            if (raw.StartsWith("```"))
            {
                var firstNewline = raw.IndexOf('\n');
                if (firstNewline > 0) raw = raw.Substring(firstNewline + 1);
            }
            if (raw.EndsWith("```"))
            {
                var lastBacktick = raw.LastIndexOf("```");
                if (lastBacktick >= 0) raw = raw.Substring(0, lastBacktick);
            }
            return raw.Trim();
        }

        private static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            
            // Basic validation: check for balanced braces and brackets
            int braceCount = 0;
            int bracketCount = 0;
            bool inString = false;
            bool escapeNext = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }
                
                if (c == '\\')
                {
                    escapeNext = true;
                    continue;
                }
                
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                
                if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                }
            }
            
            // JSON is valid if all braces and brackets are balanced
            return braceCount == 0 && bracketCount == 0;
        }

        private string BuildFallbackItinerary(int totalDays, string reason)
        {
            var days = new List<object>();
            for (int d = 1; d <= totalDays; d++)
            {
                days.Add(new
                {
                    day = d,
                    date = $"Day {d}",
                    activities = new[]
                    {
                        new
                        {
                            time = "09:00 AM",
                            title = "Explore the City",
                            description = $"AI generation encountered an issue ({reason}). Please regenerate this trip.",
                            location = "City Center"
                        }
                    }
                });
            }
            return JsonSerializer.Serialize(new { itinerary = days });
        }
    }

    public interface IPersonalisationAgent
    {
        Task<string> RerankForGroupAsync(string jsonItinerary, string groupType);
    }

    public class PersonalisationAgent : IPersonalisationAgent
    {
        private readonly string _apiKey;
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(180) };

        public PersonalisationAgent(string geminiApiKey)
        {
            _apiKey = geminiApiKey;
        }

        public async Task<string> RerankForGroupAsync(string jsonItinerary, string groupType)
        {
            Console.WriteLine($"[Personalisation Agent] Rewriting tone for {groupType} travelers via Gemini REST...");

            var prompt = $@"You are a personalisation AI. Take this JSON itinerary and rewrite the descriptions to sound appealing specifically to {groupType} travelers. Keep the JSON structure perfectly intact. Return ONLY valid JSON.

Itinerary: {jsonItinerary}";

            try
            {
                // Try models with exponential backoff for rate limits
                var models = SemanticKernelSetup.PreferredGeminiModels;
                HttpResponseMessage? response = null;
                string? responseBody = null;
                const int maxRetries = 3;

                foreach (var model in models)
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            maxOutputTokens = 65536,
                            responseMimeType = "application/json"
                        }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Exponential backoff retry for same model on 429
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        response = await _http.PostAsync(url, content);
                        responseBody = await response.Content.ReadAsStringAsync();

                        Console.WriteLine($"[Personalisation Agent] {model} HTTP Status: {response.StatusCode} (attempt {attempt + 1}/{maxRetries})");

                        if (response.IsSuccessStatusCode) break;

                        if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                        {
                            var delay = (int)Math.Pow(2, attempt) * 1000; // 1s, 2s, 4s
                            Console.WriteLine($"[Personalisation Agent] Rate limited ({response.StatusCode}), waiting {delay}ms before retry...");
                            await Task.Delay(delay);
                            continue;
                        }
                        else break;
                    }

                    if (response!.IsSuccessStatusCode) break;

                    if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                    {
                        Console.WriteLine($"[Personalisation Agent] {model} still unavailable after retries, trying next model...");
                        await Task.Delay(2000);
                        continue;
                    }
                    else break;
                }

                if (!response!.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Personalisation Agent] Gemini error: {response.StatusCode}");
                    return jsonItinerary; // Return original if personalisation fails
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var textResult = parts[0].GetProperty("text").GetString() ?? jsonItinerary;
                        textResult = CleanJsonResponse(textResult);
                        Console.WriteLine($"[Personalisation Agent] SUCCESS! Personalised {textResult.Length} chars.");
                        return textResult;
                    }
                }

                return jsonItinerary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Personalisation Agent] Error: {ex.Message}");
                return jsonItinerary; // Graceful fallback
            }
        }

        private static string CleanJsonResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "{}";
            raw = raw.Trim();
            if (raw.StartsWith("```"))
            {
                var firstNewline = raw.IndexOf('\n');
                if (firstNewline > 0) raw = raw.Substring(firstNewline + 1);
            }
            if (raw.EndsWith("```"))
            {
                raw = raw.Substring(0, raw.Length - 3);
            }
            return raw.Trim();
        }
    }
}
