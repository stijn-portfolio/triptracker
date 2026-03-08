using System.Collections.ObjectModel;
using System.Windows.Input;
using TripTracker.App.Models;

namespace TripTracker.App.ViewModels
{
    // Interface voor MapViewModel
    public interface IMapViewModel
    {
        ObservableCollection<TripStop> Stops { get; }
        string Title { get; set; }
        ICommand GoBackCommand { get; }

        // Event dat fired wanneer alle stops geladen zijn (1x i.p.v. per stop)
        event Action? StopsUpdated;
    }
}
