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
    /// ViewModel voor het toevoegen van een Trip.
    /// Zoals AddStopViewModel (Les 3 - SafariSnap pattern).
    /// </summary>
    public class AddTripViewModel : ObservableObject, IAddTripViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPhotoService _photoService;
        private readonly ITripDataService _tripDataService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        private string name = string.Empty;
        public string Name
        {
            get => name;
            set
            {
                if (SetProperty(ref name, value))
                {
                    // Update Save button state wanneer naam verandert
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

        // Foto preview voor UI
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

        // Foto bytes voor opslag
        private byte[]? photoData;
        public byte[]? PhotoData
        {
            get => photoData;
            set => SetProperty(ref photoData, value);
        }

        // Helper property voor UI visibility
        public bool HasPhoto => PhotoPreview != null;

        // Bewaar pad naar opgeslagen foto
        private string? savedPhotoPath;

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand PickPhotoCommand { get; private set; }
        public ICommand CapturePhotoCommand { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public AddTripViewModel(
            INavigationService navigationService,
            IPhotoService photoService,
            ITripDataService tripDataService)
        {
            _navigationService = navigationService;
            _photoService = photoService;
            _tripDataService = tripDataService;
            BindCommands();
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND BINDING
        // ═══════════════════════════════════════════════════════════

        private void BindCommands()
        {
            SaveCommand = new AsyncRelayCommand(SaveTrip, CanSave);
            CancelCommand = new AsyncRelayCommand(Cancel);
            PickPhotoCommand = new AsyncRelayCommand(PickPhoto);
            CapturePhotoCommand = new AsyncRelayCommand(CapturePhoto);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND HANDLERS
        // ═══════════════════════════════════════════════════════════

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

        private async Task CapturePhoto()
        {
            // PhotoService bevat retry pattern voor Android
            var bytes = await _photoService.CapturePhotoAsync();
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
                // Sla foto lokaal op als er een is
                string? photoPath = null;
                if (PhotoData != null)
                {
                    photoPath = await SavePhotoLocally();
                }

                var newTrip = new Trip
                {
                    Name = Name,
                    Description = Description,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    ImageUrl = photoPath ?? "https://picsum.photos/400/300" // Placeholder als geen foto
                };

                // Gebruik generieke ApiService<Trip>.PostAsync()
                await _tripDataService.PostAsync(newTrip);

                // Stuur refresh message en ga terug
                WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
                await _navigationService.NavigateBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving trip: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    "Could not save trip. Please try again.",
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
