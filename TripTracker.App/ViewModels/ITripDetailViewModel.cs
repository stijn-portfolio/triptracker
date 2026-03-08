using System.Collections.ObjectModel;
using System.Windows.Input;
using TripTracker.App.Models;

namespace TripTracker.App.ViewModels
{
    // Interface voor TripDetailViewModel
    // Zoals IDetailsViewModel in SafariSnap (Les 3)
    public interface ITripDetailViewModel
    {
        bool IsLoading { get; set; }
        Trip? CurrentTrip { get; set; }
        ObservableCollection<TripStop> TripStops { get; set; }
        TripStop? SelectedStop { get; set; }
        ICommand AddStopCommand { get; set; }
        ICommand ViewStopCommand { get; set; }
        ICommand EditStopCommand { get; set; }
        ICommand DeleteStopCommand { get; set; }
        ICommand GoBackCommand { get; set; }
        ICommand ShowTripOnMapCommand { get; set; }
    }
}
