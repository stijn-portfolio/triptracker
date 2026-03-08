using System.Windows.Input;
using TripTracker.App.Models;

namespace TripTracker.App.ViewModels
{
    // Interface voor AddStopViewModel - uitgebreid met foto/analyse
    public interface IAddStopViewModel
    {
        // Trip waar we stop aan toevoegen
        Trip? CurrentTrip { get; set; }

        // Foto data
        ImageSource? PhotoPreview { get; set; }
        byte[]? PhotoData { get; set; }
        bool IsAnalyzing { get; set; }
        bool HasPhoto { get; }

        // Stop details (worden door AI ingevuld, bewerkbaar door gebruiker)
        string Title { get; set; }
        string? Description { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
        string LatitudeDisplay { get; }   // Computed: voor UI binding
        string LongitudeDisplay { get; }  // Computed: voor UI binding
        string? Address { get; set; }
        string? PhotoUrl { get; set; }
        string? Country { get; set; }

        // Commands
        ICommand CapturePhotoCommand { get; }
        ICommand PickPhotoCommand { get; }
        ICommand AnalyzePhotoCommand { get; }
        ICommand SaveCommand { get; }
        ICommand CancelCommand { get; }
    }
}
