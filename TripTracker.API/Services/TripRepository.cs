using Microsoft.EntityFrameworkCore;
using TripTracker.API.DbContexts;
using TripTracker.API.Entities;

namespace TripTracker.API.Services
{
    public class TripRepository : ITripRepository
    {
        private readonly TripTrackerContext _context;

        public TripRepository(TripTrackerContext context)
        {
            _context = context;
        }

        public void AddTrip(Trip trip)
        {
            _context.Trips.Add(trip);
        }

        public void DeleteTrip(Trip trip)
        {
            _context.Trips.Remove(trip);
        }

        public void UpdateTrip(Trip trip)
        {
            // No code required here as EF Core tracks changes,
            // but method is added for consistency and potential future use.
        }

        public async Task<IEnumerable<Trip>> GetTripsAsync()
        {
            return await _context.Trips
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<Trip> GetTripAsync(int id)
        {
            return await _context.Trips
                .Include(t => t.TripStops)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() >= 0);
        }
    }
}
