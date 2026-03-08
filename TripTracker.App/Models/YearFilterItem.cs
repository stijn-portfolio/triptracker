using CommunityToolkit.Mvvm.ComponentModel;

namespace TripTracker.App.Models
{
    // Wrapper class voor jaar filter buttons
    // Bevat Year en IsSelected voor visuele feedback
    public class YearFilterItem : ObservableObject
    {
        public int? Year { get; set; }

        public string DisplayText => Year?.ToString() ?? "All";

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
    }
}
