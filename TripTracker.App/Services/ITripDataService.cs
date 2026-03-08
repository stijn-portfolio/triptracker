using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Interface voor Trip-specifieke API operaties.
    /// Breidt IApiService uit met extra methode voor TripStops.
    /// </summary>
    public interface ITripDataService : IApiService<Trip>
    {
        /// <summary>
        /// Haalt alle TripStops op voor een specifieke Trip.
        /// </summary>
        Task<List<TripStop>> GetTripStopsAsync(int tripId);
    }
}
