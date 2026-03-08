using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Reverse Geocoding service - zet GPS coordinaten om naar adres.
    /// Gebruikt MAUI Geocoding API met fallback naar OpenStreetMap Nominatim.
    /// (Windows vereist Bing Maps token, daarom fallback)
    /// </summary>
    public class GeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public GeocodingService()
        {
            _httpClient = new HttpClient();
            // Nominatim vereist een User-Agent header
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TripTracker/1.0");
        }

        // ═══════════════════════════════════════════════════════════
        // GEOCODING METHODS
        // ═══════════════════════════════════════════════════════════

        public async Task<Placemark?> ReverseGeocodeAsync(double latitude, double longitude)
        {
            // Probeer eerst MAUI native geocoding
            var result = await TryNativeGeocodingAsync(latitude, longitude);

            if (result != null)
                return result;

            // Fallback naar OpenStreetMap Nominatim (gratis, geen API key nodig)
            return await TryNominatimGeocodingAsync(latitude, longitude);
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private async Task<Placemark?> TryNativeGeocodingAsync(double latitude, double longitude)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Trying native geocoding for: {latitude}, {longitude}");

                var location = new Location(latitude, longitude);
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
                var placemark = placemarks?.FirstOrDefault();

                if (placemark != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Geocoding] Native success: {placemark.Locality}, {placemark.CountryName}");
                    return placemark;
                }

                return null;
            }
            catch (Exception ex)
            {
                // Op Windows faalt dit zonder Bing Maps token - dat is OK, we gebruiken fallback
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Native failed: {ex.Message}");
                return null;
            }
        }

        private async Task<Placemark?> TryNominatimGeocodingAsync(double latitude, double longitude)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Trying Nominatim fallback for: {latitude}, {longitude}");

                // OpenStreetMap Nominatim API (gratis, geen key nodig)
                // Gebruik punt als decimaalscheidingsteken (niet komma!)
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                var response = await _httpClient.GetFromJsonAsync<NominatimResponse>(url);

                if (response?.Address != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Geocoding] Nominatim success: {response.Address.City ?? response.Address.Town ?? response.Address.Village}, {response.Address.Country}");

                    // Converteer Nominatim response naar MAUI Placemark
                    return new Placemark
                    {
                        CountryName = response.Address.Country,
                        CountryCode = response.Address.CountryCode?.ToUpper(),
                        AdminArea = response.Address.State,
                        SubAdminArea = response.Address.County,
                        Locality = response.Address.City ?? response.Address.Town ?? response.Address.Village,
                        SubLocality = response.Address.Suburb,
                        Thoroughfare = response.Address.Road,
                        SubThoroughfare = response.Address.HouseNumber,
                        PostalCode = response.Address.Postcode
                    };
                }

                System.Diagnostics.Debug.WriteLine("[Geocoding] Nominatim returned no address");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Nominatim failed: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RESPONSE CLASSES (Nominatim API)
        // ═══════════════════════════════════════════════════════════

        private class NominatimResponse
        {
            [JsonPropertyName("address")]
            public NominatimAddress? Address { get; set; }
        }

        private class NominatimAddress
        {
            [JsonPropertyName("house_number")]
            public string? HouseNumber { get; set; }

            [JsonPropertyName("road")]
            public string? Road { get; set; }

            [JsonPropertyName("suburb")]
            public string? Suburb { get; set; }

            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("town")]
            public string? Town { get; set; }

            [JsonPropertyName("village")]
            public string? Village { get; set; }

            [JsonPropertyName("county")]
            public string? County { get; set; }

            [JsonPropertyName("state")]
            public string? State { get; set; }

            [JsonPropertyName("postcode")]
            public string? Postcode { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }

            [JsonPropertyName("country_code")]
            public string? CountryCode { get; set; }
        }
    }
}
