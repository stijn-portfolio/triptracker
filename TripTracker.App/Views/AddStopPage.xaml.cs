using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

public partial class AddStopPage : ContentPage
{
    public AddStopPage(IAddStopViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
