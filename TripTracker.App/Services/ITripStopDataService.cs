using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Interface voor TripStop-specifieke API operaties.
    /// Gebruikt standaard IApiService methodes.
    /// </summary>
    public interface ITripStopDataService : IApiService<TripStop>
    {
    }
}
