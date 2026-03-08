using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Concrete implementatie van ApiService voor Trip.
    /// Zoals NationalParkDataService in SafariSnap (Les 3).
    /// </summary>
    public class TripDataService : ApiService<Trip>, ITripDataService
    {
        protected override string EndPoint => "trips";

        /// <summary>
        /// Extra methode om TripStops voor een specifieke Trip op te halen.
        /// </summary>
        public async Task<List<TripStop>> GetTripStopsAsync(int tripId)
        {
            var response = await client.GetAsync($"{BASE_URL}/{EndPoint}/{tripId}/tripstops");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<TripStop>>(jsonData)!;
            }
            throw new Exception($"GetTripStopsAsync request failed with status code {response.StatusCode}");
        }
    }
}
