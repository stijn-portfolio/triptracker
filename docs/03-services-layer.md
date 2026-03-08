---
fase: 3
title: Services Layer - API & Navigation
status: completed
tags:
  - services
  - api
  - httpclient
  - navigation
  - dependency-injection
created: 2025-12-20
---

# Fase 3: Services Layer - API & Navigation

## Overzicht

De Services Layer implementeert het **Repository Pattern** en **Separation of Concerns** principe. Deze laag scheidt de business logic (ViewModels) van de data access logic (API calls) en navigatie logica.

### Waarom een Services Layer?

1. **Separation of Concerns**: ViewModels hoeven niet te weten HOE data wordt opgehaald
2. **Testbaarheid**: Services kunnen gemakkelijk gemockt worden voor unit tests
3. **Herbruikbaarheid**: Dezelfde service kan in meerdere ViewModels gebruikt worden
4. **Onderhoudbaarheid**: API endpoints en navigatie logica op één plek
5. **DRY Principe**: Geen duplicatie van HttpClient code

> [!info] Cursus Referentie
> Deze implementatie volgt **Les 3 - DataServices** uit de cursus, zoals gedemonstreerd in het SafariSnap voorbeeld.

---

## Architectuur Overzicht

```
┌─────────────────────────────────────────────────────────────┐
│                      ViewModels                              │
│  (Business Logic - weet NIET hoe data wordt opgehaald)      │
└───────────────────┬─────────────────────────────────────────┘
                    │ inject via constructor
                    ↓
┌─────────────────────────────────────────────────────────────┐
│                   Services Layer                             │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  DATA SERVICES (API calls)                          │    │
│  │  IApiService<T> / ApiService<T>                     │    │
│  │  ITripDataService / TripDataService                 │    │
│  │  ITripStopDataService / TripStopDataService         │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  NAVIGATIE                                          │    │
│  │  INavigationService / NavigationService             │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  LOCATIE & GEOCODING                                │    │
│  │  IGeolocationService / GeolocationService (GPS)     │    │
│  │  IGeocodingService / GeocodingService (adres⟷coords)│    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  FOTO & AI                                          │    │
│  │  IPhotoService / PhotoService (camera/gallery)      │    │
│  │  IAnalyzeImageService / AnalyzeImageService (OpenAI)│    │
│  │  PhotoService (capture/pick + resize)               │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
└───────────────────┬─────────────────────────────────────────┘
                    │
        ┌───────────┼───────────┬─────────────┐
        ↓           ↓           ↓             ↓
┌───────────┐ ┌───────────┐ ┌────────┐ ┌───────────┐
│  REST API │ │   MAUI    │ │  MAUI  │ │  OpenAI   │
│  Backend  │ │Navigation │ │  GPS   │ │    API    │
└───────────┘ └───────────┘ └────────┘ └───────────┘
```

### File Structuur

```
TripTracker.App/
├── Services/                        ← Interfaces + Implementaties
│   ├── IApiService.cs
│   ├── ApiService.cs                ← Abstract base
│   ├── ITripDataService.cs          ← Interface voor Trip API
│   ├── TripDataService.cs           ← Erft van ApiService<Trip>
│   ├── ITripStopDataService.cs      ← Interface voor TripStop API
│   ├── TripStopDataService.cs       ← Erft van ApiService<TripStop>
│   ├── INavigationService.cs
│   ├── NavigationService.cs
│   ├── IGeolocationService.cs
│   ├── GeolocationService.cs
│   ├── IGeocodingService.cs
│   ├── GeocodingService.cs
│   ├── IPhotoService.cs
│   ├── PhotoService.cs          ← inclusief resize (was PhotoImageService)
│   ├── IAnalyzeImageService.cs
│   ├── AnalyzeImageService.cs
│   └── OpenAIKeys.cs
│
├── Models/                          ← Alleen data models
│   ├── Trip.cs
│   └── TripStop.cs
```

> [!info] Documentatie per Service
> Dit document behandelt **ApiService** en **NavigationService**.
> De overige services worden besproken waar ze geïntroduceerd worden:
>
> | Service | Zie document |
> |---------|--------------|
> | PhotoService, GeolocationService, GeocodingService, AnalyzeImageService | [[06-07-smart-stop-capture]] |
> | PhotoService (resize) | Geïntegreerd in PhotoService |

---

## 1. Generieke API Service Interface

### IApiService&lt;T&gt;

Generieke interface die de basis CRUD operaties definieert voor elk type entity.

**Locatie**: `Services/IApiService.cs`

```csharp
namespace TripTracker.App.Services
{
    // Generieke interface voor API communicatie
    // Zoals in SafariSnap (Les 3 - DataServices)
    public interface IApiService<T>
    {
        Task<T> GetAsync(int id);
        Task<List<T>> GetAllAsync();
        Task PostAsync(T data);
        Task PutAsync(int id, T data);
        Task DeleteAsync(int id);
    }
}
```

> [!tip] Examenvraag
> **Vraag**: Waarom gebruiken we een generieke interface `IApiService<T>` in plaats van aparte interfaces voor elke entity?
>
> **Antwoord**:
> - **Code hergebruik**: Dezelfde CRUD operaties voor alle entities
> - **Type safety**: Compiler checkt dat je het juiste type gebruikt
> - **DRY principe**: Geen duplicatie van interface definities
> - **Consistentie**: Alle DataServices hebben dezelfde methode signaturen

### Voordelen Generieke Interface

| Voordeel | Uitleg |
|----------|--------|
| **Type Safety** | `IApiService<Trip>` kan geen `TripStop` objecten returnen |
| **Intellisense** | IDE weet precies welk type wordt verwacht |
| **Minder code** | Eén interface voor alle entities |
| **Consistentie** | Alle services hebben dezelfde methodes |

---

## 2. Abstract Base Class - ApiService&lt;T&gt;

### Implementatie

**Locatie**: `Services/ApiService.cs`

> [!info] Wat is JSON Serialization?
> API's communiceren via **JSON tekst**. Wij werken met **C# objecten**.
>
> ```
> ┌─────────────────┐                      ┌─────────────────┐
> │   C# Object     │                      │   JSON Tekst    │
> │   Trip { }      │  ←── Deserialize ──  │   {"id": 1}     │
> │                 │  ─── Serialize ───►  │                 │
> └─────────────────┘                      └─────────────────┘
> ```
>
> | Richting | Methode | Wanneer? |
> |----------|---------|----------|
> | JSON → Object | `JsonConvert.DeserializeObject<T>()` | GET response lezen |
> | Object → JSON | `PostAsJsonAsync()` | POST/PUT data sturen |
>
> **Package:** `Newtonsoft.Json` - meest populaire JSON library voor .NET

```csharp
using Newtonsoft.Json;          // Voor DeserializeObject
using System.Net.Http.Json;     // Voor PostAsJsonAsync, PutAsJsonAsync

namespace TripTracker.App.Services
{
    // Abstract base class voor API communicatie
    // Zoals in SafariSnap (Les 3 - DataServices)
    public abstract class ApiService<T> : IApiService<T>
    {
        // Base URL voor de API
        // Ngrok URL voor Android toegang (localhost werkt niet op telefoon)
        // Lokaal testen op Windows: gebruik "https://localhost:7162/api"
        protected static readonly string BASE_URL =
            "https://mao-subtympanistic-pauletta.ngrok-free.dev/api";

        // Statische HttpClient met timeout (hergebruik voor performance)
        protected static readonly HttpClient client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        // Abstract property - elke specifieke service definieert zijn eigen endpoint
        protected abstract string EndPoint { get; }

        public virtual async Task<T> GetAsync(int id)
        {
            var response = await client.GetAsync($"{BASE_URL}/{EndPoint}/{id}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonData)!;
            }
            throw new Exception(
                $"GetAsync request failed with status code {response.StatusCode}"
            );
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            var response = await client.GetAsync($"{BASE_URL}/{EndPoint}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(jsonData)!;
            }
            throw new Exception(
                $"GetAllAsync request failed with status code {response.StatusCode}"
            );
        }

        public virtual async Task PostAsync(T data)
        {
            var response = await client.PostAsJsonAsync($"{BASE_URL}/{EndPoint}", data);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception(
                    $"PostAsync request failed with status code {response.StatusCode}"
                );
            }
        }

        public virtual async Task PutAsync(int id, T data)
        {
            var response = await client.PutAsJsonAsync(
                $"{BASE_URL}/{EndPoint}/{id}", data
            );
            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception(
                    $"PutAsync request failed with status code {response.StatusCode}"
                );
            }
        }

        public virtual async Task DeleteAsync(int id)
        {
            var response = await client.DeleteAsync($"{BASE_URL}/{EndPoint}/{id}");
            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception(
                    $"DeleteAsync request failed with status code {response.StatusCode}"
                );
            }
        }
    }
}
```

### Design Beslissingen

> [!tip] Examenvraag: Wat betekent `static` en `abstract`?
>
> **`static`** = Eén gedeelde instantie voor de hele applicatie
> ```csharp
> protected static readonly HttpClient client = new();
> //        ^^^^^^
> // Alle TripDataService EN TripStopDataService delen DEZELFDE HttpClient
> // → Efficiënt, geen nieuwe connectie per request
> ```
>
> **`abstract`** = "Kindklasse MOET dit invullen"
> ```csharp
> protected abstract string EndPoint { get; }
> //        ^^^^^^^^
> // ApiService zegt: "Ik weet niet welke endpoint, jij moet het zeggen"
> // TripDataService zegt: EndPoint => "trips"
> // TripStopDataService zegt: EndPoint => "tripstops"
> ```

> [!warning] Waarom `static readonly HttpClient`?
> - **Performance**: HttpClient hergebruiken = minder overhead
> - **Socket Exhaustion**: Voorkomen dat er te veel sockets worden aangemaakt
> - **Best Practice**: Microsoft raadt aan om HttpClient te hergebruiken
> - **Thread Safety**: HttpClient is thread-safe voor concurrent gebruik

> [!info] Waarom `abstract string EndPoint`?
> - Elke concrete service definieert zijn eigen API endpoint
> - `TripDataService` → `"trips"`
> - `TripStopDataService` → `"tripstops"`
> - Base class kan de endpoint gebruiken in alle CRUD methodes

### HTTP Status Codes

| Methode | Verwachte Status Code | Betekenis |
|---------|----------------------|-----------|
| `GetAsync()` | 200 OK | Data succesvol opgehaald |
| `GetAllAsync()` | 200 OK | Lijst succesvol opgehaald |
| `PostAsync()` | 201 Created | Resource succesvol aangemaakt |
| `PutAsync()` | 204 No Content | Resource succesvol geüpdatet |
| `DeleteAsync()` | 204 No Content | Resource succesvol verwijderd |

> [!tip] Examenvraag
> **Vraag**: Waarom checken we op specifieke HTTP status codes in plaats van alleen `IsSuccessStatusCode`?
>
> **Antwoord**:
> - **Strikte validatie**: We verwachten EXACT 201 voor POST, niet 200
> - **API contract**: De API moet aan de REST standaard voldoen
> - **Debugging**: Makkelijker om problemen te identificeren
> - **Best practice**: RESTful APIs gebruiken specifieke status codes

---

## 3. Concrete DataServices

### ITripDataService Interface

**Locatie**: `Services/ITripDataService.cs`

```csharp
using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    // Interface voor Trip-specifieke API operaties
    // Erft alle CRUD operaties van IApiService<Trip>
    // Voegt GetTripStopsAsync toe voor nested resource
    public interface ITripDataService : IApiService<Trip>
    {
        Task<List<TripStop>> GetTripStopsAsync(int tripId);
    }
}
```

### TripDataService

**Locatie**: `Services/TripDataService.cs`

```csharp
using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    // Concrete implementatie van ApiService voor Trip
    // Implementeert ITripDataService voor dependency injection
    public class TripDataService : ApiService<Trip>, ITripDataService
    {
        protected override string EndPoint => "trips";

        // Extra methode om TripStops voor een specifieke Trip op te halen
        public async Task<List<TripStop>> GetTripStopsAsync(int tripId)
        {
            var response = await client.GetAsync(
                $"{BASE_URL}/{EndPoint}/{tripId}/tripstops"
            );
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<TripStop>>(jsonData)!;
            }
            throw new Exception(
                $"GetTripStopsAsync request failed with status code {response.StatusCode}"
            );
        }
    }
}
```

> [!info] Nested Resource
> `GetTripStopsAsync()` haalt TripStops op via de Trip resource:
> - **Endpoint**: `/api/trips/{tripId}/tripstops`
> - **RESTful pattern**: Nested resources onder parent resource
> - **API Controller**: `TripsController.GetTripStopsAsync()`

### ITripStopDataService Interface

**Locatie**: `Services/ITripStopDataService.cs`

```csharp
using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    // Interface voor TripStop-specifieke API operaties
    // Erft alle CRUD operaties van IApiService<TripStop>
    public interface ITripStopDataService : IApiService<TripStop>
    {
    }
}
```

### TripStopDataService

**Locatie**: `Services/TripStopDataService.cs`

```csharp
using TripTracker.App.Models;

namespace TripTracker.App.Services
{
    // Concrete implementatie van ApiService voor TripStop
    // Implementeert ITripStopDataService voor dependency injection
    public class TripStopDataService : ApiService<TripStop>, ITripStopDataService
    {
        protected override string EndPoint => "tripstops";
    }
}
```

> [!tip] Waarom interfaces voor DataServices?
> **Testbaarheid**: DataServices kunnen gemockt worden in unit tests
> **DI Pattern**: Consistent met andere services (NavigationService, PhotoService, etc.)
> **Loose Coupling**: ViewModels kennen alleen de interface, niet de concrete implementatie

### API Endpoints Mapping

| DataService | EndPoint | API Controller | Base URL |
|-------------|----------|---------------|----------|
| `TripDataService` | `"trips"` | `TripsController` | `/api/trips` |
| `TripStopDataService` | `"tripstops"` | `TripStopsController` | `/api/tripstops` |

**Voorbeeld API Calls (via geïnjecteerde service)**:

```csharp
// In ViewModel constructor:
private readonly ITripDataService _tripDataService;
private readonly ITripStopDataService _tripStopDataService;

public MyViewModel(ITripDataService tripDataService, ITripStopDataService tripStopDataService)
{
    _tripDataService = tripDataService;
    _tripStopDataService = tripStopDataService;
}

// GET /api/trips
var trips = await _tripDataService.GetAllAsync();

// GET /api/trips/5
var trip = await _tripDataService.GetAsync(5);

// GET /api/trips/5/tripstops
var stops = await _tripDataService.GetTripStopsAsync(5);

// POST /api/tripstops
var newStop = new TripStop { Title = "Paris", ... };
await _tripStopDataService.PostAsync(newStop);

// PUT /api/tripstops/10
var updatedStop = new TripStop { Id = 10, Title = "Paris Edited", ... };
await _tripStopDataService.PutAsync(10, updatedStop);

// DELETE /api/tripstops/10
await _tripStopDataService.DeleteAsync(10);
```

---

## 4. Navigation Service

### INavigationService Interface

**Locatie**: `Services/INavigationService.cs`

```csharp
namespace TripTracker.App.Services
{
    // Interface voor navigatie tussen pagina's
    // Zoals in SafariSnap (Les 3)
    public interface INavigationService
    {
        Task NavigateToTripDetailPageAsync();
        Task NavigateToAddStopPageAsync();
        Task NavigateToStopDetailPageAsync();
        Task NavigateBackAsync();
    }
}
```

### NavigationService Implementatie

**Locatie**: `Services/NavigationService.cs`

```csharp
using TripTracker.App.Views;

namespace TripTracker.App.Services
{
    // Implementatie van NavigationService
    // Inject IServiceProvider om pages via DI op te halen
    // Zoals in SafariSnap (Les 3)
    public class NavigationService : INavigationService
    {
        private INavigation _navigation;
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _navigation = Application.Current!.MainPage!.Navigation;
        }

        public async Task NavigateToTripDetailPageAsync()
        {
            await _navigation.PushAsync(
                _serviceProvider.GetRequiredService<TripDetailPage>()
            );
        }

        public async Task NavigateToAddStopPageAsync()
        {
            await _navigation.PushAsync(
                _serviceProvider.GetRequiredService<AddStopPage>()
            );
        }

        public async Task NavigateToStopDetailPageAsync()
        {
            await _navigation.PushAsync(
                _serviceProvider.GetRequiredService<StopDetailPage>()
            );
        }

        public async Task NavigateBackAsync()
        {
            if (_navigation.NavigationStack.Count > 1)
            {
                await _navigation.PopAsync();
            }
            else
            {
                throw new InvalidOperationException("No pages to navigate back to!");
            }
        }
    }
}
```

### Wat is IServiceProvider?

> [!tip] Examenvraag: Wat is IServiceProvider?
> `IServiceProvider` is een **ingebouwde .NET interface** - de centrale DI container die alle geregistreerde services bevat.
>
> | Vraag | Antwoord |
> |-------|----------|
> | Waar komt het vandaan? | Ingebouwd in .NET (`System` namespace) |
> | NuGet nodig? | Nee, standaard beschikbaar |
> | Wie maakt het aan? | MAUI via `builder.Build()` |
> | Wie injecteert het? | MAUI automatisch in constructors |

**Hoe het werkt:**

```
┌─────────────────────────────────────────────────────┐
│  MauiProgram.cs - Registreren                       │
├─────────────────────────────────────────────────────┤
│  builder.Services.AddSingleton<TripsPage>();        │
│  builder.Services.AddTransient<TripDetailPage>();   │
│                    │                                │
│                    ▼                                │
│           ┌───────────────────┐                     │
│           │  IServiceProvider │  ← Container        │
│           │  (alle services)  │                     │
│           └───────────────────┘                     │
└─────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│  NavigationService - Ophalen                        │
├─────────────────────────────────────────────────────┤
│  _serviceProvider.GetRequiredService<Page>();       │
│  → Container maakt Page + alle dependencies!        │
└─────────────────────────────────────────────────────┘
```

### Waarom IServiceProvider in NavigationService?

> [!info] Dependency Injection Pattern
> **IServiceProvider** wordt gebruikt om Pages op te halen uit de DI container:
>
> 1. **Pages zijn geregistreerd in MauiProgram.cs**
> 2. **`GetRequiredService<T>()`** haalt de Page op met alle dependencies
> 3. **Nieuwe instantie per navigatie** (bij Transient registration)
> 4. **Automatische dependency injection** in Page constructor

**Zonder DI (OLD WAY)**:
```csharp
// ❌ FOUT - Dependencies moeten manueel aangemaakt worden
await Navigation.PushAsync(new TripDetailPage(new TripDetailViewModel()));
```

**Met DI (CORRECT WAY)**:
```csharp
// ✅ CORRECT - DI container maakt Page + alle dependencies
await _navigation.PushAsync(
    _serviceProvider.GetRequiredService<TripDetailPage>()
);
```

> [!tip] Ezelsbruggetje
> | Begrip | Analogie |
> |--------|----------|
> | `IServiceProvider` | Magazijn met alle onderdelen |
> | `GetRequiredService<T>()` | "Geef mij een T, volledig gemonteerd" |
> | Registratie in MauiProgram | Catalogus: "Dit hebben we op voorraad" |

### Navigation Stack

> [!warning] Navigation Stack Validatie
> `NavigateBackAsync()` checkt of er pagina's in de stack zitten:
> - **Count > 1**: Er is een previous page → PopAsync()
> - **Count = 1**: We zitten op de root page → Exception
> - **Waarom exception?**: Voorkomen dat de app crasht bij invalid navigatie

**MAUI Navigation Stack**:
```
┌─────────────────────────┐
│ StopDetailPage          │ ← Current page
├─────────────────────────┤
│ AddStopPage             │
├─────────────────────────┤
│ TripDetailPage          │
├─────────────────────────┤
│ TripsPage (Root)        │
└─────────────────────────┘
```

> [!tip] Examenvraag
> **Vraag**: Waarom gebruiken we een NavigationService in plaats van direct `Navigation.PushAsync()` in ViewModels?
>
> **Antwoord**:
> - **Testbaarheid**: NavigationService kan gemockt worden in unit tests
> - **Separation of Concerns**: ViewModel weet niet HOE navigatie werkt
> - **Dependency Injection**: Pages worden via DI opgehaald met alle dependencies
> - **Centralisatie**: Alle navigatie logica op één plek
> - **MVVM principe**: ViewModel mag geen referentie hebben naar Views

---

## 5. Dependency Injection Registratie

### MauiProgram.cs

**Locatie**: `MauiProgram.cs`

```csharp
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TripTracker.App.Services;
using TripTracker.App.ViewModels;
using TripTracker.App.Views;

namespace TripTracker.App;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ===== Services registreren =====

        // Navigation service (Transient = nieuwe instantie per request)
        builder.Services.AddTransient<INavigationService, NavigationService>();

        // Data Services (API communicatie)
        // Singleton = hergebruik dezelfde instantie (HttpClient wordt hergebruikt)
        builder.Services.AddSingleton<ITripDataService, TripDataService>();
        builder.Services.AddSingleton<ITripStopDataService, TripStopDataService>();

        // Fase 6+7: Smart Stop Capture services
        // Singleton = hergebruik dezelfde instantie
        builder.Services.AddSingleton<IPhotoService, PhotoService>();
        builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
        builder.Services.AddSingleton<IGeocodingService, GeocodingService>();
        builder.Services.AddSingleton<IAnalyzeImageService, AnalyzeImageService>();

        // ===== Pages en ViewModels registreren =====
        // Singleton = dezelfde instantie hergebruiken (voor hoofdpagina's)
        // Transient = nieuwe instantie per navigatie (voor detail pagina's)

        // Trips pagina (hoofdpagina - Singleton)
        builder.Services.AddSingleton<TripsPage>();
        builder.Services.AddSingleton<ITripsViewModel, TripsViewModel>();

        // Trip detail pagina (Transient - nieuwe instantie bij elke navigatie)
        builder.Services.AddTransient<TripDetailPage>();
        builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();

        // Add stop pagina (Transient)
        builder.Services.AddTransient<AddStopPage>();
        builder.Services.AddTransient<IAddStopViewModel, AddStopViewModel>();

        // Stop detail pagina (Transient)
        builder.Services.AddTransient<StopDetailPage>();
        builder.Services.AddTransient<IStopDetailViewModel, StopDetailViewModel>();

        return builder.Build();
    }
}
```

### Service Lifetimes

| Lifetime | Betekenis | Gebruik voor | Voorbeeld |
|----------|-----------|--------------|-----------|
| **Singleton** | Eén instantie voor hele app | Services die state bewaren, root pages | `TripsPage`, `PhotoService` |
| **Transient** | Nieuwe instantie bij elke request | Detail pages, ViewModels | `TripDetailPage`, `NavigationService` |
| **Scoped** | Eén instantie per scope | Weinig gebruikt in MAUI | - |

> [!warning] Singleton vs Transient voor Pages
>
> **Singleton** (hoofdpagina's):
> - `TripsPage`: Eén instantie, altijd in de navigation stack
> - State wordt bewaard tussen navigaties
> - Performance voordeel (geen reconstructie)
>
> **Transient** (detail pagina's):
> - `TripDetailPage`: Nieuwe instantie bij elke navigatie
> - Frisse state bij elke keer openen
> - ViewModel wordt ook opnieuw aangemaakt

### DI in ViewModels

**Voorbeeld**: TripsViewModel (exact zoals in TripTracker)

```csharp
public class TripsViewModel : ObservableRecipient, IRecipient<RefreshDataMessage>, ITripsViewModel
{
    private readonly INavigationService _navigationService;
    private readonly ITripDataService _tripDataService;

    public TripsViewModel(
        INavigationService navigationService,
        ITripDataService tripDataService)
    {
        _navigationService = navigationService;
        _tripDataService = tripDataService;

        // Registreer voor messages (Messenger komt van ObservableRecipient)
        Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

        LoadTripsAsync();
        BindCommands();
    }

    // DataService via constructor injection - consistent DI pattern
    private async Task LoadTripsAsync()
    {
        var tripList = await _tripDataService.GetAllAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Trips.Clear();
            foreach (var trip in tripList)
            {
                Trips.Add(trip);
            }
        });
    }

    private async Task GoToTripDetail(Trip? trip)
    {
        if (trip != null)
        {
            await _navigationService.NavigateToTripDetailPageAsync();
            WeakReferenceMessenger.Default.Send(new TripSelectedMessage(trip));
        }
    }
}
```

> [!info] DI Pattern: Alles via Constructor Injection
>
> | Service | Geïnjecteerd? | Waarom? |
> |---------|---------------|---------|
> | `INavigationService` | ✅ Ja | Heeft `IServiceProvider` nodig voor pages |
> | `ITripDataService` | ✅ Ja | API calls voor Trip data |
> | `ITripStopDataService` | ✅ Ja (waar nodig) | API calls voor TripStop data |
>
> **Consistent Pattern:**
> ```csharp
> // Alle services via constructor - testbaar en consistent!
> public TripsViewModel(INavigationService nav, ITripDataService tripData)
> ```

> [!tip] Examenvraag
> **Vraag**: Wat is het verschil tussen Singleton en Transient lifetime in Dependency Injection?
>
> **Antwoord**:
> - **Singleton**: Eén instantie voor de hele applicatie. Hergebruik van dezelfde instance bij elke inject. Gebruik voor services die state bewaren of duur zijn om aan te maken.
> - **Transient**: Nieuwe instantie bij elke inject. Geen state tussen requests. Gebruik voor lightweight services en pages die telkens verse data moeten tonen.
> - **Performance**: Singleton = beter, Transient = meer overhead maar verse state
> - **Memory**: Singleton = constant geheugen, Transient = wordt garbage collected

---

## 6. Usage in ViewModels

### Voorbeeld: TripDetailViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TripTracker.App.Messages;
using TripTracker.App.Models;
using TripTracker.App.Services;

namespace TripTracker.App.ViewModels
{
    public class TripDetailViewModel : ObservableRecipient,
        IRecipient<TripSelectedMessage>,
        IRecipient<RefreshDataMessage>,
        ITripDetailViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ITripDataService _tripDataService;
        private readonly ITripStopDataService _tripStopDataService;

        private Trip? currentTrip;
        public Trip? CurrentTrip
        {
            get => currentTrip;
            set => SetProperty(ref currentTrip, value);
        }

        public TripDetailViewModel(
            INavigationService navigationService,
            ITripDataService tripDataService,
            ITripStopDataService tripStopDataService)
        {
            _navigationService = navigationService;
            _tripDataService = tripDataService;
            _tripStopDataService = tripStopDataService;

            // Registreer voor messages (type-safe pattern)
            Messenger.Register<TripDetailViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));
            Messenger.Register<TripDetailViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));
        }

        public void Receive(TripSelectedMessage message)
        {
            CurrentTrip = message.Value;
            _ = LoadTripStopsAsync();
        }

        public void Receive(RefreshDataMessage message)
        {
            _ = LoadTripStopsAsync();
        }

        private async Task LoadTripStopsAsync()
        {
            if (CurrentTrip == null) return;

            try
            {
                // Via geïnjecteerde service - consistent DI pattern
                var stops = await _tripDataService.GetTripStopsAsync(CurrentTrip.Id);

                // UI updates op MainThread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TripStops = new ObservableCollection<TripStop>(stops);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stops: {ex.Message}");
            }
        }
    }
}
```

### Data Flow

```
User taps Trip in TripsPage
        ↓
TripsViewModel sends TripSelectedMessage
        ↓
TripDetailViewModel receives message
        ↓
LoadTripDetailAsync(tripId)
        ↓
_tripDataService.GetAsync(tripId)
        ↓
HTTP GET /api/trips/{tripId}
        ↓
API returns TripDto
        ↓
Deserialize to Trip model
        ↓
_tripDataService.GetTripStopsAsync(tripId)
        ↓
HTTP GET /api/trips/{tripId}/tripstops
        ↓
API returns List<TripStopDto>
        ↓
Deserialize to List<TripStop>
        ↓
Update SelectedTrip.TripStops
        ↓
UI updates via data binding
```

---

## 7. Ngrok voor Android Testing

### Probleem: Localhost werkt niet op Android

Android emulator en fysieke devices kunnen **niet** verbinden met `localhost` of `https://localhost:7162`.

### Oplossing: Ngrok

**Ngrok** maakt een publieke URL die doorverwijst naar je lokale API:

```bash
# Start API lokaal
cd TripTracker.API
dotnet run

# In andere terminal: Start ngrok
ngrok http https://localhost:7162
```

**Output**:
```
Forwarding   https://mao-subtympanistic-pauletta.ngrok-free.dev -> https://localhost:7162
```

### Update BASE_URL in ApiService

```csharp
// Voor Android/iOS testing
protected static readonly string BASE_URL =
    "https://mao-subtympanistic-pauletta.ngrok-free.dev/api";

// Voor Windows lokaal testen
// protected static readonly string BASE_URL = "https://localhost:7162/api";
```

> [!warning] Ngrok Free Tier
> - URL verandert bij elke herstart van ngrok
> - Update `BASE_URL` telkens je ngrok herstart
> - Voor productie: gebruik Azure hosting

---

## 8. Error Handling Best Practices

### Try-Catch in ViewModels

```csharp
[RelayCommand]
private async Task LoadTrips()
{
    try
    {
        IsBusy = true;
        var trips = await _tripDataService.GetAllAsync();
        Trips = new ObservableCollection<Trip>(trips);
    }
    catch (HttpRequestException httpEx)
    {
        // Network error
        await ShowError("Network Error",
            "Could not connect to server. Check your internet connection.");
    }
    catch (JsonException jsonEx)
    {
        // Deserialization error
        await ShowError("Data Error",
            "Received invalid data from server.");
    }
    catch (Exception ex)
    {
        // Generic error
        await ShowError("Error", $"An error occurred: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task ShowError(string title, string message)
{
    await Application.Current!.MainPage!.DisplayAlert(title, message, "OK");
}
```

### Custom Exception Types

Voor grotere apps:

```csharp
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

// In ApiService
if (!response.IsSuccessStatusCode)
{
    throw new ApiException(
        response.StatusCode,
        $"API request failed: {response.ReasonPhrase}"
    );
}
```

---

## Examenvragen

### Vraag 1: Services Layer Architectuur

**Vraag**: Leg uit waarom we een Services Layer gebruiken tussen ViewModels en de API. Wat zijn de voordelen?

**Antwoord**:
1. **Separation of Concerns**: ViewModels weten niet HOE data wordt opgehaald (HTTP, database, etc.)
2. **Testbaarheid**: Services kunnen gemockt worden voor unit tests zonder echte API calls
3. **Herbruikbaarheid**: Dezelfde service kan in meerdere ViewModels gebruikt worden
4. **Onderhoudbaarheid**: API endpoints wijzigen = alleen aanpassen in service, niet in alle ViewModels
5. **DRY principe**: HttpClient code staat op één plek (ApiService base class)
6. **Centralisatie**: Error handling en logging kan centraal in de service

---

### Vraag 2: Generieke Interface vs Concrete Interface

**Vraag**: Wat is het verschil tussen `IApiService<T>` (generiek) en aparte interfaces zoals `ITripService`, `ITripStopService`? Wat zijn de voordelen van de generieke aanpak?

**Antwoord**:

**Generieke aanpak** (`IApiService<T>`):
- **Voordelen**:
  - Minder code duplication (één interface voor alle entities)
  - Type safety (compiler checkt types)
  - Consistente API voor alle services
  - Gemakkelijk nieuwe entities toevoegen
- **Nadelen**:
  - Minder flexibel voor entity-specifieke methodes
  - Alle entities moeten dezelfde CRUD operaties ondersteunen

**Concrete interfaces** (`ITripService`, `ITripStopService`):
- **Voordelen**:
  - Meer flexibiliteit per entity
  - Duidelijkere naming (geen generics)
  - Entity-specifieke methodes mogelijk
- **Nadelen**:
  - Veel code duplication
  - Meer interfaces om te onderhouden

**Beste aanpak**: Generieke base interface + entity-specifieke extensies

```csharp
// Basis CRUD
public class TripDataService : ApiService<Trip> { }

// + Extra methodes
public async Task<List<TripStop>> GetTripStopsAsync(int tripId) { ... }
```

---

### Vraag 3: HttpClient Best Practices

**Vraag**: Waarom gebruiken we een `static readonly HttpClient` in de ApiService base class? Wat zijn de voordelen en risico's?

**Antwoord**:

**Voordelen**:
1. **Performance**: HttpClient hergebruiken is veel sneller dan telkens nieuw aanmaken
2. **Socket Exhaustion Prevention**: Voorkomen dat te veel sockets worden aangemaakt
3. **Connection Pooling**: HttpClient hergebruikt TCP verbindingen
4. **Memory**: Minder garbage collection

**Risico's**:
1. **DNS Changes**: Static HttpClient respecteert geen DNS wijzigingen
2. **Thread Safety**: Moet thread-safe zijn (HttpClient IS thread-safe)

**Best Practice** (volgens Microsoft):
- Gebruik `IHttpClientFactory` in productie apps
- In MAUI apps: static HttpClient is acceptabel voor eenvoudige scenarios
- Zet timeout om long-running requests te voorkomen

```csharp
// ✅ CORRECT
protected static readonly HttpClient client = new HttpClient()
{
    Timeout = TimeSpan.FromSeconds(60)
};

// ❌ FOUT - elke call maakt nieuwe HttpClient
public async Task<T> GetAsync(int id)
{
    using var client = new HttpClient(); // DON'T DO THIS!
    // ...
}
```

---

### Vraag 4: Dependency Injection Lifetimes

**Vraag**: Leg het verschil uit tussen `AddSingleton`, `AddTransient` en `AddScoped` in Dependency Injection. Wanneer gebruik je welke?

**Antwoord**:

| Lifetime | Wanneer aangemaakt | Wanneer gebruikt | Voorbeeld |
|----------|-------------------|------------------|-----------|
| **Singleton** | Eén keer bij app start | Hergebruik voor hele app | Services met state, API clients |
| **Transient** | Bij elke inject | Nieuwe instance per request | ViewModels, Pages |
| **Scoped** | Per scope (request) | Binnen één scope | Weinig gebruikt in MAUI |

**TripTracker voorbeelden**:

```csharp
// Singleton - eén instantie voor hele app
builder.Services.AddSingleton<IPhotoService, PhotoService>();
builder.Services.AddSingleton<TripsPage>();

// Transient - nieuwe instantie bij elke navigatie
builder.Services.AddTransient<TripDetailPage>();
builder.Services.AddTransient<INavigationService, NavigationService>();
```

**Wanneer welke?**:
- **Singleton**: Services die state bewaren, duur om aan te maken, thread-safe
- **Transient**: Lightweight services, Pages die verse state nodig hebben
- **Scoped**: Web apps (per HTTP request), weinig gebruikt in MAUI

---

### Vraag 5: NavigationService Pattern

**Vraag**: Waarom gebruiken we een `INavigationService` in plaats van direct `Navigation.PushAsync()` in ViewModels? Wat zijn de voordelen voor MVVM?

**Antwoord**:

**Zonder NavigationService** (FOUT):
```csharp
// ❌ ViewModel heeft referentie naar View
[RelayCommand]
private async Task NavigateToDetail()
{
    await Navigation.PushAsync(new TripDetailPage());
    // Problemen:
    // 1. ViewModel kent de View (breekt MVVM)
    // 2. Dependencies moeten manueel aangemaakt
    // 3. Niet testbaar (geen mock mogelijk)
}
```

**Met NavigationService** (CORRECT):
```csharp
// ✅ Clean separation
[RelayCommand]
private async Task NavigateToDetail()
{
    await _navigationService.NavigateToTripDetailPageAsync();
    // Voordelen:
    // 1. ViewModel kent geen Views
    // 2. Dependency Injection voor Pages
    // 3. Makkelijk te mocken in tests
    // 4. Centralisatie van navigatie logica
}
```

**MVVM voordelen**:
1. **Testbaarheid**: NavigationService kan gemockt worden
2. **Separation of Concerns**: ViewModel weet niet HOW navigatie werkt
3. **Reusability**: NavigationService kan in meerdere ViewModels gebruikt
4. **Dependency Injection**: Pages krijgen automatisch dependencies
5. **No View references**: ViewModel heeft geen `using` naar Views folder

---

### Vraag 6: API Error Handling

**Vraag**: Hoe handel je errors af in de Services Layer? Waar hoort error handling thuis: in de Service of in de ViewModel?

**Antwoord**:

**Strategie**: Throw exceptions in Services, catch in ViewModels

**In Service** (ApiService):
```csharp
public virtual async Task<T> GetAsync(int id)
{
    var response = await client.GetAsync($"{BASE_URL}/{EndPoint}/{id}");
    if (response.IsSuccessStatusCode)
    {
        var jsonData = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(jsonData)!;
    }
    // Throw exception - laat ViewModel beslissen wat te doen
    throw new Exception(
        $"GetAsync failed with status code {response.StatusCode}"
    );
}
```

**In ViewModel**:
```csharp
[RelayCommand]
private async Task LoadTrip()
{
    try
    {
        IsBusy = true;
        SelectedTrip = await _tripDataService.GetAsync(tripId);
    }
    catch (HttpRequestException ex)
    {
        // Network error - toon user-friendly message
        await ShowAlert("Network Error", "Check your connection");
    }
    catch (Exception ex)
    {
        // Generic error
        await ShowAlert("Error", ex.Message);
    }
    finally
    {
        IsBusy = false;
    }
}
```

**Waarom?**:
- **Service**: Verantwoordelijk voor technische correctheid (status codes)
- **ViewModel**: Verantwoordelijk voor user experience (alerts, retry logic)
- **Separation**: Service weet niet hoe errors aan user getoond worden

---

## Samenvatting

### Kernconcepten

1. **Generieke Interface**: `IApiService<T>` voor type-safe CRUD operaties
2. **Abstract Base Class**: `ApiService<T>` met herbruikbare HttpClient logic
3. **Concrete Services**: `TripDataService` en `TripStopDataService` met endpoint
4. **Navigation Service**: `INavigationService` voor MVVM-compliant navigatie
5. **Dependency Injection**: Services, Pages en ViewModels geregistreerd in MauiProgram
6. **Service Lifetimes**: Singleton voor app-wide, Transient voor per-request

### File Structuur

```
TripTracker.App/
├── Services/
│   ├── IApiService.cs              (Generieke interface)
│   ├── ApiService.cs               (Abstract base class)
│   ├── ITripDataService.cs         (Trip-specifieke interface)
│   ├── TripDataService.cs          (Trip API implementatie)
│   ├── ITripStopDataService.cs     (TripStop-specifieke interface)
│   ├── TripStopDataService.cs      (TripStop API implementatie)
│   ├── INavigationService.cs       (Navigation interface)
│   └── NavigationService.cs        (Navigation implementatie)
├── Models/
│   ├── Trip.cs                     (Model met ObservableObject)
│   └── TripStop.cs                 (Model met ObservableObject)
└── MauiProgram.cs                  (DI registratie)
```

### Best Practices

1. Gebruik generieke interfaces voor herbruikbare code
2. HttpClient hergebruiken via static field
3. Specifieke HTTP status codes checken
4. Error handling in ViewModels, niet in Services
5. NavigationService voor MVVM compliance
6. Dependency Injection voor testbaarheid
7. Singleton voor state, Transient voor fresh instances
8. Ngrok voor Android/iOS testing

---

## Volgende Fase

**Fase 4: ViewModels & Messaging**
- MVVM Community Toolkit
- ObservableObject, RelayCommand
- WeakReferenceMessenger
- Messages tussen ViewModels
- Data binding

---

*Documentatie gegenereerd voor TripTracker - Challenge 1 - AI.NET Cursus*
