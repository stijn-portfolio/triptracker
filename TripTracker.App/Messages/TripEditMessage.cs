using CommunityToolkit.Mvvm.Messaging.Messages;
using TripTracker.App.Models;

namespace TripTracker.App.Messages
{
    // Message voor het doorgeven van trip data naar EditTripPage
    // Zoals TripSelectedMessage pattern
    public class TripEditMessage : ValueChangedMessage<Trip>
    {
        public TripEditMessage(Trip trip) : base(trip) { }
    }
}
