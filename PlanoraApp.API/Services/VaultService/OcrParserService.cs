using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using System.Text.Json;

namespace PlanoraApp.API.Services.VaultService
{
    public interface IOcrParserService
    {
        Task<string> ParseTravelDocumentAsync(string blobUrl);
    }

    public class OcrParserService : IOcrParserService
    {
        private readonly DocumentAnalysisClient? _client;

        public OcrParserService(IConfiguration config)
        {
            var endpoint = config["DocIntelligence:Endpoint"];
            var key = config["DocIntelligence:ApiKey"];

            if (!string.IsNullOrEmpty(endpoint) && !endpoint.Contains("PASTE_HERE") &&
                !string.IsNullOrEmpty(key) && !key.Contains("PASTE_HERE"))
            {
                _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
            }
        }

        public async Task<string> ParseTravelDocumentAsync(string blobUrl)
        {
            if (_client == null)
            {
                // OCR falls back gracefully to a basic parsing payload if keys omit
                return JsonSerializer.Serialize(new { Title = "Document Upload", TravelDate = (string?)null, Type = "Unknown" });
            }

            try
            {
                var uri = new Uri(blobUrl);
                // "prebuilt-document" pulls out generic forms (useful for tickets and random receipts)
                // In production, "prebuilt-receipt" or custom models are also viable.
                AnalyzeDocumentOperation operation = await _client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, "prebuilt-document", uri);
                AnalyzeResult result = operation.Value;

                // For MVP, extract high-level key-values and concat the text
                var extractedFields = new Dictionary<string, string>();
                foreach (var kvp in result.KeyValuePairs)
                {
                    if (kvp.Key != null && kvp.Value != null)
                    {
                        extractedFields[kvp.Key.Content] = kvp.Value.Content;
                    }
                }

                // Grab a rough date approximation
                string? roughDate = null;
                var dateKeys = extractedFields.Keys.Where(k => k.ToLower().Contains("date")).ToList();
                if (dateKeys.Any())
                {
                    roughDate = extractedFields[dateKeys.First()];
                }

                return JsonSerializer.Serialize(new 
                { 
                    Title = "Parsed Travel Document",
                    TravelDate = roughDate,
                    Type = "Ticket/Receipt",
                    RawData = extractedFields
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OCR Error] {ex.Message}");
                return JsonSerializer.Serialize(new { Title = "Unparsed Document", Error = ex.Message });
            }
        }
    }
}
