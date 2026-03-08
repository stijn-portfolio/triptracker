using System.Windows.Input;

namespace TripTracker.App.ViewModels
{
    // Interface voor EditStopViewModel - DI registratie
    public interface IEditStopViewModel
    {
        int StopId { get; }
        string Title { get; set; }
        string? Description { get; set; }
        string? Address { get; set; }
        string? Country { get; set; }
        DateTime VisitDate { get; set; }
        TimeSpan VisitTime { get; set; }
        ImageSource? PhotoPreview { get; }
        bool HasPhoto { get; }
        bool IsAnalyzing { get; }

        ICommand SaveCommand { get; }
        ICommand CancelCommand { get; }
        ICommand PickPhotoCommand { get; }
        ICommand AnalyzePhotoCommand { get; }
    }
}
