using CommunityToolkit.Mvvm.Messaging.Messages;
using TripTracker.App.Models;

namespace TripTracker.App.Messages
{
    // Message om stops naar de MapPage te sturen
    // Kan vanuit 3 contexten worden gebruikt:
    // - TripsPage: alle stops van alle trips
    // - TripDetailPage: alle stops van één trip
    // - StopDetailPage: één specifieke stop
    public class ShowStopsOnMapMessage : ValueChangedMessage<(List<TripStop> Stops, string Title)>
    {
        public ShowStopsOnMapMessage(List<TripStop> stops, string title)
            : base((stops, title))
        {
        }
    }
}
