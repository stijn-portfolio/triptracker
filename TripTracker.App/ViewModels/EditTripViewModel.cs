using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;
using TripTracker.App.Messages;
using TripTracker.App.Models;
using TripTracker.App.Services;
using NativeMedia;

namespace TripTracker.App.ViewModels
{
    /// <summary>
    /// ViewModel voor het bewerken van een Trip.
    /// Zoals AddTripViewModel maar met PutAsync.
    /// IRecipient voor TripEditMessage.
    /// </summary>
    public class EditTripViewModel : ObservableRecipient, IRecipient<TripEditMessage>, IEditTripViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPhotoService _photoService;
        private readonly ITripDataService _tripDataService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        private int tripId;
        public int TripId
        {
            get => tripId;
            private set => SetProperty(ref tripId, value);
        }

        private string name = string.Empty;
        public string Name
        {
            get => name;
            set
            {
                if (SetProperty(ref name, value))
                {
                    ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private string? description;
        public string? Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private DateTime startDate = DateTime.Today;
        public DateTime StartDate
        {
            get => startDate;
            set => SetProperty(ref startDate, value);
        }

        private DateTime endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => endDate;
            set => SetProperty(ref endDate, value);
        }

        private ImageSource? photoPreview;
        public ImageSource? PhotoPreview
        {
            get => photoPreview;
            set
            {
                if (SetProperty(ref photoPreview, value))
                {
                    OnPropertyChanged(nameof(HasPhoto));  // HANDMATIG triggeren! Berekend veld.
                }
            }
        }

        private byte[]? photoData;
        public byte[]? PhotoData
        {
            get => photoData;
            set => SetProperty(ref photoData, value);
        }

        public bool HasPhoto => PhotoPreview != null;

        // Oorspronkelijke image URL (voor als foto niet gewijzigd wordt)
        private string? originalImageUrl;

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand PickPhotoCommand { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public EditTripViewModel(
            INavigationService navigationService,
            IPhotoService photoService,
            ITripDataService tripDataService)
        {
            _navigationService = navigationService;
            _photoService = photoService;
            _tripDataService = tripDataService;
            BindCommands();

            // Registreer voor TripEditMessage
            Messenger.Register<EditTripViewModel, TripEditMessage>(this, (r, m) => r.Receive(m));
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE HANDLER
        // ═══════════════════════════════════════════════════════════

        public void Receive(TripEditMessage message)
        {
            var trip = message.Value;
            SetTrip(trip.Id, trip.Name, trip.Description, trip.StartDate, trip.EndDate, trip.ImageUrl);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND BINDING
        // ═══════════════════════════════════════════════════════════

        private void BindCommands()
        {
            SaveCommand = new AsyncRelayCommand(SaveTrip, CanSave);
            CancelCommand = new AsyncRelayCommand(Cancel);
            PickPhotoCommand = new AsyncRelayCommand(PickPhoto);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND HANDLERS
        // ═══════════════════════════════════════════════════════════

        public void SetTrip(int tripId, string name, string? description, DateTime startDate, DateTime? endDate, string? imageUrl)
        {
            TripId = tripId;
            Name = name;
            Description = description;
            StartDate = startDate;
            EndDate = endDate ?? startDate; // Default naar startDate als null
            originalImageUrl = imageUrl;

            // Laad bestaande foto als preview
            if (!string.IsNullOrEmpty(imageUrl))
            {
                // Check of het een lokaal bestand of URL is
                if (File.Exists(imageUrl))
                {
                    PhotoPreview = ImageSource.FromFile(imageUrl);
                }
                else if (imageUrl.StartsWith("http"))
                {
                    PhotoPreview = ImageSource.FromUri(new Uri(imageUrl));
                }
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name);
        }

        private async Task PickPhoto()
        {
            var bytes = await _photoService.PickPhotoAsync();
            if (bytes != null)
            {
                PhotoData = bytes;
                PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
        }

        private async Task SaveTrip()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return;
            }

            try
            {
                // Bepaal image URL
                string? photoPath = originalImageUrl;

                // Als nieuwe foto geselecteerd, sla lokaal op
                if (PhotoData != null)
                {
                    photoPath = await SavePhotoLocally();
                }

                var updatedTrip = new Trip
                {
                    Id = TripId,
                    Name = Name,
                    Description = Description,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    ImageUrl = photoPath ?? "https://picsum.photos/400/300"
                };

                // Gebruik generieke ApiService<Trip>.PutAsync()
                await _tripDataService.PutAsync(TripId, updatedTrip);

                // Stuur refresh message en ga terug
                WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
                await _navigationService.NavigateBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating trip: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    "Could not save changes. Please try again.",
                    "OK");
            }
        }

        private async Task<string> SavePhotoLocally()
        {
            if (PhotoData == null)
                return string.Empty;

            try
            {
                // 1. App folder (voor app-gebruik)
                var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
                Directory.CreateDirectory(photosDir);

                var fileName = $"trip_{Guid.NewGuid()}.jpg";
                var filePath = Path.Combine(photosDir, fileName);

                await File.WriteAllBytesAsync(filePath, PhotoData);

                // 2. OOK naar galerij (voor de gebruiker)
                try
                {
                    await MediaGallery.SaveAsync(MediaFileType.Image, PhotoData, fileName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Gallery] Could not save to gallery: {ex.Message}");
                    // Geen probleem - app folder save is gelukt
                }

                System.Diagnostics.Debug.WriteLine($"Photo saved to: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving photo: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task Cancel()
        {
            await _navigationService.NavigateBackAsync();
        }
    }
}
