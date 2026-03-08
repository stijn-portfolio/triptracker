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
    /// ViewModel voor de hoofdpagina met trips.
    /// Zoals ListViewModel in SafariSnap (Les 3).
    /// </summary>
    public class TripsViewModel : ObservableRecipient, IRecipient<RefreshDataMessage>, ITripsViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ITripDataService _tripDataService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        private ObservableCollection<Trip> trips = new();
        public ObservableCollection<Trip> Trips
        {
            get => trips;
            set => SetProperty(ref trips, value);
        }

        private ObservableCollection<Trip> filteredTrips = new();
        public ObservableCollection<Trip> FilteredTrips
        {
            get => filteredTrips;
            set => SetProperty(ref filteredTrips, value);
        }

        private ObservableCollection<YearFilterItem> yearFilters = new();
        public ObservableCollection<YearFilterItem> YearFilters
        {
            get => yearFilters;
            set => SetProperty(ref yearFilters, value);
        }

        private int? selectedYear;
        public int? SelectedYear
        {
            get => selectedYear;
            set
            {
                if (SetProperty(ref selectedYear, value))
                {
                    ApplyYearFilter();
                    UpdateYearFilterSelection();
                }
            }
        }

        private Trip? selectedTrip;
        public Trip? SelectedTrip
        {
            get => selectedTrip;
            set => SetProperty(ref selectedTrip, value);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand ViewTripCommand { get; set; }
        public ICommand AddTripCommand { get; set; }
        public ICommand DeleteTripCommand { get; set; }
        public ICommand EditTripCommand { get; set; }
        public ICommand ShowAllOnMapCommand { get; set; }
        public ICommand SelectYearCommand { get; set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public TripsViewModel(INavigationService navigationService, ITripDataService tripDataService)
        {
            _navigationService = navigationService;
            _tripDataService = tripDataService;

            // Registreer voor RefreshDataMessage
            Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

            LoadTripsAsync();
            BindCommands();
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE HANDLER
        // ═══════════════════════════════════════════════════════════

        public void Receive(RefreshDataMessage message)
        {
            // Fire-and-forget: start async zonder te wachten
            _ = LoadTripsAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // DATA LOADING
        // ═══════════════════════════════════════════════════════════

        private async Task LoadTripsAsync()
        {
            IsLoading = true;
            try
            {
                var tripList = await _tripDataService.GetAllAsync();

                // UI updates MOETEN op MainThread (anders crash/freeze)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Trips.Clear();
                    foreach (var trip in tripList)
                    {
                        Trips.Add(trip);
                    }

                    UpdateAvailableYears(); //YearFilters vullen
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading trips: {ex.Message}");
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
            ViewTripCommand = new AsyncRelayCommand<Trip>(GoToTripDetail);
            AddTripCommand = new AsyncRelayCommand(AddNewTrip);
            DeleteTripCommand = new AsyncRelayCommand<Trip>(DeleteTrip);
            EditTripCommand = new AsyncRelayCommand<Trip>(EditTrip);
            ShowAllOnMapCommand = new AsyncRelayCommand(ShowAllOnMap);
            SelectYearCommand = new RelayCommand<object>(SelectYear);
        }

        // ═══════════════════════════════════════════════════════════
        // YEAR FILTER HELPERS
        // ═══════════════════════════════════════════════════════════

        private void SelectYear(object? yearObj)
        {
            // CommandParameter is altijd YearFilterItem (vanuit XAML DataTemplate)
            if (yearObj is YearFilterItem item)
                SelectedYear = item.Year;
        }

        private void UpdateAvailableYears()
        {
            var years = Trips
                .Select(t => t.StartDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // Clear en vul opnieuw i.p.v. nieuwe collectie maken
            YearFilters.Clear();
            YearFilters.Add(new YearFilterItem { Year = null, IsSelected = false });
            foreach (var year in years)
            {
                YearFilters.Add(new YearFilterItem { Year = year, IsSelected = false });
            }

            // Default: huidig jaar als het bestaat, anders All
            var currentYear = DateTime.Now.Year;
            var newSelectedYear = years.Contains(currentYear) ? currentYear : (int?)null;

            // Alleen updaten als waarde verandert
            if (SelectedYear != newSelectedYear)
            {
                SelectedYear = newSelectedYear;
            }
            else
            {
                // Forceer filter en UI update bij eerste keer laden
                ApplyYearFilter();
                UpdateYearFilterSelection();
            }
        }

        private void UpdateYearFilterSelection()
        {
            foreach (var item in YearFilters)
            {
                // Alleen updaten als waarde daadwerkelijk verandert (voorkomt onnodige UI updates)
                var shouldBeSelected = item.Year == SelectedYear;
                if (item.IsSelected != shouldBeSelected)
                {
                    item.IsSelected = shouldBeSelected;
                }
            }
        }

        private void ApplyYearFilter()
        {
            // Clear en vul opnieuw i.p.v. nieuwe collectie maken (betere performance)
            FilteredTrips.Clear();

            var tripsToShow = SelectedYear == null
                ? Trips
                : Trips.Where(t => t.StartDate.Year == SelectedYear);

            foreach (var trip in tripsToShow)
            {
                FilteredTrips.Add(trip);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND HANDLERS
        // ═══════════════════════════════════════════════════════════

        private async Task GoToTripDetail(Trip? trip)
        {
            if (trip != null)
            {
                SelectedTrip = trip;
                await _navigationService.NavigateToTripDetailPageAsync();
                WeakReferenceMessenger.Default.Send(new TripSelectedMessage(trip));
                SelectedTrip = null;
            }
        }

        private async Task AddNewTrip()
        {
            await _navigationService.NavigateToAddTripPageAsync();
        }

        private async Task DeleteTrip(Trip? trip)
        {
            if (trip == null)
                return;

            // Bevestigingsdialoog
            var confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Delete Trip",
                $"Are you sure you want to delete '{trip.Name}'?",
                "Delete",
                "Cancel");

            if (!confirm)
                return;

            try
            {
                await _tripDataService.DeleteAsync(trip.Id);

                // Verwijder lokaal uit lijst en update filter
                Trips.Remove(trip);
                ApplyYearFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting trip: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    "Could not delete trip. Please try again.",
                    "OK");
            }
        }

        private async Task EditTrip(Trip? trip)
        {
            if (trip == null)
                return;

            // EERST navigeren, DAN message sturen (zodat target VM geregistreerd is)
            await _navigationService.NavigateToEditTripPageAsync();
            WeakReferenceMessenger.Default.Send(new TripEditMessage(trip));
        }

        private async Task ShowAllOnMap()
        {
            IsLoading = true;
            try
            {
                // Haal alle stops op van alle trips (TripStops worden niet standaard meegeladen)
                var allStops = new List<TripStop>();

                foreach (var trip in Trips)
                {
                    try
                    {
                        var stops = await _tripDataService.GetTripStopsAsync(trip.Id);
                        allStops.AddRange(stops);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading stops for trip {trip.Id}: {ex.Message}");
                    }
                }

                if (!allStops.Any())
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "No Stops",
                        "There are no stops to show on the map.",
                        "OK");
                    return;
                }

                // EERST navigeren, DAN message sturen (zodat MapViewModel al geregistreerd is)
                await _navigationService.NavigateToMapPageAsync();
                WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(allStops, "All Stops"));
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
