using CommunityToolkit.Mvvm.ComponentModel;

namespace TripTracker.App.Models
{
    // Model voor TripStop - erft van ObservableObject voor MVVM data binding
    // Equivalent van Sighting in SafariSnap (Les 2)
    public class TripStop : ObservableObject
    {
        private int id;
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        private int tripId;
        public int TripId
        {
            get => tripId;
            set => SetProperty(ref tripId, value);
        }

        // Trip navigation property verwijderd - niet nodig
        // We gebruiken alleen TripId voor de relatie

        private string title = string.Empty;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        private string? description;
        public string? Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private double latitude;
        public double Latitude
        {
            get => latitude;
            set => SetProperty(ref latitude, value);
        }

        private double longitude;
        public double Longitude
        {
            get => longitude;
            set => SetProperty(ref longitude, value);
        }

        private string? address;
        public string? Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        private string? photoUrl;
        public string? PhotoUrl
        {
            get => photoUrl;
            set => SetProperty(ref photoUrl, value);
        }

        private DateTime dateTime;
        public DateTime DateTime
        {
            get => dateTime;
            set => SetProperty(ref dateTime, value);
        }

        private string? country;
        public string? Country
        {
            get => country;
            set => SetProperty(ref country, value);
        }
    }
}
