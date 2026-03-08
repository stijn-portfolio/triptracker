namespace TripTracker.App.Services
{
    /// <summary>
    /// GPS Locatie service - ref zoals SafariSnap (Les 3).
    /// LET OP: Permissions moeten op MainThread gevraagd worden!
    /// </summary>
    public class GeolocationService : IGeolocationService
    {
        // ═══════════════════════════════════════════════════════════
        // LOCATION METHODS
        // ═══════════════════════════════════════════════════════════

        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[GPS] Starting location request...");

                // Check/request permission op MainThread
                var status = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("[GPS] Checking permission...");
                    var currentStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    System.Diagnostics.Debug.WriteLine($"[GPS] Current status: {currentStatus}");

                    if (currentStatus != PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("[GPS] Requesting permission...");
                        currentStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                        System.Diagnostics.Debug.WriteLine($"[GPS] After request: {currentStatus}");
                    }

                    return currentStatus;
                });

                System.Diagnostics.Debug.WriteLine($"[GPS] Final permission status: {status}");

                if (status == PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("[GPS] Getting location...");
                    // GeolocationAccuracy.Medium = goed genoeg voor reizen, snel resultaat
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.Default.GetLocationAsync(request);
                    System.Diagnostics.Debug.WriteLine($"[GPS] Got location: {location?.Latitude}, {location?.Longitude}");
                    return location;
                }

                System.Diagnostics.Debug.WriteLine("[GPS] Permission NOT granted");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPS] ERROR: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }
}
