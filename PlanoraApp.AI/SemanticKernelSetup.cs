using Microsoft.SemanticKernel;

namespace PlanoraApp.AI
{
    public static class SemanticKernelSetup
    {
        public const string DefaultGeminiModel = "gemini-2.5-flash";

        public static readonly string[] PreferredGeminiModels =
        {
            "gemini-2.5-flash",
            "gemini-2.5-flash-lite"
        };

        public static Kernel BuildKernel(string geminiApiKey, string geminiModel = DefaultGeminiModel)
        {
            var builder = Kernel.CreateBuilder();

            // Gemini natively supports the OpenAI REST interface!
            // We use the ultra-stable OpenAI SDK pointed at Google's URL to bypass the Google Alpha SDK 404 bugs.
            var httpClient = new HttpClient { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/") };
            
            builder.AddOpenAIChatCompletion(
                modelId: geminiModel,
                apiKey: geminiApiKey,
                httpClient: httpClient
            );

            return builder.Build();
        }
    }
}
