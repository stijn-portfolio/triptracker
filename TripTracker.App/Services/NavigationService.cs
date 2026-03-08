using TripTracker.App.Views;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Implementatie van NavigationService.
    /// Inject IServiceProvider om pages via DI op te halen.
    /// Zoals in SafariSnap (Les 3).
    /// </summary>
    public class NavigationService : INavigationService
    {
        private INavigation _navigation; // MAUI navigation
        private readonly IServiceProvider _serviceProvider; // DI container

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _navigation = Application.Current!.MainPage!.Navigation;
        }

        // ═══════════════════════════════════════════════════════════
        // NAVIGATION METHODS
        // ═══════════════════════════════════════════════════════════

        public async Task NavigateToTripDetailPageAsync()
        {
            await _navigation.PushAsync(_serviceProvider.GetRequiredService<TripDetailPage>());
        }

        public async Task NavigateToAddTripPageAsync()
        {
            await _navigation.PushAsync(_serviceProvider.GetRequiredService<AddTripPage>());
        }

        public async Task NavigateToEditTripPageAsync()
        {
            await _navigation.PushAsync(_serviceProvider.GetRequiredService<EditTripPage>());
        }

        public async Task NavigateToEditStopPageAsync()
        {
            await _navigation.PushAsync(_serviceProvider.GetRequiredService<EditStopPage>());
        }

        public async Task NavigateToAddStopPageAsync()
        {
            await _navigation.PushAsync(_serviceProvider.GetRequiredService<AddStopPage>());
        }

        public async Task NavigateToStopDetailPageAsync()
        {
            await _navigation.PushAsync(_serviceProvider.GetRequiredService<StopDetailPage>());
        }

        public async Task NavigateToMapPageAsync()
        {
            await _navigation.PushAsync(_serviceProvider.GetRequiredService<MapPage>());
        }

        public async Task NavigateBackAsync()
        {
            if (_navigation.NavigationStack.Count > 1)
            {
                await _navigation.PopAsync();
            }
            else
            {
                throw new InvalidOperationException("No pages to navigate back to!");
            }
        }
    }
}
