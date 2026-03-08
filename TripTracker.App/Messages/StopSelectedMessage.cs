using CommunityToolkit.Mvvm.Messaging.Messages;
using TripTracker.App.Models;

namespace TripTracker.App.Messages
{
    // Message om geselecteerde TripStop door te geven naar detail page
    public class StopSelectedMessage : ValueChangedMessage<TripStop>
    {
        public StopSelectedMessage(TripStop stop) : base(stop)
        {
        }
    }
}
