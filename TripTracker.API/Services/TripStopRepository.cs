using Microsoft.EntityFrameworkCore;
using TripTracker.API.DbContexts;
using TripTracker.API.Entities;

namespace TripTracker.API.Services
{
    public class TripStopRepository : ITripStopRepository
    {
        private readonly TripTrackerContext _context;

        public TripStopRepository(TripTrackerContext context)
        {
            _context = context;
        }

        public void AddTripStop(TripStop tripStop)
        {
            _context.TripStops.Add(tripStop);
        }

        public void DeleteTripStop(TripStop tripStop)
        {
            _context.TripStops.Remove(tripStop);
        }

        public void UpdateTripStop(TripStop tripStop)
        {
            // No code required here as EF Core tracks changes,
            // but method is added for consistency and potential future use.
        }

        public async Task<IEnumerable<TripStop>> GetTripStopsAsync(int tripId)
        {
            return await _context.TripStops
                .Where(s => s.TripId == tripId)
                .Include(s => s.Trip)
                .OrderByDescending(s => s.DateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TripStop>> GetAllTripStopsAsync()
        {
            return await _context.TripStops
                .Include(s => s.Trip)
                .OrderByDescending(s => s.DateTime)
                .ToListAsync();
        }

        public async Task<TripStop> GetTripStopAsync(int id)
        {
            return await _context.TripStops
                .Include(s => s.Trip)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() >= 0);
        }
    }
}
