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
    /// ViewModel voor trip detail pagina.
    /// Zoals DetailsViewModel in SafariSnap (Les 3).
    /// IRecipient voor TripSelectedMessage en RefreshDataMessage.
    /// </summary>
    public class TripDetailViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>, IRecipient<RefreshDataMessage>, ITripDetailViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ITripDataService _tripDataService;
        private readonly ITripStopDataService _tripStopDataService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        private Trip? currentTrip;
        public Trip? CurrentTrip
        {
            get => currentTrip;
            set => SetProperty(ref currentTrip, value);
        }

        private ObservableCollection<TripStop> tripStops = new();
        public ObservableCollection<TripStop> TripStops
        {
            get => tripStops;
            set => SetProperty(ref tripStops, value);
        }

        private TripStop? selectedStop;
        public TripStop? SelectedStop
        {
            get => selectedStop;
            set => SetProperty(ref selectedStop, value);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand AddStopCommand { get; set; }
        public ICommand ViewStopCommand { get; set; }
        public ICommand EditStopCommand { get; set; }
        public ICommand DeleteStopCommand { get; set; }
        public ICommand GoBackCommand { get; set; }
        public ICommand ShowTripOnMapCommand { get; set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public TripDetailViewModel(
            INavigationService navigationService,
            ITripDataService tripDataService,
            ITripStopDataService tripStopDataService)
        {
            _navigationService = navigationService;
            _tripDataService = tripDataService;
            _tripStopDataService = tripStopDataService;

            // Registreer voor TripSelectedMessage
            Messenger.Register<TripDetailViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));

            // Registreer voor RefreshDataMessage (herlaad stops na opslaan nieuwe stop)
            Messenger.Register<TripDetailViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

            BindCommands();
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE HANDLERS
        // ═══════════════════════════════════════════════════════════

        public void Receive(TripSelectedMessage message)
        {
            CurrentTrip = message.Value;
            _ = LoadTripStopsAsync();
        }

        public void Receive(RefreshDataMessage message)
        {
            // Herlaad ZOWEL trip ALS stops (trip dates kunnen gewijzigd zijn)
            _ = RefreshTripAndStopsAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // DATA LOADING
        // ═══════════════════════════════════════════════════════════

        private async Task LoadTripStopsAsync()
        {
            if (CurrentTrip != null)
            {
                IsLoading = true;
                try
                {
                    var stops = await _tripDataService.GetTripStopsAsync(CurrentTrip.Id);

                    // UI updates MOETEN op MainThread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TripStops = new ObservableCollection<TripStop>(stops);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading trip stops: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task RefreshTripAndStopsAsync()
        {
            if (CurrentTrip == null) return;

            IsLoading = true;
            try
            {
                // Herlaad trip (voor updated dates)
                var updatedTrip = await _tripDataService.GetAsync(CurrentTrip.Id);
                if (updatedTrip != null)
                {
                    CurrentTrip = updatedTrip;
                }

                // Herlaad stops
                var stops = await _tripDataService.GetTripStopsAsync(CurrentTrip.Id);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TripStops = new ObservableCollection<TripStop>(stops);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing trip data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND BINDING
        // ═══════════════════════════════════════════════════════════

        private void BindCommands()
        {
            AddStopCommand = new AsyncRelayCommand(GoToAddStop);
            ViewStopCommand = new AsyncRelayCommand<TripStop>(GoToStopDetail);
            EditStopCommand = new AsyncRelayCommand<TripStop>(EditStop);
            DeleteStopCommand = new AsyncRelayCommand<TripStop>(DeleteStop);
            GoBackCommand = new AsyncRelayCommand(GoBack);
            ShowTripOnMapCommand = new AsyncRelayCommand(ShowTripOnMap);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND HANDLERS
        // ═══════════════════════════════════════════════════════════

        private async Task GoToAddStop()
        {
            await _navigationService.NavigateToAddStopPageAsync();
            // Stuur CurrentTrip mee zodat AddStopViewModel weet bij welke trip de stop hoort
            if (CurrentTrip != null)
            {
                WeakReferenceMessenger.Default.Send(new TripSelectedMessage(CurrentTrip));
            }
        }

        private async Task GoToStopDetail(TripStop? stop)
        {
            if (stop != null)
            {
                SelectedStop = stop;
                await _navigationService.NavigateToStopDetailPageAsync();
                WeakReferenceMessenger.Default.Send(new StopSelectedMessage(stop));
                SelectedStop = null;
            }
        }

        private async Task EditStop(TripStop? stop)
        {
            if (stop != null)
            {
                await _navigationService.NavigateToEditStopPageAsync();
                WeakReferenceMessenger.Default.Send(new StopEditMessage(stop));
            }
        }

        private async Task DeleteStop(TripStop? stop)
        {
            if (stop != null)
            {
                try
                {
                    await _tripStopDataService.DeleteAsync(stop.Id);
                    TripStops.Remove(stop);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting stop: {ex.Message}");
                }
            }
        }

        private async Task GoBack()
        {
            await _navigationService.NavigateBackAsync();
        }

        private async Task ShowTripOnMap()
        {
            if (CurrentTrip == null || !TripStops.Any())
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "No Stops",
                    "This trip has no stops to show on the map.",
                    "OK");
                return;
            }

            // EERST navigeren, DAN message sturen (zodat MapViewModel al geregistreerd is)
            await _navigationService.NavigateToMapPageAsync();
            WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(TripStops.ToList(), CurrentTrip.Name));
        }
    }
}
