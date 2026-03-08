using TripTracker.API.Entities;

namespace TripTracker.API.Services
{
    public interface ITripRepository
    {
        Task<IEnumerable<Trip>> GetTripsAsync();
        Task<Trip> GetTripAsync(int id);
        void AddTrip(Trip trip);           // SYNC - alleen track!
        void DeleteTrip(Trip trip);        // SYNC - alleen track!
        void UpdateTrip(Trip trip);        // SYNC - alleen track!
        Task<bool> SaveChangesAsync();     // ASYNC - persists to DB
    }
}
