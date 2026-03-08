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
    /// ViewModel voor het bewerken van een TripStop.
    /// Tekstvelden en foto aanpasbaar, GPS blijft ongewijzigd.
    /// IRecipient voor StopEditMessage.
    /// </summary>
    public class EditStopViewModel : ObservableRecipient, IRecipient<StopEditMessage>, IEditStopViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IPhotoService _photoService;
        private readonly IAnalyzeImageService _analyzeImageService;
        private readonly ITripStopDataService _tripStopDataService;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        private int stopId;
        public int StopId
        {
            get => stopId;
            private set => SetProperty(ref stopId, value);
        }

        private int tripId;
        public int TripId
        {
            get => tripId;
            private set => SetProperty(ref tripId, value);
        }

        private string title = string.Empty;
        public string Title
        {
            get => title;
            set
            {
                if (SetProperty(ref title, value))
                {
                    ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged(); // Update Save button state wanneer title verandert

                }
            }
        }

        private string? description;
        public string? Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private string? address;
        public string? Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        private string? country;
        public string? Country
        {
            get => country;
            set => SetProperty(ref country, value);
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

        private bool isAnalyzing;
        public bool IsAnalyzing
        {
            get => isAnalyzing;
            set => SetProperty(ref isAnalyzing, value);
        }

        // Datum en tijd (apart voor DatePicker en TimePicker)
        private DateTime visitDate = DateTime.Today;
        public DateTime VisitDate
        {
            get => visitDate;
            set => SetProperty(ref visitDate, value);
        }

        private TimeSpan visitTime = DateTime.Now.TimeOfDay;
        public TimeSpan VisitTime
        {
            get => visitTime;
            set => SetProperty(ref visitTime, value);
        }

        // Bewaar originele waarden voor vergelijking
        private double latitude;
        private double longitude;
        private string? originalPhotoUrl;
        private string? originalAddress;

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand PickPhotoCommand { get; private set; }
        public ICommand AnalyzePhotoCommand { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public EditStopViewModel(
            INavigationService navigationService,
            IPhotoService photoService,
            IAnalyzeImageService analyzeImageService,
            ITripStopDataService tripStopDataService)
        {
            _navigationService = navigationService;
            _photoService = photoService;
            _analyzeImageService = analyzeImageService;
            _tripStopDataService = tripStopDataService;
            BindCommands();

            // Registreer voor StopEditMessage
            Messenger.Register<EditStopViewModel, StopEditMessage>(this, (r, m) => r.Receive(m));
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE HANDLER
        // ═══════════════════════════════════════════════════════════

        public void Receive(StopEditMessage message)
        {
            var stop = message.Value;
            SetStop(stop);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND BINDING
        // ═══════════════════════════════════════════════════════════

        private void BindCommands()
        {
            SaveCommand = new AsyncRelayCommand(SaveStop, CanSave);
            CancelCommand = new AsyncRelayCommand(Cancel);
            PickPhotoCommand = new AsyncRelayCommand(PickPhoto);
            AnalyzePhotoCommand = new AsyncRelayCommand(AnalyzePhoto, CanAnalyze);
        }

        // ═══════════════════════════════════════════════════════════
        // COMMAND HANDLERS
        // ═══════════════════════════════════════════════════════════

        private bool CanAnalyze() => HasPhoto && !IsAnalyzing;

        private async Task PickPhoto()
        {
            var bytes = await _photoService.PickPhotoAsync();
            if (bytes != null)
            {
                PhotoData = bytes;
                PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
                ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
            }
        }

        private async Task AnalyzePhoto()
        {
            if (PhotoData == null) return;

            try
            {
                IsAnalyzing = true;
                ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();

                var analysis = await _analyzeImageService.AnalyzePhotoAsync(PhotoData);

                if (analysis != null)
                {
                    Title = analysis.Title ?? Title;
                    Description = analysis.Description ?? Description;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditStop] AI Analysis failed: {ex.Message}");
            }
            finally
            {
                IsAnalyzing = false;
                ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
        }

        public void SetStop(TripStop stop)
        {
            StopId = stop.Id;
            TripId = stop.TripId;
            Title = stop.Title;
            Description = stop.Description;
            Address = stop.Address;
            Country = stop.Country;

            // Datum en tijd instellen
            VisitDate = stop.DateTime.Date;
            VisitTime = stop.DateTime.TimeOfDay;

            // Bewaar originele waarden voor vergelijking
            latitude = stop.Latitude;
            longitude = stop.Longitude;
            originalPhotoUrl = stop.PhotoUrl;
            originalAddress = stop.Address;

            // Laad foto preview en data
            _ = LoadPhotoAsync(stop.PhotoUrl);
        }

        private async Task LoadPhotoAsync(string? photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl)) return;

            try
            {
                if (File.Exists(photoUrl))
                {
                    // Lokaal bestand - laad bytes voor AI analyse
                    PhotoData = await File.ReadAllBytesAsync(photoUrl);
                    PhotoPreview = ImageSource.FromStream(() => new MemoryStream(PhotoData));
                }
                else if (photoUrl.StartsWith("http"))
                {
                    // Remote URL - download bytes
                    using var httpClient = new HttpClient();
                    PhotoData = await httpClient.GetByteArrayAsync(photoUrl);
                    PhotoPreview = ImageSource.FromStream(() => new MemoryStream(PhotoData));
                }

                // Update command state
                ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditStop] Error loading photo: {ex.Message}");
                // Fallback: alleen preview zonder data
                if (File.Exists(photoUrl))
                    PhotoPreview = ImageSource.FromFile(photoUrl);
                else if (photoUrl.StartsWith("http"))
                    PhotoPreview = ImageSource.FromUri(new Uri(photoUrl));
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        private async Task SaveStop()
        {
            if (string.IsNullOrWhiteSpace(Title)) return;

            try
            {
                // Combineer datum en tijd
                var combinedDateTime = VisitDate.Date + VisitTime;

                // Bepaal foto URL (nieuwe foto of origineel behouden)
                string? photoPath = originalPhotoUrl;
                if (PhotoData != null)
                {
                    photoPath = await SavePhotoLocally();
                }

                // Forward geocoding als adres is gewijzigd
                if (!string.IsNullOrWhiteSpace(Address) && Address != originalAddress)
                {
                    try
                    {
                        var searchAddress = !string.IsNullOrWhiteSpace(Country)
                            ? $"{Address}, {Country}"
                            : Address;

                        var locations = await Geocoding.Default.GetLocationsAsync(searchAddress);
                        var location = locations?.FirstOrDefault();

                        if (location != null)
                        {
                            latitude = location.Latitude;
                            longitude = location.Longitude;
                            System.Diagnostics.Debug.WriteLine($"[EditStop] Geocoded: {searchAddress} -> {latitude}, {longitude}");
                        }
                        else
                        {
                            await Application.Current!.MainPage!.DisplayAlert(
                                "Location Not Found",
                                "Could not find coordinates for this address. Location remains unchanged.",
                                "OK");
                        }
                    }
                    catch (Exception geoEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EditStop] Geocoding failed: {geoEx.Message}");
                    }
                }

                var updatedStop = new TripStop
                {
                    Id = StopId,
                    TripId = TripId,
                    Title = Title,
                    Description = Description,
                    Address = Address,
                    Country = Country,
                    Latitude = latitude,
                    Longitude = longitude,
                    DateTime = combinedDateTime,
                    PhotoUrl = photoPath
                };

                await _tripStopDataService.PutAsync(StopId, updatedStop);

                // Stuur refresh message en ga terug
                WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
                await _navigationService.NavigateBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating stop: {ex.Message}");
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
