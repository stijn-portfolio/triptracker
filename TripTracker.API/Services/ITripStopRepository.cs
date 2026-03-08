using TripTracker.API.Entities;

namespace TripTracker.API.Services
{
    public interface ITripStopRepository
    {
        Task<IEnumerable<TripStop>> GetTripStopsAsync(int tripId);
        Task<IEnumerable<TripStop>> GetAllTripStopsAsync();
        Task<TripStop> GetTripStopAsync(int id);
        void AddTripStop(TripStop tripStop);
        void DeleteTripStop(TripStop tripStop);
        void UpdateTripStop(TripStop tripStop);
        Task<bool> SaveChangesAsync();
    }
}
