using CommunityToolkit.Mvvm.Messaging.Messages;
using TripTracker.App.Models;

namespace TripTracker.App.Messages
{
    // Message om geselecteerde Trip door te geven naar detail page
    // Zoals SightingSelectedMessage in SafariSnap (Les 3)
    public class TripSelectedMessage : ValueChangedMessage<Trip>
    {
        public TripSelectedMessage(Trip trip) : base(trip)
        {
        }
    }
}
