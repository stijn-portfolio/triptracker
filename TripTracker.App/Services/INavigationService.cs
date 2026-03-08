namespace TripTracker.App.Services
{
    // Interface voor navigatie tussen pagina's
    // Zoals in SafariSnap (Les 3)
    public interface INavigationService
    {
        Task NavigateToTripDetailPageAsync();
        Task NavigateToAddTripPageAsync();  // Fase 10
        Task NavigateToEditTripPageAsync(); // Fase 11
        Task NavigateToEditStopPageAsync(); // Fase 11
        Task NavigateToAddStopPageAsync();
        Task NavigateToStopDetailPageAsync();
        Task NavigateToMapPageAsync();      // Fase 13
        Task NavigateBackAsync();
    }
}
