using System.Collections.ObjectModel;
using System.Windows.Input;
using TripTracker.App.Models;

namespace TripTracker.App.ViewModels
{
    // Interface voor TripsViewModel
    // Zoals IListViewModel in SafariSnap (Les 3)
    public interface ITripsViewModel
    {
        bool IsLoading { get; set; }
        ObservableCollection<Trip> Trips { get; set; }
        ObservableCollection<Trip> FilteredTrips { get; set; }
        ObservableCollection<YearFilterItem> YearFilters { get; set; }
        int? SelectedYear { get; set; }
        Trip? SelectedTrip { get; set; }
        ICommand ViewTripCommand { get; set; }
        ICommand AddTripCommand { get; set; }
        ICommand DeleteTripCommand { get; set; }
        ICommand EditTripCommand { get; set; }
        ICommand ShowAllOnMapCommand { get; set; }
        ICommand SelectYearCommand { get; set; }
    }
}
