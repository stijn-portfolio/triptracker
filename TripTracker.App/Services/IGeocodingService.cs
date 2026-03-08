namespace TripTracker.App.Services
{
    // Interface voor reverse geocoding (coordinaten → adres)
    public interface IGeocodingService
    {
        // Zet lat/lng om naar adres informatie
        Task<Placemark?> ReverseGeocodeAsync(double latitude, double longitude);
    }
}
