using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TripTracker.App.Messages;
using TripTracker.App.Models;
using TripTracker.App.Services;

namespace TripTracker.App.ViewModels
{
    /// <summary>
    /// ViewModel voor MapPage.
    /// Ontvangt stops via ShowStopsOnMapMessage.
    /// </summary>
    public class MapViewModel : ObservableRecipient, IRecipient<ShowStopsOnMapMessage>, IMapViewModel
    {
        private readonly INavigationService _navigationService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<TripStop> Stops { get; } = new();

        // Event voor MapPage - fired 1x wanneer alle stops geladen zijn
        public event Action? StopsUpdated;

        private string title = "Map";
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand GoBackCommand { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public MapViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            // Registreer voor ShowStopsOnMapMessage
            Messenger.Register<MapViewModel, ShowStopsOnMapMessage>(this, (r, m) => r.Receive(m));

            GoBackCommand = new AsyncRelayCommand(GoBack);
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE HANDLER
        // ═══════════════════════════════════════════════════════════

        public void Receive(ShowStopsOnMapMessage message)
        {
            var (stops, mapTitle) = message.Value;

            // Clear en vul in één keer (geen CollectionChanged)
            Stops.Clear();
            foreach (var stop in stops)
            {
                Stops.Add(stop);
            }

            Title = mapTitle;

            // Fire event één keer na alle stops geladen
            StopsUpdated?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND HANDLERS
        // ═══════════════════════════════════════════════════════════

        private async Task GoBack()
        {
            await _navigationService.NavigateBackAsync();
        }
    }
}
