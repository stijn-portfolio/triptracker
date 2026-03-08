using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind voor AddTripPage
// Minimaal: alleen BindingContext setup (MVVM compliant)
public partial class AddTripPage : ContentPage
{
    public AddTripPage(IAddTripViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
