namespace TripTracker.App.Services
{
    // Interface voor GPS locatie - zoals SafariSnap (Les 3)
    public interface IGeolocationService
    {
        // Haal huidige GPS locatie op (of null bij fout/geen permissie)
        Task<Location?> GetCurrentLocationAsync();
    }
}
