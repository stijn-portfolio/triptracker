using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind is LEEG - alle logica zit in ViewModel!
public partial class StopDetailPage : ContentPage
{
    public StopDetailPage(IStopDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
