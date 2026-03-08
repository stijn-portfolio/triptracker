namespace TripTracker.App.Services
{
    // Interface voor foto capture/selectie - zoals SafariSnap (Les 3)
    // Gebruikt MAUI MediaPicker voor camera en galerij
    public interface IPhotoService
    {
        // Maak foto met camera, retourneer bytes (of null bij cancel)
        Task<byte[]?> CapturePhotoAsync();

        // Kies foto uit galerij, retourneer bytes (of null bij cancel)
        Task<byte[]?> PickPhotoAsync();
    }
}
