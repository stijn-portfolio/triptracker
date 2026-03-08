using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind is LEEG - alle logica zit in ViewModel!
// Dit is een MUST voor MVVM (cursus vereiste)
public partial class TripsPage : ContentPage
{
    public TripsPage(ITripsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
