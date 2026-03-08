using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripTracker.App.Services
{
    /// <summary>
    /// AI Foto Analyse service - gebaseerd op SafariSnap (Les 3).
    /// Gebruikt OpenAI Vision API (GPT-4o) voor het analyseren van reisfoto's.
    /// </summary>
    public class AnalyzeImageService : IAnalyzeImageService
    {
        private readonly OpenAIClient _client;

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public AnalyzeImageService()
        {
            _client = new OpenAIClient(OpenAIKeys.Key);
        }

        // ═══════════════════════════════════════════════════════════
        // AI ANALYSIS
        // ═══════════════════════════════════════════════════════════

        public async Task<StopAnalysis?> AnalyzePhotoAsync(byte[] imageData)
        {
            try
            {
                var chatClient = _client.GetChatClient(OpenAIKeys.LLModel);

                // Prompt in Nederlands (gebruiker keuze)
                // Vraagt om JSON output voor makkelijke parsing
                var prompt =
@"Analyseer deze reisfoto en geef een korte beschrijving.

Retourneer ALLEEN geldige JSON in dit formaat:
{
  ""title"": ""korte titel (max 5 woorden)"",
  ""description"": ""beschrijving in 2-3 zinnen""
}

BELANGRIJK:
- Schrijf in het Nederlands
- Beschrijf wat je ziet (gebouw, landschap, monument, etc.)
- Wees beknopt maar informatief
- Als je de locatie herkent, noem die dan

Voeg GEEN markdown of andere opmaak toe. Alleen JSON!";

                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(prompt),
                        ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageData), "image/jpeg")
                    )
                };

                ChatCompletionOptions options = new();
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

                var outputText = completion.Content[0].Text.Trim();

                System.Diagnostics.Debug.WriteLine($"OpenAI response: {outputText}");

                // Strip markdown code blocks als OpenAI die toch toevoegt
                if (outputText.StartsWith("```"))
                {
                    // Verwijder ```json of ``` aan begin en ``` aan eind
                    var lines = outputText.Split('\n');
                    var jsonLines = lines.Skip(1).TakeWhile(l => !l.StartsWith("```"));
                    outputText = string.Join("\n", jsonLines).Trim();
                    System.Diagnostics.Debug.WriteLine($"OpenAI cleaned: {outputText}");
                }

                // Parse JSON response
                var result = JsonSerializer.Deserialize<AnalysisResponse>(outputText);

                if (result != null)
                {
                    return new StopAnalysis
                    {
                        Title = result.Title ?? "Nieuwe stop",
                        Description = result.Description ?? ""
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI Analysis error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RESPONSE CLASSES (JSON)
        // ═══════════════════════════════════════════════════════════

        private class AnalysisResponse
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }
        }
    }
}
