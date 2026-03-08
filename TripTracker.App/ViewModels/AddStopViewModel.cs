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
    /// ViewModel voor het toevoegen van een TripStop.
    /// Smart Stop Capture: foto → GPS → AI analyse.
    /// Gebaseerd op SafariSnap InfoViewModel (Les 3).
    /// </summary>
    public class AddStopViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>, IAddStopViewModel
    {
        // ═══════════════════════════════════════════════════════════
        // SERVICES (DI)
        // ═══════════════════════════════════════════════════════════

        private readonly INavigationService _navigationService;
        private readonly IPhotoService _photoService;
        private readonly IGeolocationService _geolocationService;
        private readonly IGeocodingService _geocodingService;
        private readonly IAnalyzeImageService _analyzeImageService;
        private readonly ITripStopDataService _tripStopDataService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        private Trip? currentTrip;
        public Trip? CurrentTrip
        {
            get => currentTrip;
            set => SetProperty(ref currentTrip, value);
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

        // Foto bytes voor opslag en AI analyse
        private byte[]? photoData;
        public byte[]? PhotoData
        {
            get => photoData;
            set => SetProperty(ref photoData, value);
        }

        // Loading indicator tijdens AI analyse
        private bool isAnalyzing;
        public bool IsAnalyzing
        {
            get => isAnalyzing;
            set => SetProperty(ref isAnalyzing, value);
        }

        // Helper property voor UI visibility
        public bool HasPhoto => PhotoPreview != null;

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
            set
            {
                if (SetProperty(ref latitude, value))
                {
                    OnPropertyChanged(nameof(LatitudeDisplay));  // HANDMATIG triggeren! Berekend veld.
                }
            }
        }

        // Display property voor XAML binding (string ipv double)
        public string LatitudeDisplay => Latitude != 0 ? Latitude.ToString("F6") : "Fetching...";

        private double longitude;
        public double Longitude
        {
            get => longitude;
            set
            {
                if (SetProperty(ref longitude, value))
                {
                    OnPropertyChanged(nameof(LongitudeDisplay));  // HANDMATIG triggeren! Berekend veld.
                }
            }
        }

        // Display property voor XAML binding (string ipv double)
        public string LongitudeDisplay => Longitude != 0 ? Longitude.ToString("F6") : "Fetching...";

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

        private string? country;
        public string? Country
        {
            get => country;
            set => SetProperty(ref country, value);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand CapturePhotoCommand { get; private set; }
        public ICommand PickPhotoCommand { get; private set; }
        public ICommand AnalyzePhotoCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public AddStopViewModel(
            INavigationService navigationService,
            IPhotoService photoService,
            IGeolocationService geolocationService,
            IGeocodingService geocodingService,
            IAnalyzeImageService analyzeImageService,
            ITripStopDataService tripStopDataService)
        {
            _navigationService = navigationService;
            _photoService = photoService;
            _geolocationService = geolocationService;
            _geocodingService = geocodingService;
            _analyzeImageService = analyzeImageService;
            _tripStopDataService = tripStopDataService;

            // Registreer voor TripSelectedMessage
            Messenger.Register<AddStopViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));

            BindCommands();
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE HANDLER
        // ═══════════════════════════════════════════════════════════

        public void Receive(TripSelectedMessage message)
        {
            CurrentTrip = message.Value;
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND BINDING
        // ═══════════════════════════════════════════════════════════

        private void BindCommands()
        {
            CapturePhotoCommand = new AsyncRelayCommand(CapturePhoto);
            PickPhotoCommand = new AsyncRelayCommand(PickPhoto);
            AnalyzePhotoCommand = new AsyncRelayCommand(AnalyzePhoto, () => HasPhoto && !IsAnalyzing);
            SaveCommand = new AsyncRelayCommand(SaveStop, CanSave);
            CancelCommand = new AsyncRelayCommand(Cancel);
        }

        private bool CanSave()
        {
            // Foto is VERPLICHT (gebruiker keuze)
            return HasPhoto && !string.IsNullOrWhiteSpace(Title) && !IsAnalyzing;
        }

        // ═══════════════════════════════════════════════════════════
        // PHOTO COMMANDS
        // ═══════════════════════════════════════════════════════════

        private async Task CapturePhoto()
        {
            // PhotoService bevat retry pattern voor Android
            var bytes = await _photoService.CapturePhotoAsync();
            if (bytes != null)
            {
                await ProcessPhoto(bytes);
            }
        }

        private async Task PickPhoto()
        {
            var bytes = await _photoService.PickPhotoAsync();
            if (bytes != null)
            {
                await ProcessPhoto(bytes);
            }
        }

        private async Task ProcessPhoto(byte[] bytes)
        {
            // Sla foto data op
            PhotoData = bytes;

            // Toon preview in UI
            PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));

            // Update command states DIRECT (zodat buttons werken)
            ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();

            // Haal GPS locatie op (op achtergrond - niet blocking)
            _ = GetLocationAndGeocode();
        }

        private async Task GetLocationAndGeocode()
        {
            try
            {
                var location = await _geolocationService.GetCurrentLocationAsync();

                if (location != null)
                {
                    var placemark = await _geocodingService.ReverseGeocodeAsync(location.Latitude, location.Longitude);

                    // UI updates MOETEN op MainThread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Latitude = location.Latitude;
                        Longitude = location.Longitude;

                        if (placemark != null)
                        {
                            // Bouw adres string - filter duplicaten
                            var addressParts = new List<string>();
                            if (!string.IsNullOrEmpty(placemark.Thoroughfare))
                                addressParts.Add(placemark.Thoroughfare);
                            if (!string.IsNullOrEmpty(placemark.SubLocality) &&
                                !addressParts.Contains(placemark.SubLocality))
                                addressParts.Add(placemark.SubLocality);
                            if (!string.IsNullOrEmpty(placemark.Locality) &&
                                !addressParts.Contains(placemark.Locality))
                                addressParts.Add(placemark.Locality);

                            Address = string.Join(", ", addressParts);
                            Country = placemark.CountryName;
                        }
                    });
                }
            }
            catch
            {
                // Geen probleem - gebruiker kan handmatig invullen (fallback)
            }
        }

        // ═══════════════════════════════════════════════════════════
        // AI ANALYSIS
        // ═══════════════════════════════════════════════════════════

        private async Task AnalyzePhoto()
        {
            if (PhotoData == null)
                return;

            try
            {
                IsAnalyzing = true;
                ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();

                var analysis = await _analyzeImageService.AnalyzePhotoAsync(PhotoData);

                if (analysis != null)
                {
                    Title = analysis.Title;
                    Description = analysis.Description;
                }
                else
                {
                    // Fallback: handmatige invoer
                    if (string.IsNullOrEmpty(Title))
                    {
                        Title = "Nieuwe stop";
                    }
                }
            }
            catch
            {
                // Fallback: gebruiker kan handmatig invullen
            }
            finally
            {
                IsAnalyzing = false;
                ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // SAVE / CANCEL
        // ═══════════════════════════════════════════════════════════

        private async Task SaveStop()
        {
            if (CurrentTrip == null || PhotoData == null || string.IsNullOrWhiteSpace(Title))
            {
                return;
            }

            try
            {
                // Sla foto lokaal op (gebruiker keuze: lokaal bestand + pad)
                var photoPath = await SavePhotoLocally();

                var newStop = new TripStop
                {
                    TripId = CurrentTrip.Id,
                    Title = Title,
                    Description = Description,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Address = Address,
                    PhotoUrl = photoPath, // Lokaal pad naar foto
                    Country = Country,
                    DateTime = DateTime.Now
                };

                await _tripStopDataService.PostAsync(newStop);

                // Stuur refresh message en ga terug
                WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
                await _navigationService.NavigateBackAsync();
            }
            catch
            {
                // Error saving - geen actie
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

                var fileName = $"stop_{Guid.NewGuid()}.jpg";
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

                return filePath;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task Cancel()
        {
            await _navigationService.NavigateBackAsync();
        }
    }
}
