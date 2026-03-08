using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind is LEEG - alle logica zit in ViewModel!
public partial class TripDetailPage : ContentPage
{
    public TripDetailPage(ITripDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
