using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Concrete implementatie van ApiService voor TripStop.
    /// Zoals SightingDataService in SafariSnap (Les 3).
    /// </summary>
    public class TripStopDataService : ApiService<TripStop>, ITripStopDataService
    {
        protected override string EndPoint => "tripstops";
    }
}
