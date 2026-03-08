using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TripTracker.App.Models
{
    // Model voor de MAUI app - erft van ObservableObject voor MVVM data binding
    // Zoals Sighting in SafariSnap (Les 2)
    public class Trip : ObservableObject
    {
        private int id;
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        private string name = string.Empty;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string? description;
        public string? Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private DateTime startDate;
        public DateTime StartDate
        {
            get => startDate;
            set => SetProperty(ref startDate, value);
        }

        private DateTime? endDate;
        public DateTime? EndDate
        {
            get => endDate;
            set => SetProperty(ref endDate, value);
        }

        private string? imageUrl;
        public string? ImageUrl
        {
            get => imageUrl;
            set => SetProperty(ref imageUrl, value);
        }

        private ObservableCollection<TripStop> tripStops = new();
        public ObservableCollection<TripStop> TripStops
        {
            get => tripStops;
            set => SetProperty(ref tripStops, value);
        }
    }
}
