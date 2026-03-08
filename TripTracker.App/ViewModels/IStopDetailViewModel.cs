using System.Windows.Input;
using TripTracker.App.Models;

namespace TripTracker.App.ViewModels
{
    // Interface voor StopDetailViewModel
    public interface IStopDetailViewModel
    {
        TripStop? CurrentStop { get; set; }
        ICommand GoBackCommand { get; set; }
        ICommand EditCommand { get; set; }
        ICommand DeleteCommand { get; set; }
        ICommand ShowStopOnMapCommand { get; set; }
    }
}
