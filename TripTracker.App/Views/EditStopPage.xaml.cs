using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

public partial class EditStopPage : ContentPage
{
    // Code-behind LEEG - MVVM pattern
    public EditStopPage(IEditStopViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
