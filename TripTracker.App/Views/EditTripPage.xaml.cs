using TripTracker.App.ViewModels;

namespace TripTracker.App.Views
{
    // Code-behind voor EditTripPage
    // Minimale code - MVVM pattern (logica in ViewModel)
    public partial class EditTripPage : ContentPage
    {
        public EditTripPage(IEditTripViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
