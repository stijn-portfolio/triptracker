---
fase: Overzicht
status: Voltooid
tags:
  - triptracker
  - maui
  - mvvm
  - openai
  - examen
created: 2025-12-20
---

# TripTracker - project overzicht

## Wat is TripTracker?

TripTracker is een **.NET MAUI** reis-app waarmee gebruikers hun reizen kunnen bijhouden. Per reis kunnen "stops" (tussenstops) worden toegevoegd met:
- Foto (camera of galerij)
- AI-gegenereerde titel en beschrijving (OpenAI Vision)
- GPS-locatie met automatische adres-lookup
- Handmatige aanpassingen

> [!info] Challenge 1 Vereisten
> Deze app voldoet aan alle vereisten voor Challenge 1:
> - MVVM met CommunityToolkit (code-behind LEEG)
> - OpenAI model integratie (Vision API)
> - .NET API met 2+ tabellen (Trips ↔ TripStops)
> - NavigationService + Messages
> - Ngrok hosting voor Android testing

---

## Architectuur

```
┌─────────────────────────────────────────────────────────────┐
│                    TripTracker.App (MAUI)                   │
├─────────────────────────────────────────────────────────────┤
│  Views/           │  ViewModels/        │  Services/        │
│  - TripsPage      │  - TripsViewModel   │  - ApiService     │
│  - TripDetailPage │  - TripDetailVM     │  - PhotoService   │
│  - AddTripPage    │  - AddTripVM        │  - GeolocationSvc │
│  - EditTripPage   │  - EditTripVM       │  - GeocodingService│
│  - AddStopPage    │  - AddStopVM        │  - AnalyzeImageSvc│
│  - StopDetailPage │  - StopDetailVM     │  - NavigationSvc  │
│  - EditStopPage   │  - EditStopVM       │  - PhotoImageSvc  │
│  - MapPage        │  - MapViewModel     │                   │
├─────────────────────────────────────────────────────────────┤
│  Models/          │  Messages/          │  Converters/      │
│  - Trip           │  - TripSelectedMsg  │  - InvertedBool   │
│  - TripStop       │  - RefreshTripsMsg  │  - YearSelected   │
│  - YearFilterItem │  - StopSelectedMsg  │  - YearSelectedTxt│
│                   │  - TripEditMessage  │                   │
│                   │  - StopEditMessage  │                   │
│                   │  - ShowStopsOnMapMsg│                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP (Ngrok)
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    TripTracker.API                          │
├─────────────────────────────────────────────────────────────┤
│  Controllers/     │  Services/          │  Entities/        │
│  - TripsController│  - TripRepository   │  - Trip           │
│  - TripStopsCtrl  │  - TripStopRepo     │  - TripStop       │
├─────────────────────────────────────────────────────────────┤
│  DbContexts/      │  MappingProfiles/   │  Models/ (DTOs)   │
│  - TripTrackerCtx │  - TripProfile      │  - TripDto        │
│                   │  - TripStopProfile  │  - TripStopDto    │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ Entity Framework
                              ▼
                    ┌─────────────────────┐
                    │   SQL Server        │
                    │   (LocalDB)         │
                    └─────────────────────┘
```

---

## Examen voorbereiding

> **[[STUDY-GUIDE]]** - Compact overzicht van alle concepten voor het examen (4-5 A4)

---

## Documentatie per fase

| Fase | Document | Onderwerp | Status |
|------|----------|-----------|--------|
| 1 | [[01-api-bouwen]] | API met Entity Framework, Repository Pattern, AutoMapper | ✅ |
| 2 | [[02-maui-setup]] | MAUI project structuur, DI, ObservableObject | ✅ |
| 3 | [[03-services-layer]] | ApiService, NavigationService, HttpClient | ✅ |
| 4 | [[04-viewmodels]] | MVVM, ObservableRecipient, Messaging | ✅ |
| 5 | [[05-views-navigatie]] | XAML, Shell navigatie, Data Binding | ✅ |
| 6+7 | [[06-07-smart-stop-capture]] | OpenAI Vision, GPS, Geocoding, Camera | ✅ |
| 9 | [[09-ui-polish]] | Bug fixes, kleuren, datum formats | ✅ |
| 10 | [[10-add-trip-page]] | AddTripPage met foto picker | ✅ |
| 11 | [[11-trip-delete-edit]] | SwipeView Delete/Edit voor trips en stops | ✅ |
| 12 | [[12-ui-cleanup]] | .NET 8 downgrade, camera fix, UI polish | ✅ |
| 13 | [[13-mappage]] | Mapsui, OpenStreetMap integratie, ThemeStyle | ✅ |
| 14 | [[14-stop-editing]] | Forward geocoding, DataTrigger, AI Describe | ✅ |
| 15 | [[15-year-filter]] | BindableLayout, LINQ filtering, DataTrigger | ✅ |
| 16 | [[16-utilities-converters]] | PhotoService, IValueConverter | ✅ |

---

## Cursus referenties

| TripTracker Concept | Cursus Les | SafariSnap Equivalent |
|---------------------|------------|----------------------|
| API structuur | Les 1 - API Development | Safari.API |
| MVVM pattern | Les 2 - MAUI & MVVM | SafariSnap basis |
| DataServices | Les 3 - DataServices | ApiService, ListViewModel |
| OpenAI Vision | Les 3 - OpenAI integratie | AnalyzeImageService |
| Geolocation | Les 3 - Device features | GeolocationService |
| IValueConverter | Les 2 - Data Binding | Geen (extra toevoeging) |
| Mapsui kaart | Externe library | Geen SafariSnap equivalent |

---

## Belangrijke patronen

### 1. MVVM met CommunityToolkit

```csharp
// Model: ObservableObject voor property change
public class Trip : ObservableObject
{
    private string name;
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }
}

// ViewModel: ObservableRecipient voor messaging
public class TripsViewModel : ObservableRecipient, IRecipient<RefreshDataMessage>
{
    public void Receive(RefreshDataMessage message) { ... }
}
```

### 2. Generieke ApiService

```csharp
// Base class voor alle API calls
public abstract class ApiService<T> : IApiService<T>
{
    protected abstract string Endpoint { get; }

    public async Task<IEnumerable<T>> GetAllAsync() { ... }
    public async Task<T?> GetAsync(int id) { ... }
    public async Task PostAsync(T entity) { ... }
}
```

### 3. Messaging tussen ViewModels

```csharp
// Versturen
WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));

// Ontvangen (via IRecipient<T>)
Messenger.Register<ViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));
```

---

## Quick start

### API starten
```bash
cd TripTracker.API
dotnet run
```

### Ngrok voor Android
```bash
ngrok http 5206
# Update BASE_URL in ApiService.cs met Ngrok URL
```

### App starten
```bash
cd TripTracker.App
dotnet build -t:Run -f net8.0-android
```

> [!warning] .NET 8 (niet .NET 9!)
> TripTracker gebruikt .NET 8 vanwege een bekende MAUI bug met camera op Android in .NET 9. Zie [[12-ui-cleanup]] voor details.

---

## Examenvragen overzicht

> [!tip] Veelgestelde vragen
> 1. **Waarom MVVM?** - Separation of concerns, testbaarheid, code-behind leeg
> 2. **Waarom interfaces voor ViewModels?** - DI, mockbaarheid, loose coupling
> 3. **Waarom WeakReferenceMessenger?** - Voorkomt memory leaks, loose coupling
> 4. **Waarom generieke ApiService?** - DRY principe, herbruikbaarheid
> 5. **Waarom Repository Pattern?** - Abstractie van data access, testbaarheid

---

## Project locaties

- **App code:** `C:\Dev\TM\AIDOTNET\TripTracker\TripTracker.App\`
- **API code:** `C:\Dev\TM\AIDOTNET\TripTracker\TripTracker.API\`
- **Documentatie:** `C:\Dev\TM\AIDOTNET\TripTracker\docs\`
- **Cursusmateriaal:** `C:\Users\user\Koofr\Thomas More\23 AIDOTNET\`
