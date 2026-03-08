using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;
using TripTracker.App.Messages;
using TripTracker.App.Models;
using TripTracker.App.Services;

namespace TripTracker.App.ViewModels
{
    /// <summary>
    /// ViewModel voor stop detail pagina.
    /// IRecipient voor StopSelectedMessage en RefreshDataMessage.
    /// </summary>
    public class StopDetailViewModel : ObservableRecipient, IRecipient<StopSelectedMessage>, IRecipient<RefreshDataMessage>, IStopDetailViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ITripStopDataService _tripStopDataService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        private TripStop? currentStop;
        public TripStop? CurrentStop
        {
            get => currentStop;
            set => SetProperty(ref currentStop, value);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand GoBackCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand ShowStopOnMapCommand { get; set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public StopDetailViewModel(
            INavigationService navigationService,
            ITripStopDataService tripStopDataService)
        {
            _navigationService = navigationService;
            _tripStopDataService = tripStopDataService;

            // Registreer voor messages
            Messenger.Register<StopDetailViewModel, StopSelectedMessage>(this, (r, m) => r.Receive(m));
            Messenger.Register<StopDetailViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

            BindCommands();
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE HANDLERS
        // ═══════════════════════════════════════════════════════════

        public void Receive(StopSelectedMessage message)
        {
            CurrentStop = message.Value;
        }

        public void Receive(RefreshDataMessage message)
        {
            // Herlaad stop data na edit
            _ = RefreshCurrentStop();
        }

        // ═══════════════════════════════════════════════════════════
        // DATA LOADING
        // ═══════════════════════════════════════════════════════════

        private async Task RefreshCurrentStop()
        {
            if (CurrentStop == null) return;

            try
            {
                var refreshedStop = await _tripStopDataService.GetAsync(CurrentStop.Id);
                if (refreshedStop != null)
                {
                    CurrentStop = refreshedStop;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing stop: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND BINDING
        // ═══════════════════════════════════════════════════════════

        private void BindCommands()
        {
            GoBackCommand = new AsyncRelayCommand(GoBack);
            EditCommand = new AsyncRelayCommand(EditStop);
            DeleteCommand = new AsyncRelayCommand(DeleteStop);
            ShowStopOnMapCommand = new AsyncRelayCommand(ShowStopOnMap);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND HANDLERS
        // ═══════════════════════════════════════════════════════════

        private async Task GoBack()
        {
            await _navigationService.NavigateBackAsync();
        }

        private async Task EditStop()
        {
            if (CurrentStop == null) return;

            // Navigeer naar EditStopPage en stuur message met data
            await _navigationService.NavigateToEditStopPageAsync();
            WeakReferenceMessenger.Default.Send(new StopEditMessage(CurrentStop));
        }

        private async Task DeleteStop()
        {
            if (CurrentStop == null) return;

            // Bevestiging vragen
            var confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Delete Stop",
                $"Are you sure you want to delete '{CurrentStop.Title}'?",
                "Delete", "Cancel");

            if (!confirm) return;

            try
            {
                await _tripStopDataService.DeleteAsync(CurrentStop.Id);
                WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
                await _navigationService.NavigateBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting stop: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    "Could not delete stop. Please try again.",
                    "OK");
            }
        }

        private async Task ShowStopOnMap()
        {
            if (CurrentStop == null) return;

            // EERST navigeren, DAN message sturen (zodat MapViewModel al geregistreerd is)
            await _navigationService.NavigateToMapPageAsync();
            WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(
                new List<TripStop> { CurrentStop },
                CurrentStop.Title));
        }
    }
}
