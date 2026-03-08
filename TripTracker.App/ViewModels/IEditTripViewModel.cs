using System.Windows.Input;

namespace TripTracker.App.ViewModels
{
    // Interface voor EditTripViewModel
    // Zoals IAddTripViewModel maar met SetTrip methode
    public interface IEditTripViewModel
    {
        int TripId { get; }
        string Name { get; set; }
        string? Description { get; set; }
        DateTime StartDate { get; set; }
        DateTime EndDate { get; set; }
        ImageSource? PhotoPreview { get; }
        bool HasPhoto { get; }
        ICommand SaveCommand { get; }
        ICommand CancelCommand { get; }
        ICommand PickPhotoCommand { get; }

        // Methode om bestaande trip data te laden
        void SetTrip(int tripId, string name, string? description, DateTime startDate, DateTime? endDate, string? imageUrl);
    }
}
