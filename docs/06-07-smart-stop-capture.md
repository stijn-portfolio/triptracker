---
fase: Smart Stop Capture met OpenAI & Geolocation
status: Voltooid
tags:
  - maui
  - openai
  - vision-api
  - gps
  - geolocation
  - geocoding
  - mediapicker
  - permissions
  - examen
created: 2025-12-20
---

# Smart stop capture - OpenAI vision + GPS integratie

## Overzicht

Deze documentatie beschrijft de **Smart Stop Capture** functionaliteit van TripTracker, waarbij AI (OpenAI Vision API) en GPS-geolocation worden gecombineerd voor een naadloze gebruikerservaring.

> [!info] Doel
> "One-tap" UX voor het toevoegen van trip stops: gebruiker maakt foto, AI analyseert automatisch wat er op de foto staat, GPS haalt locatie op, en alles wordt ingevuld - gebruiker hoeft alleen nog te bevestigen.

**Wat is er gebouwd:**
- Foto capture via camera/galerij met automatische compressie
- GPS locatie ophalen met permission handling op MainThread
- Reverse geocoding (coordinaten naar adres) via Nominatim API
- OpenAI Vision API integratie voor automatische titel + beschrijving
- Lokale foto opslag in app data directory
- Fire-and-forget patroon voor GPS (geen blocking UI)

**Gebaseerd op:**
- Les 3 - SafariSnap (PhotoService, GeolocationService, AnalyzeImageService)
- OpenAI Vision API documentatie

---

## 1. Architectuur: services overzicht

### Service dependencies

```
AddStopViewModel
    ├── IPhotoService → MediaPicker (camera/gallery)
    ├── IGeolocationService → GPS locatie
    ├── IGeocodingService → Reverse geocoding (coordinaten → adres)
    └── IAnalyzeImageService → OpenAI Vision API
```

### Flow diagram

```
User taps Camera/Gallery
    ↓
PhotoService → byte[] (compressed)
    ↓
[Update UI Preview]
    ↓
Fire-and-forget GPS task ──┐
    ↓                       ↓
User taps "Analyze"    GeolocationService → Location
    ↓                       ↓
AnalyzeImageService    GeocodingService → Placemark (adres)
    ↓                       ↓
[Update Title/Desc]    [Update Lat/Lng/Address] (MainThread!)
    ↓
User taps "Save"
    ↓
Save photo locally → App Data Directory
    ↓
POST to API
```

---

## 2. Photo service - camera & gallery

### IPhotoService interface

**Locatie:** `Services/IPhotoService.cs`

```csharp
namespace TripTracker.App.Services
{
    // Interface voor foto capture/selectie - zoals SafariSnap (Les 3)
    // Gebruikt MAUI MediaPicker voor camera en galerij
    public interface IPhotoService
    {
        // Maak foto met camera, retourneer bytes (of null bij cancel)
        Task<byte[]?> CapturePhotoAsync();

        // Kies foto uit galerij, retourneer bytes (of null bij cancel)
        Task<byte[]?> PickPhotoAsync();
    }
}
```

### PhotoService implementation

**Locatie:** `Services/PhotoService.cs`

```csharp
using Microsoft.Maui.Graphics.Platform;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Foto service - combineert capture/pick met compressie.
    /// Gebaseerd op SafariSnap (Les 3).
    /// </summary>
    public class PhotoService : IPhotoService
    {
        // ═══════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════

        // Max 4MB voor OpenAI Vision API
        private const int ImageMaxSizeBytes = 4194304;
        // Max resolutie voor compressie
        private const int ImageMaxResolution = 1024;

        // ═══════════════════════════════════════════════════════════
        // PHOTO CAPTURE
        // ═══════════════════════════════════════════════════════════

        public async Task<byte[]?> CapturePhotoAsync()
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                    return null;

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return null;

                // Kleine delay om Android tijd te geven na camera
                await Task.Delay(100);

                // Comprimeer met retry pattern (Android file lock)
                byte[]? bytes = null;
                try
                {
                    bytes = await ResizePhotoStreamAsync(photo);
                }
                catch
                {
                    // Retry na extra delay
                    await Task.Delay(200);
                    bytes = await ResizePhotoStreamAsync(photo);
                }

                return bytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Camera error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PHOTO PICK
        // ═══════════════════════════════════════════════════════════

        public async Task<byte[]?> PickPhotoAsync()
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return null;

                return await ResizePhotoStreamAsync(photo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private async Task<byte[]> ResizePhotoStreamAsync(FileResult photo)
        {
            byte[]? result = null;

            using (var stream = await photo.OpenReadAsync())
            {
                if (stream.Length > ImageMaxSizeBytes)
                {
                    var image = PlatformImage.FromStream(stream);
                    if (image != null)
                    {
                        var newImage = image.Downsize(ImageMaxResolution, true);
                        result = newImage.AsBytes();
                    }
                }
                else
                {
                    using (var binaryReader = new BinaryReader(stream))
                    {
                        result = binaryReader.ReadBytes((int)stream.Length);
                    }
                }
            }

            return result!;
        }
    }
}
```

> [!tip] Examenvraag: Waarom compressie?
> - **OpenAI Vision API** heeft een limiet van 20MB per afbeelding, maar kleinere afbeeldingen zijn sneller en goedkoper
> - **4MB limiet** zorgt voor snelle uploads zonder kwaliteitsverlies
> - **1024px resolutie** is ruim voldoende voor object herkenning
> - `PlatformImage.Downsize()` behoudt aspect ratio

> [!warning] Let op: MediaPicker permissions
> Op Android moet je CAMERA en STORAGE permissions hebben in AndroidManifest.xml. MAUI vraagt automatisch om permissies wanneer je MediaPicker gebruikt.

---

## 3. Geolocation service - GPS locatie

### IGeolocationService interface

**Locatie:** `Services/IGeolocationService.cs`

```csharp
namespace TripTracker.App.Services
{
    // Interface voor GPS locatie - zoals SafariSnap (Les 3)
    public interface IGeolocationService
    {
        // Haal huidige GPS locatie op (of null bij fout/geen permissie)
        Task<Location?> GetCurrentLocationAsync();
    }
}
```

### GeolocationService implementation

**Locatie:** `Services/GeolocationService.cs`

```csharp
namespace TripTracker.App.Services
{
    // GPS Locatie service - EXACT zoals SafariSnap (Les 3)
    // LET OP: Permissions moeten op MainThread gevraagd worden!
    public class GeolocationService : IGeolocationService
    {
        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[GPS] Starting location request...");

                // Check/request permission op MainThread
                var status = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("[GPS] Checking permission...");
                    var currentStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    System.Diagnostics.Debug.WriteLine($"[GPS] Current status: {currentStatus}");

                    if (currentStatus != PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("[GPS] Requesting permission...");
                        currentStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                        System.Diagnostics.Debug.WriteLine($"[GPS] After request: {currentStatus}");
                    }

                    return currentStatus;
                });

                System.Diagnostics.Debug.WriteLine($"[GPS] Final permission status: {status}");

                if (status == PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("[GPS] Getting location...");
                    // GeolocationAccuracy.Medium = goed genoeg voor reizen, snel resultaat
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.Default.GetLocationAsync(request);
                    System.Diagnostics.Debug.WriteLine($"[GPS] Got location: {location?.Latitude}, {location?.Longitude}");
                    return location;
                }

                System.Diagnostics.Debug.WriteLine("[GPS] Permission NOT granted");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPS] ERROR: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }
}
```

> [!tip] Examenvraag: Waarom MainThread.InvokeOnMainThreadAsync?
> **KRITIEK!** Permissions moeten ALTIJD op de UI thread (MainThread) gevraagd worden, anders crash de app op Android. `MainThread.InvokeOnMainThreadAsync()` zorgt dat de permission dialog op de correcte thread wordt getoond.

> [!info] GeolocationAccuracy opties
> - `Best`: Hoogste precisie, langzaam, veel batterij
> - `Medium`: Goed genoeg voor meeste apps (gebruikt hier)
> - `Low`: Snelste, minste batterij
>
> Voor TripTracker is `Medium` perfect: binnen 100m nauwkeurig, binnen 10 seconden resultaat.

---

## 4. Geocoding service - reverse geocoding

### IGeocodingService interface

**Locatie:** `Services/IGeocodingService.cs`

```csharp
namespace TripTracker.App.Services
{
    // Interface voor reverse geocoding (coordinaten → adres)
    // Nieuw voor TripTracker - niet in SafariSnap
    public interface IGeocodingService
    {
        // Zet lat/lng om naar adres informatie
        Task<Placemark?> ReverseGeocodeAsync(double latitude, double longitude);
    }
}
```

### GeocodingService implementation

**Locatie:** `Services/GeocodingService.cs`

```csharp
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TripTracker.App.Services
{
    // Reverse Geocoding service - zet GPS coordinaten om naar adres
    // Gebruikt MAUI Geocoding API met fallback naar OpenStreetMap Nominatim
    // (Windows vereist Bing Maps token, daarom fallback)
    public class GeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;

        public GeocodingService()
        {
            _httpClient = new HttpClient();
            // Nominatim vereist een User-Agent header
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TripTracker/1.0");
        }

        public async Task<Placemark?> ReverseGeocodeAsync(double latitude, double longitude)
        {
            // Probeer eerst MAUI native geocoding
            var result = await TryNativeGeocodingAsync(latitude, longitude);

            if (result != null)
                return result;

            // Fallback naar OpenStreetMap Nominatim (gratis, geen API key nodig)
            return await TryNominatimGeocodingAsync(latitude, longitude);
        }

        private async Task<Placemark?> TryNativeGeocodingAsync(double latitude, double longitude)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Trying native geocoding for: {latitude}, {longitude}");

                var location = new Location(latitude, longitude);
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
                var placemark = placemarks?.FirstOrDefault();

                if (placemark != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Geocoding] Native success: {placemark.Locality}, {placemark.CountryName}");
                    return placemark;
                }

                return null;
            }
            catch (Exception ex)
            {
                // Op Windows faalt dit zonder Bing Maps token - dat is OK, we gebruiken fallback
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Native failed: {ex.Message}");
                return null;
            }
        }

        private async Task<Placemark?> TryNominatimGeocodingAsync(double latitude, double longitude)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Trying Nominatim fallback for: {latitude}, {longitude}");

                // OpenStreetMap Nominatim API (gratis, geen key nodig)
                // Gebruik punt als decimaalscheidingsteken (niet komma!)
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                var response = await _httpClient.GetFromJsonAsync<NominatimResponse>(url);

                if (response?.Address != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Geocoding] Nominatim success: {response.Address.City ?? response.Address.Town ?? response.Address.Village}, {response.Address.Country}");

                    // Converteer Nominatim response naar MAUI Placemark
                    return new Placemark
                    {
                        CountryName = response.Address.Country,
                        CountryCode = response.Address.CountryCode?.ToUpper(),
                        AdminArea = response.Address.State,
                        SubAdminArea = response.Address.County,
                        Locality = response.Address.City ?? response.Address.Town ?? response.Address.Village,
                        SubLocality = response.Address.Suburb,
                        Thoroughfare = response.Address.Road,
                        SubThoroughfare = response.Address.HouseNumber,
                        PostalCode = response.Address.Postcode
                    };
                }

                System.Diagnostics.Debug.WriteLine("[Geocoding] Nominatim returned no address");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Geocoding] Nominatim failed: {ex.Message}");
                return null;
            }
        }

        // Nominatim API response classes
        private class NominatimResponse
        {
            [JsonPropertyName("address")]
            public NominatimAddress? Address { get; set; }
        }

        private class NominatimAddress
        {
            [JsonPropertyName("house_number")]
            public string? HouseNumber { get; set; }

            [JsonPropertyName("road")]
            public string? Road { get; set; }

            [JsonPropertyName("suburb")]
            public string? Suburb { get; set; }

            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("town")]
            public string? Town { get; set; }

            [JsonPropertyName("village")]
            public string? Village { get; set; }

            [JsonPropertyName("county")]
            public string? County { get; set; }

            [JsonPropertyName("state")]
            public string? State { get; set; }

            [JsonPropertyName("postcode")]
            public string? Postcode { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }

            [JsonPropertyName("country_code")]
            public string? CountryCode { get; set; }
        }
    }
}
```

> [!info] Waarom Nominatim fallback?
> **MAUI Geocoding API** gebruikt platform-specifieke providers:
> - **Android**: Google Play Services (werkt out-of-the-box)
> - **iOS**: Apple Maps (werkt out-of-the-box)
> - **Windows**: Bing Maps (vereist API key - NIET gratis!)
>
> **Nominatim** is een gratis OpenStreetMap service zonder API key. Perfect als fallback!

> [!warning] Nominatim Usage Policy
> Nominatim heeft een fair use policy:
> - Max 1 request per seconde
> - **User-Agent header VERPLICHT**
> - Gebruik caching waar mogelijk
>
> Voor TripTracker (low frequency, user-initiated) is dit geen probleem.

> [!tip] Examenvraag: Waarom InvariantCulture bij ToString()?
> In België gebruiken we komma (`,`) als decimaalscheidingsteken: `51,2194`
>
> De Nominatim API verwacht een punt (`.`): `51.2194`
>
> `InvariantCulture` forceert punt als decimaalscheidingsteken, ongeacht systeem locale.

---

## 5. Analyze image service - OpenAI vision API

### IAnalyzeImageService interface

**Locatie:** `Services/IAnalyzeImageService.cs`

```csharp
namespace TripTracker.App.Services
{
    // Interface voor AI foto analyse - zoals SafariSnap (Les 3)
    // Gebruikt OpenAI Vision API om titel en beschrijving te genereren
    public interface IAnalyzeImageService
    {
        // Analyseer reisfoto en genereer titel + beschrijving
        Task<StopAnalysis?> AnalyzePhotoAsync(byte[] imageData);
    }

    // Resultaat van de AI analyse
    public class StopAnalysis
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
```

### AnalyzeImageService implementation

**Locatie:** `Services/AnalyzeImageService.cs`

```csharp
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripTracker.App.Services
{
    // AI Foto Analyse service - gebaseerd op SafariSnap (Les 3)
    // Gebruikt OpenAI Vision API (GPT-4o) voor het analyseren van reisfoto's
    public class AnalyzeImageService : IAnalyzeImageService
    {
        private readonly OpenAIClient _client;

        public AnalyzeImageService()
        {
            _client = new OpenAIClient(OpenAIKeys.Key);
        }

        public async Task<StopAnalysis?> AnalyzePhotoAsync(byte[] imageData)
        {
            try
            {
                var chatClient = _client.GetChatClient(OpenAIKeys.LLModel);

                // Prompt in Nederlands (gebruiker keuze)
                // Vraagt om JSON output voor makkelijke parsing
                var prompt =
@"Analyseer deze reisfoto en geef een korte beschrijving.

Retourneer ALLEEN geldige JSON in dit formaat:
{
  ""title"": ""korte titel (max 5 woorden)"",
  ""description"": ""beschrijving in 2-3 zinnen""
}

BELANGRIJK:
- Schrijf in het Nederlands
- Beschrijf wat je ziet (gebouw, landschap, monument, etc.)
- Wees beknopt maar informatief
- Als je de locatie herkent, noem die dan

Voeg GEEN markdown of andere opmaak toe. Alleen JSON!";

                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(prompt),
                        ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageData), "image/jpeg")
                    )
                };

                ChatCompletionOptions options = new();
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

                var outputText = completion.Content[0].Text.Trim();

                System.Diagnostics.Debug.WriteLine($"OpenAI response: {outputText}");

                // Strip markdown code blocks als OpenAI die toevoegt
                if (outputText.StartsWith("```"))
                {
                    // Verwijder ```json of ``` aan begin en ``` aan eind
                    var lines = outputText.Split('\n');
                    var jsonLines = lines.Skip(1).TakeWhile(l => !l.StartsWith("```"));
                    outputText = string.Join("\n", jsonLines).Trim();
                    System.Diagnostics.Debug.WriteLine($"OpenAI cleaned: {outputText}");
                }

                // Parse JSON response
                var result = JsonSerializer.Deserialize<AnalysisResponse>(outputText);

                if (result != null)
                {
                    return new StopAnalysis
                    {
                        Title = result.Title ?? "Nieuwe stop",
                        Description = result.Description ?? ""
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI Analysis error: {ex.Message}");
                return null;
            }
        }

        // Interne class voor JSON deserialisatie
        private class AnalysisResponse
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }
        }
    }
}
```

### OpenAI keys configuration

**Locatie:** `Services/OpenAIKeys.cs`

```csharp
namespace TripTracker.App.Services
{
    // OpenAI configuratie - zoals in SafariSnap (Les 3)
    // LET OP: In productie zou je deze keys NOOIT in code zetten!
    // Gebruik dan Azure Key Vault of environment variables.
    public static class OpenAIKeys
    {
        // API Key (NIET in git - zie OpenAIKeys.cs in project)
        public const string Key = "sk-proj-YOUR-API-KEY-HERE";

        // GPT-4o Vision model voor foto analyse
        public const string LLModel = "gpt-4o";
    }
}
```

> [!warning] Security Best Practices
> **NOOIT API keys in code!** Dit is alleen voor development/education.
>
> In productie:
> - Gebruik Azure Key Vault
> - Of environment variables
> - Of server-side proxy (API key blijft op server)

> [!tip] Examenvraag: Waarom GPT-4o en niet GPT-4 Vision?
> **GPT-4o** (o = omni) is OpenAI's nieuwste multimodal model:
> - Sneller dan GPT-4 Vision
> - Goedkoper (50% minder tokens)
> - Betere image understanding
> - Native ondersteuning voor tekst + afbeeldingen in 1 model

> [!info] Markdown Stripping Logica
> OpenAI voegt soms markdown code blocks toe aan JSON output:
>
> ```
> ```json
> {"title": "Eiffeltoren", "description": "..."}
> ```
> ```
>
> De code detecteert dit patroon en verwijdert de ` ``` ` markers voor parsing.

---

## 6. AddStopViewModel - complete flow

### Properties en dependencies

**Locatie:** `ViewModels/AddStopViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;
using TripTracker.App.Messages;
using TripTracker.App.Models;
using TripTracker.App.Services;

namespace TripTracker.App.ViewModels
{
    // ViewModel voor het toevoegen van een TripStop
    // HERZIEN voor Fase 6+7: Smart Stop Capture
    // Gebaseerd op SafariSnap InfoViewModel (Les 3)
    public class AddStopViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>, IAddStopViewModel
    {
        // Services (DI)
        private readonly INavigationService _navigationService;
        private readonly IPhotoService _photoService;
        private readonly IGeolocationService _geolocationService;
        private readonly IGeocodingService _geocodingService;
        private readonly IAnalyzeImageService _analyzeImageService;

        // Properties
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
                    OnPropertyChanged(nameof(HasPhoto));
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
                    OnPropertyChanged(nameof(LatitudeDisplay));
                }
            }
        }

        // Display property voor XAML binding (string ipv double)
        public string LatitudeDisplay => Latitude != 0 ? Latitude.ToString("F6") : "";

        private double longitude;
        public double Longitude
        {
            get => longitude;
            set
            {
                if (SetProperty(ref longitude, value))
                {
                    OnPropertyChanged(nameof(LongitudeDisplay));
                }
            }
        }

        // Display property voor XAML binding (string ipv double)
        public string LongitudeDisplay => Longitude != 0 ? Longitude.ToString("F6") : "";

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

        // Commands
        public ICommand CapturePhotoCommand { get; private set; }
        public ICommand PickPhotoCommand { get; private set; }
        public ICommand AnalyzePhotoCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public AddStopViewModel(
            INavigationService navigationService,
            IPhotoService photoService,
            IGeolocationService geolocationService,
            IGeocodingService geocodingService,
            IAnalyzeImageService analyzeImageService)
        {
            _navigationService = navigationService;
            _photoService = photoService;
            _geolocationService = geolocationService;
            _geocodingService = geocodingService;
            _analyzeImageService = analyzeImageService;

            // Registreer voor TripSelectedMessage
            Messenger.Register<AddStopViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));

            BindCommands();
        }

        public void Receive(TripSelectedMessage message)
        {
            CurrentTrip = message.Value;
        }

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
    }
}
```

### Photo processing flow

```csharp
private async Task CapturePhoto()
{
    System.Diagnostics.Debug.WriteLine("[CapturePhoto] Starting camera...");
    var bytes = await _photoService.CapturePhotoAsync();
    System.Diagnostics.Debug.WriteLine($"[CapturePhoto] Got bytes: {bytes?.Length ?? 0}");
    if (bytes != null)
    {
        await ProcessPhoto(bytes);
    }
}

private async Task PickPhoto()
{
    System.Diagnostics.Debug.WriteLine("[PickPhoto] Opening gallery...");
    var bytes = await _photoService.PickPhotoAsync();
    System.Diagnostics.Debug.WriteLine($"[PickPhoto] Got bytes: {bytes?.Length ?? 0}");
    if (bytes != null)
    {
        await ProcessPhoto(bytes);
    }
}

private async Task ProcessPhoto(byte[] bytes)
{
    System.Diagnostics.Debug.WriteLine($"[ProcessPhoto] Processing {bytes.Length} bytes...");

    // Sla foto data op
    PhotoData = bytes;

    // Toon preview in UI
    PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
    System.Diagnostics.Debug.WriteLine($"[ProcessPhoto] HasPhoto = {HasPhoto}");

    // Update command states DIRECT (zodat buttons werken)
    ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
    System.Diagnostics.Debug.WriteLine("[ProcessPhoto] Commands updated");

    // Haal GPS locatie op (op achtergrond - niet blocking)
    _ = GetLocationAndGeocode();
}
```

> [!tip] Examenvraag: Fire-and-Forget patroon
> `_ = GetLocationAndGeocode();` is een **fire-and-forget** task:
> - GPS request loopt op achtergrond (niet blocking)
> - Gebruiker kan direct verder werken
> - UI update gebeurt via MainThread callback wanneer GPS klaar is
>
> **Voordeel**: UX is niet blocking - gebruiker kan meteen AI analyse starten terwijl GPS bezig is.

### GPS + geocoding flow (Fire-and-Forget)

```csharp
private async Task GetLocationAndGeocode()
{
    System.Diagnostics.Debug.WriteLine("[GetLocation] Starting...");
    try
    {
        // Haal GPS locatie op
        System.Diagnostics.Debug.WriteLine("[GetLocation] Getting GPS location...");
        var location = await _geolocationService.GetCurrentLocationAsync();
        System.Diagnostics.Debug.WriteLine($"[GetLocation] GPS result: {location?.Latitude}, {location?.Longitude}");

        if (location != null)
        {
            // Reverse geocoding: coordinaten → adres
            System.Diagnostics.Debug.WriteLine("[GetLocation] Reverse geocoding...");
            var placemark = await _geocodingService.ReverseGeocodeAsync(location.Latitude, location.Longitude);
            System.Diagnostics.Debug.WriteLine($"[GetLocation] Geocoding result: {placemark?.Locality}, {placemark?.CountryName}");

            // UI updates MOETEN op MainThread (fire-and-forget task)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Latitude = location.Latitude;
                Longitude = location.Longitude;

                if (placemark != null)
                {
                    // Bouw adres string
                    var addressParts = new List<string>();
                    if (!string.IsNullOrEmpty(placemark.Thoroughfare))
                        addressParts.Add(placemark.Thoroughfare);
                    if (!string.IsNullOrEmpty(placemark.SubLocality))
                        addressParts.Add(placemark.SubLocality);
                    if (!string.IsNullOrEmpty(placemark.Locality))
                        addressParts.Add(placemark.Locality);

                    Address = string.Join(", ", addressParts);
                    Country = placemark.CountryName;
                }
                System.Diagnostics.Debug.WriteLine($"[GetLocation] UI updated: {Latitude}, {Longitude}, {Address}, {Country}");
            });
        }
        System.Diagnostics.Debug.WriteLine("[GetLocation] Done");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[GetLocation] ERROR: {ex.Message}");
        // Geen probleem - gebruiker kan handmatig invullen (fallback)
    }
}
```

> [!warning] MainThread.BeginInvokeOnMainThread - KRITIEK!
> **UI updates** vanuit een background task MOETEN op de MainThread:
> - `BeginInvokeOnMainThread()` = fire-and-forget (niet blocking)
> - `InvokeOnMainThreadAsync()` = await-able (blocking tot UI update klaar is)
>
> Voor property updates gebruik je `BeginInvokeOnMainThread()` - geen await nodig.

### AI analysis flow

```csharp
private async Task AnalyzePhoto()
{
    System.Diagnostics.Debug.WriteLine("[AnalyzePhoto] Starting...");

    if (PhotoData == null)
    {
        System.Diagnostics.Debug.WriteLine("[AnalyzePhoto] ERROR: PhotoData is null!");
        return;
    }

    System.Diagnostics.Debug.WriteLine($"[AnalyzePhoto] PhotoData size: {PhotoData.Length} bytes");

    try
    {
        IsAnalyzing = true;
        ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();

        System.Diagnostics.Debug.WriteLine("[AnalyzePhoto] Calling OpenAI...");
        var analysis = await _analyzeImageService.AnalyzePhotoAsync(PhotoData);
        System.Diagnostics.Debug.WriteLine($"[AnalyzePhoto] OpenAI returned: {analysis?.Title ?? "NULL"}");

        if (analysis != null)
        {
            Title = analysis.Title;
            Description = analysis.Description;
        }
        else
        {
            // Fallback: handmatige invoer (gebruiker keuze)
            if (string.IsNullOrEmpty(Title))
            {
                Title = "Nieuwe stop";
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[AnalyzePhoto] ERROR: {ex.GetType().Name}: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"[AnalyzePhoto] StackTrace: {ex.StackTrace}");
        // Fallback: gebruiker kan handmatig invullen
    }
    finally
    {
        IsAnalyzing = false;
        ((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
    }
}
```

> [!info] Command CanExecute Updates
> `NotifyCanExecuteChanged()` triggert een herevaluatie van `CanExecute`:
> - `AnalyzePhotoCommand` disabled tijdens `IsAnalyzing = true`
> - `SaveCommand` disabled tijdens `IsAnalyzing = true` of als `HasPhoto = false`
>
> Dit voorkomt dubbele requests en invalid states.

### Save flow - lokale foto opslag

```csharp
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

        var stopService = new TripStopDataService();
        await stopService.PostAsync(newStop);

        // Stuur refresh message en ga terug
        WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
        await _navigationService.NavigateBackAsync();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error saving stop: {ex.Message}");
    }
}

private async Task<string> SavePhotoLocally()
{
    if (PhotoData == null)
        return string.Empty;

    try
    {
        // Maak Photos folder in app data
        var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
        Directory.CreateDirectory(photosDir);

        // Unieke bestandsnaam
        var fileName = $"stop_{Guid.NewGuid()}.jpg";
        var filePath = Path.Combine(photosDir, fileName);

        // Schrijf bytes naar bestand
        await File.WriteAllBytesAsync(filePath, PhotoData);

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
```

> [!tip] Examenvraag: FileSystem.AppDataDirectory
> **MAUI FileSystem API** geeft platform-specifieke paden:
> - **Android**: `/data/data/com.companyname.triptracker/files/`
> - **iOS**: `~/Library/Application Support/`
> - **Windows**: `C:\Users\<user>\AppData\Local\Packages\<app>\LocalState\`
>
> `AppDataDirectory` is persistent (blijft bij app updates), maar wordt verwijderd bij uninstall.

> [!info] Waarom Guid.NewGuid() voor filename?
> Zorgt voor unieke bestandsnamen zonder conflicts:
> - `stop_3f2504e0-4f89-11d3-9a0c-0305e82c3301.jpg`
>
> Geen risico op overschrijven van bestaande foto's.

---

## 7. UI - AddStopPage.xaml

**Locatie:** `Views/AddStopPage.xaml`

### Hoofdstructuur

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TripTracker.App.Views.AddStopPage"
             Title="Add Stop">

    <!-- HERZIEN Fase 6+7: Smart Stop Capture -->
    <!-- Nieuwe UX: Foto eerst, dan AI analyse, dan bewerken -->

    <Grid>
        <!-- Hoofdinhoud -->
        <ScrollView Padding="15">
            <VerticalStackLayout Spacing="15">

                <!-- FOTO SECTIE (prominent bovenaan) -->
                <Frame BorderColor="LightGray"
                       Padding="0"
                       HeightRequest="250"
                       CornerRadius="10"
                       IsClippedToBounds="True">
                    <Grid>
                        <!-- Placeholder als geen foto -->
                        <VerticalStackLayout IsVisible="{Binding HasPhoto, Converter={StaticResource InvertedBoolConverter}}"
                                             VerticalOptions="Center"
                                             HorizontalOptions="Center"
                                             Spacing="10">
                            <Label Text="Take or pick a photo to start"
                                   FontSize="16"
                                   TextColor="Gray"
                                   HorizontalTextAlignment="Center"/>
                        </VerticalStackLayout>

                        <!-- Foto preview -->
                        <Image Source="{Binding PhotoPreview}"
                               Aspect="AspectFill"
                               IsVisible="{Binding HasPhoto}"/>
                    </Grid>
                </Frame>

                <!-- Camera/Galerij knoppen -->
                <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
                    <Button Text="Camera"
                            Command="{Binding CapturePhotoCommand}"
                            BackgroundColor="#512BD4"/>
                    <Button Text="Gallery"
                            Command="{Binding PickPhotoCommand}"
                            Grid.Column="1"
                            BackgroundColor="#512BD4"/>
                </Grid>

                <!-- AI Analyse knop (alleen zichtbaar als er een foto is) -->
                <Button Text="Analyze Photo with AI"
                        Command="{Binding AnalyzePhotoCommand}"
                        IsVisible="{Binding HasPhoto}"
                        BackgroundColor="#2E7D32"
                        TextColor="White"/>

                <!-- BEWERKBARE VELDEN (zichtbaar na foto) -->
                <VerticalStackLayout IsVisible="{Binding HasPhoto}" Spacing="15">

                    <!-- Title -->
                    <VerticalStackLayout>
                        <Label Text="Title *" FontAttributes="Bold"/>
                        <Entry Text="{Binding Title}"
                               Placeholder="Enter title (or use AI)"
                               FontSize="18"/>
                    </VerticalStackLayout>

                    <!-- Description -->
                    <VerticalStackLayout>
                        <Label Text="Description" FontAttributes="Bold"/>
                        <Editor Text="{Binding Description}"
                                Placeholder="Enter description (or use AI)"
                                HeightRequest="80"/>
                    </VerticalStackLayout>

                    <!-- Location info (auto-filled) -->
                    <Frame BorderColor="LightGray" Padding="10" CornerRadius="8">
                        <VerticalStackLayout Spacing="8">
                            <Label Text="Location" FontSize="16" FontAttributes="Bold"/>

                            <Grid ColumnDefinitions="*, *" ColumnSpacing="10" RowDefinitions="Auto, Auto">
                                <VerticalStackLayout>
                                    <Label Text="Latitude" FontSize="12" TextColor="Gray"/>
                                    <Label Text="{Binding LatitudeDisplay}" FontSize="14"/>
                                </VerticalStackLayout>
                                <VerticalStackLayout Grid.Column="1">
                                    <Label Text="Longitude" FontSize="12" TextColor="Gray"/>
                                    <Label Text="{Binding LongitudeDisplay}" FontSize="14"/>
                                </VerticalStackLayout>
                            </Grid>

                            <VerticalStackLayout>
                                <Label Text="Address" FontSize="12" TextColor="Gray"/>
                                <Label Text="{Binding Address}" FontSize="14" TextColor="{StaticResource Gray600}"/>
                            </VerticalStackLayout>

                            <VerticalStackLayout>
                                <Label Text="Country" FontSize="12" TextColor="Gray"/>
                                <Label Text="{Binding Country}" FontSize="14" TextColor="{StaticResource Gray600}"/>
                            </VerticalStackLayout>
                        </VerticalStackLayout>
                    </Frame>

                    <!-- Save/Cancel knoppen -->
                    <Grid ColumnDefinitions="*, *" ColumnSpacing="10" Margin="0,10,0,0">
                        <Button Text="Cancel"
                                Command="{Binding CancelCommand}"
                                BackgroundColor="LightGray"
                                TextColor="Black"/>
                        <Button Text="Save Stop"
                                Command="{Binding SaveCommand}"
                                Grid.Column="1"
                                BackgroundColor="#512BD4"/>
                    </Grid>

                </VerticalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <!-- Loading overlay tijdens AI analyse -->
        <Grid BackgroundColor="#80000000"
              IsVisible="{Binding IsAnalyzing}">
            <VerticalStackLayout VerticalOptions="Center"
                                 HorizontalOptions="Center"
                                 Spacing="15">
                <ActivityIndicator IsRunning="True"
                                   Color="White"
                                   HeightRequest="50"
                                   WidthRequest="50"/>
                <Label Text="Analyzing photo..."
                       TextColor="White"
                       FontSize="18"/>
            </VerticalStackLayout>
        </Grid>
    </Grid>

</ContentPage>
```

### Progressive disclosure pattern

**Principe:** Toon alleen wat de gebruiker NU nodig heeft. Meer opties verschijnen pas wanneer relevant.

```
┌─────────────────────────────────────────────────────────────────┐
│                      STAP 1: START                              │
│                                                                 │
│  ┌─────────────────────────────────────┐                       │
│  │     "Take or pick a photo to start" │  ← Placeholder        │
│  └─────────────────────────────────────┘                       │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐                            │
│  │   Camera     │  │   Gallery    │  ← Enige zichtbare actie   │
│  └──────────────┘  └──────────────┘                            │
│                                                                 │
│  ❌ AI button, Title, Description, Save (verborgen)            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ Gebruiker maakt foto
┌─────────────────────────────────────────────────────────────────┐
│                      STAP 2: FOTO GEMAAKT                       │
│                                                                 │
│  ┌─────────────────────────────────────┐                       │
│  │         [FOTO PREVIEW]              │  ← Foto zichtbaar     │
│  └─────────────────────────────────────┘                       │
│                                                                 │
│  ┌─────────────────────────────────────┐                       │
│  │      Analyze Photo with AI          │  ← NU zichtbaar!      │
│  └─────────────────────────────────────┘                       │
│                                                                 │
│  Title: [________________]              ← NU zichtbaar!         │
│  Description: [__________]                                      │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐                            │
│  │   Cancel     │  │  Save Stop   │     ← NU zichtbaar!        │
│  └──────────────┘  └──────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ Gebruiker klikt "Analyze"
┌─────────────────────────────────────────────────────────────────┐
│                      STAP 3: ANALYZING                          │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░││
│  │░░░░░░░░░░░░░░░░  ⟳  Analyzing photo...  ░░░░░░░░░░░░░░░░░░░░││
│  │░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░││
│  └─────────────────────────────────────────────────────────────┘│
│                         OVERLAY                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ AI klaar
┌─────────────────────────────────────────────────────────────────┐
│                      STAP 4: INGEVULD                           │
│                                                                 │
│  Title: [Eiffel Tower___________]  ← AI ingevuld, bewerkbaar    │
│  Description: [The iconic iron...]                              │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐                            │
│  │   Cancel     │  │  Save Stop   │                            │
│  └──────────────┘  └──────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
```

**Implementatie in AddStopPage.xaml:**

| Element | Visibility Binding | Wanneer zichtbaar |
|---------|-------------------|-------------------|
| Placeholder tekst | `HasPhoto` + `InvertedBoolConverter` | Geen foto |
| Foto preview | `HasPhoto` | Na foto |
| AI Analyze knop | `HasPhoto` | Na foto |
| Invoervelden | `HasPhoto` | Na foto |
| Loading overlay | `IsAnalyzing` | Tijdens AI analyse |

**Key XAML Patterns:**

```xml
<!-- Placeholder: zichtbaar als GEEN foto -->
<VerticalStackLayout IsVisible="{Binding HasPhoto,
                      Converter={StaticResource InvertedBoolConverter}}">
    <Label Text="Take or pick a photo to start"/>
</VerticalStackLayout>

<!-- Alles hieronder: zichtbaar als WEL foto -->
<Button Text="Analyze Photo with AI"
        IsVisible="{Binding HasPhoto}"/>

<VerticalStackLayout IsVisible="{Binding HasPhoto}">
    <!-- Title, Description, Save/Cancel -->
</VerticalStackLayout>

<!-- Overlay: zichtbaar tijdens analyse -->
<Grid BackgroundColor="#80000000" IsVisible="{Binding IsAnalyzing}">
    <ActivityIndicator IsRunning="True" Color="White"/>
    <Label Text="Analyzing photo..." TextColor="White"/>
</Grid>
```

> [!tip] InvertedBoolConverter
> Converteert `true` → `false` en `false` → `true`.
> Handig voor placeholder (toon alleen als `HasPhoto = false`).

> [!success] Voordelen Progressive Disclosure
> - **Minder overweldigend** voor gebruiker
> - **Duidelijke flow**: foto → analyse → bewerken → opslaan
> - **Voorkomt fouten**: kan niet opslaan zonder foto

---

## 8. Android permissions

**Locatie:** `Platforms/Android/AndroidManifest.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
	<application android:allowBackup="true" android:icon="@mipmap/appicon" android:roundIcon="@mipmap/appicon_round" android:supportsRtl="true"></application>

	<!-- Network permissions -->
	<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
	<uses-permission android:name="android.permission.INTERNET" />

	<!-- Camera permission (voor foto's maken) -->
	<uses-permission android:name="android.permission.CAMERA" />
	<uses-feature android:name="android.hardware.camera" android:required="false" />
	<uses-feature android:name="android.hardware.camera.autofocus" android:required="false" />

	<!-- Location permissions (voor GPS) -->
	<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
	<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />

	<!-- Storage permissions (voor foto opslag - oudere Android versies) -->
	<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
	<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

	<!-- Media permissions (Android 13+) -->
	<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
</manifest>
```

> [!warning] Android 13+ Permissions
> **Android 13** (API 33) heeft nieuwe media permissions:
> - `READ_MEDIA_IMAGES`: Voor galerij toegang (vervangt `READ_EXTERNAL_STORAGE`)
> - `READ_MEDIA_VIDEO`: Voor video's
> - `READ_MEDIA_AUDIO`: Voor audio
>
> Oudere `READ/WRITE_EXTERNAL_STORAGE` permissions blijven nodig voor Android < 13.

> [!tip] Examenvraag: Waarom `required="false"` bij camera features?
> App moet ook werken op devices zonder camera (zoals emulators of tablets):
> - `android:required="false"` betekent: optioneel, niet verplicht
> - `MediaPicker.IsCaptureSupported` checkt of camera beschikbaar is
> - Zonder deze flag zou app niet installeerbaar zijn op devices zonder camera

---

## 9. Service registration in MauiProgram

**Locatie:** `MauiProgram.cs`

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Registreer services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IApiService, ApiService>();

        // Foto & AI services
        builder.Services.AddSingleton<IPhotoService, PhotoService>();
        builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
        builder.Services.AddSingleton<IGeocodingService, GeocodingService>();
        builder.Services.AddSingleton<IAnalyzeImageService, AnalyzeImageService>();

        // ViewModels
        builder.Services.AddTransient<AddStopViewModel>();

        // Views
        builder.Services.AddTransient<AddStopPage>();

        return builder.Build();
    }
}
```

> [!info] Singleton vs Transient
> - **Singleton**: 1 instance voor hele app lifecycle (services)
> - **Transient**: Nieuwe instance bij elke request (ViewModels, Views)

---

## 10. Examenvragen & antwoorden

### Vraag 1: waarom fire-and-Forget voor GPS?

**Antwoord:**
GPS locatie ophalen kan 5-10 seconden duren (vooral indoor). Met fire-and-forget:
- UI blijft responsive
- Gebruiker kan direct AI analyse starten
- GPS update komt via `MainThread.BeginInvokeOnMainThread()` wanneer klaar
- Fallback: Als GPS faalt, kan gebruiker handmatig locatie invullen

**Code:**
```csharp
// Fire-and-forget (underscore discard)
_ = GetLocationAndGeocode();
```

### Vraag 2: verschil tussen MainThread.BeginInvoke en InvokeOnMainThreadAsync?

**Antwoord:**
| Methode | Type | Gebruik |
|---------|------|---------|
| `BeginInvokeOnMainThread()` | Fire-and-forget | UI updates, geen await nodig |
| `InvokeOnMainThreadAsync()` | Awaitable | Permission requests, return values |

**Voorbeeld:**
```csharp
// Fire-and-forget (property updates)
MainThread.BeginInvokeOnMainThread(() =>
{
    Latitude = location.Latitude;
});

// Awaitable (permission request)
var status = await MainThread.InvokeOnMainThreadAsync(async () =>
{
    return await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
});
```

### Vraag 3: waarom InvariantCulture bij double.ToString()?

**Antwoord:**
Belgische locale gebruikt komma (`,`) als decimaalscheidingsteken: `51,2194`

Nominatim API verwacht punt (`.`): `51.2194`

**Oplossing:**
```csharp
var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
// Result: ...?lat=51.2194 (correct)
```

Zonder InvariantCulture zou het `51,2194` zijn → API error!

### Vraag 4: waarom markdown stripping bij OpenAI response?

**Antwoord:**
OpenAI voegt soms markdown code blocks toe aan JSON:

```
```json
{"title": "Eiffeltoren", "description": "..."}
```
```

De code detecteert ` ``` ` markers en verwijdert ze:

```csharp
if (outputText.StartsWith("```"))
{
    var lines = outputText.Split('\n');
    var jsonLines = lines.Skip(1).TakeWhile(l => !l.StartsWith("```"));
    outputText = string.Join("\n", jsonLines).Trim();
}
```

Zonder deze cleanup zou `JsonSerializer.Deserialize()` falen.

### Vraag 5: waarom NotifyCanExecuteChanged() na property updates?

**Antwoord:**
Commands met `CanExecute` logica moeten gere-evaluated worden:

```csharp
PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));

// HasPhoto is nu true, maar command weet dit niet automatisch!
((AsyncRelayCommand)AnalyzePhotoCommand).NotifyCanExecuteChanged();
((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
```

`NotifyCanExecuteChanged()` triggert:
1. `CanExecute` wordt opnieuw geëvalueerd
2. Button enabled/disabled state wordt geupdate in UI

### Vraag 6: waarom Nominatim als fallback?

**Antwoord:**
**MAUI Geocoding API** gebruikt platform-specifieke providers:
- Android: Google Play Services (gratis)
- iOS: Apple Maps (gratis)
- Windows: Bing Maps (**vereist API key - NIET gratis!**)

**Nominatim** (OpenStreetMap):
- Gratis, geen API key nodig
- Fair use policy: max 1 req/sec
- **User-Agent header VERPLICHT**

Code gebruikt try/catch met fallback:
```csharp
// Probeer eerst native
var result = await TryNativeGeocodingAsync(latitude, longitude);
if (result != null) return result;

// Fallback naar Nominatim
return await TryNominatimGeocodingAsync(latitude, longitude);
```

### Vraag 7: waarom foto compressie?

**Antwoord:**
**OpenAI Vision API** accepteert max 20MB, maar:
- Kleinere afbeeldingen = sneller upload
- Kleinere afbeeldingen = goedkoper (minder tokens)
- 1024px resolutie is voldoende voor object herkenning

**PhotoService** comprimeert automatisch:
```csharp
private const int ImageMaxSizeBytes = 4194304; // 4MB
private const int ImageMaxResolution = 1024;

if (stream.Length > ImageMaxSizeBytes)
{
    var image = PlatformImage.FromStream(stream);
    var newImage = image.Downsize(ImageMaxResolution, true);
    result = newImage.AsBytes();
}
```

### Vraag 8: verschil tussen ObservableObject en ObservableRecipient?

**Antwoord:**
| Base Class | Features |
|------------|----------|
| `ObservableObject` | INotifyPropertyChanged, SetProperty() |
| `ObservableRecipient` | **+ Messenger support (IRecipient)** |

AddStopViewModel gebruikt `ObservableRecipient` voor messages:
```csharp
public class AddStopViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>
{
    public void Receive(TripSelectedMessage message)
    {
        CurrentTrip = message.Value;
    }
}
```

### Vraag 9: waarom FileSystem.AppDataDirectory voor foto's?

**Antwoord:**
**MAUI FileSystem API** heeft meerdere storage opties:

| Directory | Gebruik | Persistent? |
|-----------|---------|-------------|
| `AppDataDirectory` | App-specific data | Ja (tot uninstall) |
| `CacheDirectory` | Temp cache | Nee (kan verwijderd worden door OS) |
| `LocalDirectory` | iOS-specific | Platform-afhankelijk |

Voor foto's gebruik je `AppDataDirectory`:
- Persistent (blijft bij app updates)
- Private (andere apps kunnen niet bij)
- Verwijderd bij uninstall (geen orphaned files)

```csharp
var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
// Android: /data/data/com.companyname.triptracker/files/Photos/
```

### Vraag 10: waarom gPT-4o ipv GPT-4 vision?

**Antwoord:**
**GPT-4o** (o = omni) voordelen:
- **50% goedkoper** dan GPT-4 Vision
- **2x sneller** response tijd
- **Betere image understanding**
- Native multimodal (tekst + beeld in 1 model)
- Ondersteunt JSON mode

```csharp
public const string LLModel = "gpt-4o";
```

Voor TripTracker (reisfoto's analyseren) is GPT-4o perfect: snel, goedkoop, en nauwkeurig.

---

## 11. Testing checklist

### Foto capture
- [ ] Camera werkt op Android (fysiek device)
- [ ] Galerij werkt op Android
- [ ] Foto wordt gecomprimeerd bij > 4MB
- [ ] Foto preview verschijnt in UI
- [ ] PhotoData property wordt correct gezet

### GPS & geocoding
- [ ] GPS permission wordt gevraagd (eerste keer)
- [ ] GPS locatie wordt opgehaald (outdoor)
- [ ] Lat/Lng worden correct getoond (6 decimalen)
- [ ] Reverse geocoding werkt (Nominatim fallback)
- [ ] Adres en land worden correct getoond
- [ ] Fire-and-forget: UI blijft responsive tijdens GPS

### OpenAI vision
- [ ] AI analyse button verschijnt na foto
- [ ] Loading overlay tijdens analyse
- [ ] Titel wordt correct ingevuld
- [ ] Beschrijving wordt correct ingevuld
- [ ] Markdown stripping werkt (als OpenAI ` ``` ` toevoegt)
- [ ] Error handling: fallback bij API error

### Opslaan
- [ ] Save button disabled tot foto + titel aanwezig
- [ ] Foto wordt opgeslagen in AppDataDirectory/Photos/
- [ ] Unieke bestandsnaam (Guid)
- [ ] TripStop wordt correct gepost naar API
- [ ] Navigatie terug naar vorige pagina
- [ ] RefreshDataMessage wordt verstuurd

### Permissions
- [ ] CAMERA permission in AndroidManifest.xml
- [ ] LOCATION permissions in AndroidManifest.xml
- [ ] STORAGE/MEDIA permissions in AndroidManifest.xml
- [ ] Runtime permission requests werken

---

## 12. Troubleshooting

### GPS werkt niet

**Symptoom:** `GetCurrentLocationAsync()` returnt `null`

**Mogelijke oorzaken:**
1. **Permissions niet granted** → Check AndroidManifest.xml
2. **Indoor locatie** → GPS signaal te zwak, probeer outdoor
3. **Emulator** → Mock location moet enabled zijn
4. **Timeout (10 sec)** → Verhoog timeout of gebruik `GeolocationAccuracy.Low`

**Debug:**
```csharp
System.Diagnostics.Debug.WriteLine($"[GPS] Permission status: {status}");
System.Diagnostics.Debug.WriteLine($"[GPS] Location: {location?.Latitude}");
```

### OpenAI vision API errors

**Symptoom:** `AnalyzePhotoAsync()` returnt `null`

**Mogelijke oorzaken:**
1. **Ongeldige API key** → Check OpenAIKeys.cs
2. **Foto te groot** → PhotoService comprimeert automatisch tot 4MB
3. **Rate limit** → OpenAI free tier heeft limiet
4. **JSON parsing error** → Markdown stripping werkt niet

**Debug:**
```csharp
System.Diagnostics.Debug.WriteLine($"OpenAI response: {outputText}");
```

### Geocoding faalt

**Symptoom:** Address blijft leeg

**Mogelijke oorzaken:**
1. **Native geocoding faalt** → Normaal op Windows, fallback naar Nominatim
2. **Nominatim rate limit** → Max 1 req/sec
3. **Geen internet** → Check network permissions
4. **User-Agent header mist** → HttpClient init in constructor

**Debug:**
```csharp
System.Diagnostics.Debug.WriteLine($"[Geocoding] Native success: {placemark?.Locality}");
System.Diagnostics.Debug.WriteLine($"[Geocoding] Nominatim success: {response.Address.City}");
```

### Foto opslaan faalt

**Symptoom:** PhotoUrl is leeg na save

**Mogelijke oorzaken:**
1. **Storage permissions** → AndroidManifest.xml
2. **AppDataDirectory niet beschikbaar** → Platform-specifiek probleem
3. **PhotoData is null** → Foto niet correct geladen

**Debug:**
```csharp
System.Diagnostics.Debug.WriteLine($"Photo saved to: {filePath}");
System.Diagnostics.Debug.WriteLine($"PhotoData size: {PhotoData?.Length}");
```

---

## Conclusie

De **Smart Stop Capture** functionaliteit combineert 4 services (Photo, Geolocation, Geocoding, AI) in 1 naadloze UX flow:

1. **Foto**: Camera/Gallery → Compressie → Preview
2. **GPS**: Fire-and-forget → MainThread update
3. **Geocoding**: Coordinaten → Adres (Nominatim fallback)
4. **AI**: OpenAI Vision → Titel + Beschrijving
5. **Opslaan**: Lokaal bestand → API POST

**Key Takeaways:**
- Fire-and-forget voor GPS (geen blocking UI)
- MainThread voor UI updates vanuit background tasks
- Nominatim als gratis geocoding fallback
- OpenAI Vision API met JSON output + markdown stripping
- Lokale foto opslag in AppDataDirectory
- Permissions op MainThread (kritiek voor Android!)

**Examenvraag:** Leg de complete flow uit van foto maken tot opslaan in database, met alle services en threading aspecten.
