namespace TripTracker.App.Services
{
    // Interface voor AI foto analyse - zoals SafariSnap (Les 3)
    // Gebruikt OpenAI Vision API om titel en beschrijving te genereren
    public interface IAnalyzeImageService
    {
        // Analyseer reisfoto en genereer titel + beschrijving
        Task<StopAnalysis?> AnalyzePhotoAsync(byte[] imageData);
    }

    // Resultaat van de AI analyse
    public class StopAnalysis
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
