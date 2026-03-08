using System.Windows.Input;

namespace TripTracker.App.ViewModels
{
    // Interface voor AddTripViewModel
    // Zoals IAddStopViewModel (Les 3 - SafariSnap pattern)
    public interface IAddTripViewModel
    {
        string Name { get; set; }
        string? Description { get; set; }
        DateTime StartDate { get; set; }
        DateTime EndDate { get; set; }
        ImageSource? PhotoPreview { get; }
        bool HasPhoto { get; }
        ICommand SaveCommand { get; }
        ICommand CancelCommand { get; }
        ICommand PickPhotoCommand { get; }
        ICommand CapturePhotoCommand { get; }
    }
}
