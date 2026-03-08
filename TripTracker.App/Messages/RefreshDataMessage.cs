using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TripTracker.App.Messages
{
    /// <summary>
    /// Message om ViewModels te vertellen dat data moet worden herladen.
    /// Wordt gebruikt voor zowel Trips als Stops refresh.
    /// </summary>
    public class RefreshDataMessage : ValueChangedMessage<bool>
    {
        public RefreshDataMessage(bool value) : base(value)
        {
        }
    }
}
