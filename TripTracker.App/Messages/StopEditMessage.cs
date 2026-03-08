using CommunityToolkit.Mvvm.Messaging.Messages;
using TripTracker.App.Models;

namespace TripTracker.App.Messages
{
    // Message voor het doorgeven van stop data naar EditStopPage
    public class StopEditMessage : ValueChangedMessage<TripStop>
    {
        public StopEditMessage(TripStop stop) : base(stop) { }
    }
}
