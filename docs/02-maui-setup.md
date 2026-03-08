---
fase: TripTracker MAUI Setup
status: Voltooid
tags:
  - maui
  - mvvm
  - dependency-injection
  - observableobject
  - examen
created: 2025-12-20
---

# TripTracker MAUI - Project Setup & MVVM Fundamentals

## Overzicht

Deze documentatie beschrijft de **TripTracker.App** setup: het .NET MAUI project met MVVM architectuur, Dependency Injection, en de fundamentele Models die de basis vormen voor de app.

> [!info] Doel
> TripTracker.App is een .NET MAUI applicatie waarmee gebruikers hun reizen kunnen bijhouden met GPS-locaties, foto's en AI-gestuurde beschrijvingen per tussenstop. De app integreert met de TripTracker.API voor data persistence.

**Wat is er gebouwd:**
- .NET MAUI app met MVVM Community Toolkit
- ObservableObject models voor two-way data binding
- Dependency Injection voor services, pages en viewmodels
- AppShell navigatie structuur
- Exception handling voor debugging

---

## 1. Wat is .NET MAUI?

**.NET Multi-platform App UI (MAUI)** is het cross-platform framework van Microsoft voor het bouwen van native mobile en desktop apps met één codebase.

> [!info] Cross-platform Development
> **Eén codebase** → **Meerdere platforms:**
> - Android
> - iOS
> - macOS
> - Windows
>
> **Hoe?** MAUI compileert naar native code per platform. XAML wordt vertaald naar platform-specifieke UI componenten.

### MAUI vs Xamarin

| Aspect | Xamarin | .NET MAUI |
|--------|---------|-----------|
| Project structuur | Aparte projecten per platform | Eén project voor alle platforms |
| UI Framework | Xamarin.Forms | .NET MAUI (evolutie van Forms) |
| .NET Versie | .NET 6 en ouder | .NET 6+ |
| Status | Legacy (maintenance mode) | Huidig framework |

**Conclusie:** MAUI is de opvolger van Xamarin.Forms met een modernere architectuur.

---

## 2. Waarom MVVM?

**MVVM (Model-View-ViewModel)** is het architectuur patroon voor .NET MAUI apps.

> [!tip] Examenvraag: Wat is MVVM?
> **MVVM bestaat uit 3 lagen:**
>
> 1. **Model** - Data + business logic (Trip, TripStop)
> 2. **View** - XAML UI (TripsPage.xaml)
> 3. **ViewModel** - UI logic + commands (TripsViewModel)
>
> **Data binding** verbindt View en ViewModel zonder code-behind.

### MVVM Voordelen

| Voordeel | Uitleg | Voorbeeld |
|----------|--------|-----------|
| **Separation of Concerns** | UI logica gescheiden van UI | ViewModel bevat geen XAML code |
| **Testability** | ViewModels zijn testbaar zonder UI | Unit tests voor TripsViewModel |
| **Reusability** | ViewModels herbruikbaar in andere Views | Zelfde ViewModel voor iOS/Android |
| **Data Binding** | Automatische UI updates | Property change → UI update |

### Code-behind blijft LEEG!

> [!warning] Examenvereiste: Geen Code-Behind
> In de cursus moet **alle logica** in ViewModels. Code-behind `.xaml.cs` bestanden blijven leeg (behalve `InitializeComponent()`).
>
> **Fout:**
> ```csharp
> // ❌ NIET doen in .xaml.cs
> public partial class TripsPage : ContentPage
> {
>     private void OnButtonClicked(object sender, EventArgs e)
>     {
>         // Logica hier = FOUT!
>     }
> }
> ```
>
> **Correct:**
> ```csharp
> // ✅ Alles in ViewModel
> public class TripsViewModel : ObservableRecipient
> {
>     [RelayCommand]
>     private void OnButtonClicked()
>     {
>         // Logica hier
>     }
> }
> ```

---

## 3. Project Structuur

### Mappenstructuur TripTracker.App

```
TripTracker.App/
├── Models/               ← ObservableObject models (Trip, TripStop)
├── ViewModels/           ← ObservableRecipient viewmodels met commands + messaging
├── Views/                ← XAML pagina's (UI)
├── Services/             ← Business logic (ApiService, NavigationService, etc.)
├── Messages/             ← CommunityToolkit.Mvvm messages voor communicatie
├── Converters/           ← Value converters voor XAML binding
├── Platforms/            ← Platform-specifieke code (Android, iOS, Windows)
├── Resources/            ← Images, fonts, styles
├── App.xaml              ← Application entry point
├── AppShell.xaml         ← Shell navigatie structuur
└── MauiProgram.cs        ← DI registratie
```

> [!info] Vergelijking met SafariSnap (Les 2)
> **SafariSnap:**
> - Models/ → `BigFiveAnimal.cs`
> - ViewModels/ → ViewModels met OpenAI logic
> - Views/ → `InfoPage.xaml`
> - Services/ → `AnalyzeImageService.cs`
>
> **TripTracker:**
> - Zelfde structuur + extra Messages/ folder voor communicatie tussen pages

### Waarom deze structuur?

| Map | Doel | Voorbeeld |
|-----|------|-----------|
| **Models/** | Data structuren met `ObservableObject` | `Trip.cs`, `TripStop.cs` |
| **ViewModels/** | UI logica, commands, state | `TripsViewModel.cs` |
| **Views/** | XAML pagina's | `TripsPage.xaml` |
| **Services/** | API calls, business logic | `ApiService.cs`, `NavigationService.cs` |
| **Messages/** | Cross-page communicatie | `TripSelectedMessage.cs` |

---

## 4. MauiProgram.cs - Dependency Injection Setup

**Locatie:** `MauiProgram.cs`

Dit is het **hart van de app configuratie**. Hier registreer je alle services, pages en viewmodels voor Dependency Injection.

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

        // Debug exception handlers (zoals SafariSnap)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Debug.WriteLine($"[AppDomain] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Debug.WriteLine($"[TaskScheduler] {e.Exception}");
            e.SetObserved();
        };

        AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
        {
            Debug.WriteLine($"[FirstChance] {e.Exception.GetType().Name}: {e.Exception.Message}");
        };

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

### Uitleg per sectie

#### 1. App Configuratie

```csharp
var builder = MauiApp.CreateBuilder();
builder
    .UseMauiApp<App>()
    .ConfigureFonts(fonts =>
    {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
    });
```

> [!tip] Examenvraag: Wat doet `UseMauiApp<App>()`?
> Dit registreert de `App` class als entry point van de applicatie. Dit is de `App.xaml.cs` die de `AppShell` initialiseert.

#### 2. Debug Logging

```csharp
#if DEBUG
    builder.Logging.AddDebug();
#endif
```

> [!info] Conditional Compilation
> `#if DEBUG` compileert alleen in Debug mode, niet in Release builds. Dit voorkomt onnodige logging in productie.

#### 3. Exception Handlers

```csharp
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    Debug.WriteLine($"[AppDomain] {e.ExceptionObject}");
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    Debug.WriteLine($"[TaskScheduler] {e.Exception}");
    e.SetObserved();
};

AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
{
    Debug.WriteLine($"[FirstChance] {e.Exception.GetType().Name}: {e.Exception.Message}");
};
```

> [!tip] Examenvraag: Waarom exception handlers?
> Deze handlers loggen **alle exceptions** naar de debug console, zelfs als ze gecaught worden.
>
> - **UnhandledException**: App crashes
> - **UnobservedTaskException**: Async tasks zonder await/try-catch
> - **FirstChanceException**: Alle exceptions (zelfs gecaught)
>
> **Nuttig voor debugging:** Je ziet alle errors, ook die normaal "verdwijnen" in try-catch blocks.

#### 4. Services Registratie

```csharp
builder.Services.AddTransient<INavigationService, NavigationService>();
builder.Services.AddSingleton<IPhotoService, PhotoService>();
builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
builder.Services.AddSingleton<IGeocodingService, GeocodingService>();
builder.Services.AddSingleton<IAnalyzeImageService, AnalyzeImageService>();
```

> [!tip] Examenvraag: Wat is het verschil tussen AddTransient en AddSingleton?
> **Dependency Injection Lifetimes:**
>
> | Lifetime | Gedrag | Gebruik voor | Voorbeeld |
> |----------|--------|--------------|-----------|
> | **Transient** | Nieuwe instance per request | Lichte services | NavigationService |
> | **Scoped** | Eén instance per HTTP request | (Niet in MAUI) | - |
> | **Singleton** | Eén instance gedurende app lifetime | Stateful services | PhotoService, ApiService |
>
> **Waarom NavigationService transient?**
> Navigatie is stateless - geen reden om dezelfde instance te hergebruiken.
>
> **Waarom PhotoService singleton?**
> Photo/geolocation services kunnen state bijhouden (permissions, caching).

#### 5. Pages & ViewModels Registratie

```csharp
// Hoofdpagina - Singleton
builder.Services.AddSingleton<TripsPage>();
builder.Services.AddSingleton<ITripsViewModel, TripsViewModel>();

// Detail pagina's - Transient
builder.Services.AddTransient<TripDetailPage>();
builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();
```

> [!tip] Examenvraag: Waarom hoofdpagina's Singleton en detail pagina's Transient?
> **Hoofdpagina's (Singleton):**
> - Blijven in geheugen gedurende app lifetime
> - State wordt behouden tussen navigaties
> - Sneller (geen herinitialisatie)
> - Voorbeeld: `TripsPage` toont altijd dezelfde lijst
>
> **Detail pagina's (Transient):**
> - Nieuwe instance bij elke navigatie
> - Geen state lekken tussen navigaties
> - Schoner geheugen (garbage collected na navigatie)
> - Voorbeeld: `TripDetailPage` toont verschillende trips

### Vergelijking met ImpressoArt (Les 2)

| Aspect | TripTracker | ImpressoArt | Verschil |
|--------|-------------|-------------|----------|
| Exception handlers | Ja, 3 handlers | Ja, identiek | Zelfde patroon |
| Service registration | 5+ services | Geen services | TripTracker complexer |
| Page/ViewModel pattern | Singleton + Transient | Transient | TripTracker optimalisatie |
| Fonts | OpenSans | OpenSans | Zelfde |

---

## 5. Models met ObservableObject

In MVVM moeten **models** de UI kunnen notifieren bij property changes. Hiervoor gebruiken we `ObservableObject` van de CommunityToolkit.

> [!info] Wat doet ObservableObject?
> `ObservableObject` implementeert `INotifyPropertyChanged`, wat two-way data binding mogelijk maakt.
>
> **Zonder ObservableObject:**
> ```csharp
> // Property change → UI update NIET automatisch
> trip.Name = "Nieuwe naam";
> // UI toont nog oude waarde!
> ```
>
> **Met ObservableObject:**
> ```csharp
> // Property change → UI update AUTOMATISCH
> trip.Name = "Nieuwe naam";
> // UI toont meteen "Nieuwe naam"
> ```

### Trip Model

**Locatie:** `Models/Trip.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TripTracker.App.Models
{
    // Model voor de MAUI app - erft van ObservableObject voor MVVM data binding
    // Zoals Sighting in SafariSnap (Les 2)
    public class Trip : ObservableObject
    {
        private int id;
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        private string name = string.Empty;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string? description;
        public string? Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private DateTime startDate;
        public DateTime StartDate
        {
            get => startDate;
            set => SetProperty(ref startDate, value);
        }

        private DateTime? endDate;
        public DateTime? EndDate
        {
            get => endDate;
            set => SetProperty(ref endDate, value);
        }

        private string? imageUrl;
        public string? ImageUrl
        {
            get => imageUrl;
            set => SetProperty(ref imageUrl, value);
        }

        private ObservableCollection<TripStop> tripStops = new();
        public ObservableCollection<TripStop> TripStops
        {
            get => tripStops;
            set => SetProperty(ref tripStops, value);
        }
    }
}
```

#### Property Pattern Uitleg

```csharp
private string name = string.Empty;
public string Name
{
    get => name;
    set => SetProperty(ref name, value);
}
```

> [!tip] Examenvraag: Wat doet SetProperty()?
> `SetProperty(ref name, value)` doet **3 dingen:**
>
> 1. **Checkt of waarde veranderd is** (performance optimalisatie)
> 2. **Update de backing field** (`name = value`)
> 3. **Triggert PropertyChanged event** → UI update
>
> **Equivalent zonder SetProperty:**
> ```csharp
> public string Name
> {
>     get => name;
>     set
>     {
>         if (name != value)
>         {
>             name = value;
>             OnPropertyChanged(nameof(Name));
>         }
>     }
> }
> ```

#### ObservableCollection vs List

```csharp
private ObservableCollection<TripStop> tripStops = new();
public ObservableCollection<TripStop> TripStops
{
    get => tripStops;
    set => SetProperty(ref tripStops, value);
}
```

> [!tip] Examenvraag: Waarom ObservableCollection i.p.v. List?
> **`List<T>`:**
> - Notificeert NIET bij Add/Remove
> - UI update alleen als je hele lijst vervangt
>
> **`ObservableCollection<T>`:**
> - Notificeert bij Add/Remove/Clear
> - UI update automatisch bij collection changes
> - **Perfect voor XAML ListView/CollectionView binding**
>
> **Voorbeeld:**
> ```csharp
> // Met List - UI update NIET
> trip.TripStops.Add(newStop);
> // UI toont nieuwe stop NIET!
>
> // Met ObservableCollection - UI update WEL
> trip.TripStops.Add(newStop);
> // UI toont nieuwe stop meteen!
> ```

### TripStop Model

**Locatie:** `Models/TripStop.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace TripTracker.App.Models
{
    // Model voor TripStop - erft van ObservableObject voor MVVM data binding
    // Equivalent van Sighting in SafariSnap (Les 2)
    public class TripStop : ObservableObject
    {
        private int id;
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        private int tripId;
        public int TripId
        {
            get => tripId;
            set => SetProperty(ref tripId, value);
        }

        // Trip navigation property verwijderd - niet nodig
        // We gebruiken alleen TripId voor de relatie

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
            set => SetProperty(ref latitude, value);
        }

        private double longitude;
        public double Longitude
        {
            get => longitude;
            set => SetProperty(ref longitude, value);
        }

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

        private DateTime dateTime;
        public DateTime DateTime
        {
            get => dateTime;
            set => SetProperty(ref dateTime, value);
        }

        private string? country;
        public string? Country
        {
            get => country;
            set => SetProperty(ref country, value);
        }
    }
}
```

#### Geen Navigation Property in MAUI Model

> [!info] Waarom geen `Trip? Trip` in TripStop?
> In de **API Entity** heb je wel `Trip` navigation property voor EF Core relaties.
>
> In de **MAUI App** gebruiken we GEEN navigation properties, alleen `TripId`:
> - Voorkomt circular reference problemen bij JSON deserialisatie
> - App haalt trip data op via aparte API call als nodig
> - **Best practice:** Entities hebben navigation properties, DTOs/Models niet

### Vergelijking met SafariSnap (Les 2)

| Aspect | TripTracker | SafariSnap | Overeenkomst |
|--------|-------------|------------|--------------|
| Base class | `ObservableObject` | `ObservableObject` | Identiek |
| Property pattern | `SetProperty(ref field, value)` | `SetProperty(ref field, value)` | Identiek |
| Collections | `ObservableCollection<T>` | Niet gebruikt | - |
| Navigation props | Alleen `TripId` (geen `Trip`) | Niet gebruikt | Beide simpel |
| Complexity | 2 models (Trip + TripStop) | 1 model (BigFiveAnimal) | TripTracker complexer |

---

## 6. App.xaml.cs - Application Entry Point

**Locatie:** `App.xaml.cs`

```csharp
namespace TripTracker.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
```

> [!tip] Examenvraag: Wat doet App.xaml.cs?
> `App` is de **entry point** van de MAUI applicatie.
>
> **Wat gebeurt er:**
> 1. `MauiProgram.CreateMauiApp()` wordt aangeroepen
> 2. `App()` constructor wordt aangeroepen
> 3. `InitializeComponent()` laadt `App.xaml` resources
> 4. `CreateWindow()` maakt een `Window` met `AppShell` als root
> 5. App start met de `AppShell` navigatie

### InitializeComponent()

> [!info] Wat doet InitializeComponent()?
> `InitializeComponent()` is een **gegenereerde method** die XAML resources laadt.
>
> **XAML:**
> ```xml
> <!-- App.xaml -->
> <Application.Resources>
>     <Color x:Key="Primary">#512BD4</Color>
> </Application.Resources>
> ```
>
> **Code:**
> ```csharp
> InitializeComponent(); // Laadt alle resources van App.xaml
> ```

### CreateWindow()

```csharp
protected override Window CreateWindow(IActivationState? activationState)
{
    return new Window(new AppShell());
}
```

> [!tip] Examenvraag: Wat is de AppShell?
> `AppShell` is de **root container** voor navigatie en layout.
>
> **Het definieert:**
> - TabBar navigatie
> - Flyout menu (indien gebruikt)
> - Routes voor navigatie
>
> **Zie volgende sectie voor details.**

---

## 7. AppShell.xaml - Navigatie Structuur

**Locatie:** `AppShell.xaml`

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="TripTracker.App.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:local="clr-namespace:TripTracker.App"
    xmlns:views="clr-namespace:TripTracker.App.Views"
    Shell.FlyoutBehavior="Disabled"
    Title="TripTracker">

    <!-- TabBar met navigatie zoals SafariSnap (Les 3) -->
    <TabBar>
        <ShellContent
            Title="Trips"
            ContentTemplate="{DataTemplate views:TripsPage}"
            Icon="{OnPlatform 'dotnet_bot.png', iOS='dotnet_bot.png', MacCatalyst='dotnet_bot.png'}" />
    </TabBar>

</Shell>
```

**Locatie:** `AppShell.xaml.cs`

```csharp
namespace TripTracker.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }
}
```

### XAML Uitleg

#### 1. Shell Element

```xml
<Shell
    x:Class="TripTracker.App.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:views="clr-namespace:TripTracker.App.Views"
    Shell.FlyoutBehavior="Disabled"
    Title="TripTracker">
```

> [!tip] Examenvraag: Wat is Shell in MAUI?
> `Shell` is een **container voor app navigatie** met built-in features:
>
> - **TabBar**: Bottom tabs (Android/iOS)
> - **Flyout**: Side menu (hamburger menu)
> - **Navigation stack**: Push/pop pages
> - **Routes**: Deep linking
>
> **Voordelen:**
> - Consistent navigatie patroon
> - Platform-specifieke rendering
> - Less code (geen custom navigatie logic)

#### 2. Shell.FlyoutBehavior

```xml
Shell.FlyoutBehavior="Disabled"
```

> [!info] Flyout vs TabBar
> **Flyout:** Side menu (hamburger icon)
> **TabBar:** Bottom tabs
>
> In TripTracker gebruiken we **alleen TabBar**, dus flyout is disabled.

#### 3. TabBar & ShellContent

```xml
<TabBar>
    <ShellContent
        Title="Trips"
        ContentTemplate="{DataTemplate views:TripsPage}"
        Icon="{OnPlatform 'dotnet_bot.png', iOS='dotnet_bot.png', MacCatalyst='dotnet_bot.png'}" />
</TabBar>
```

> [!tip] Examenvraag: Wat doet ShellContent?
> `ShellContent` definieert een **tab item** in de TabBar.
>
> **Properties:**
> - **Title**: Tekst onder icon
> - **ContentTemplate**: Welke page tonen (data template voor lazy loading)
> - **Icon**: Icon boven tekst (platform-specifiek)
>
> **DataTemplate:**
> `{DataTemplate views:TripsPage}` = lazy loading. Page wordt ALLEEN geladen als gebruiker de tab selecteert.

> [!warning] TabBar met 1 item = ONZICHTBAAR!
> TripTracker heeft een `<TabBar>` met slechts **1 ShellContent**. In dat geval toont Android/iOS **geen zichtbare tabs** - er valt immers niets te kiezen.
>
> | Situatie | Visueel Resultaat |
> |----------|-------------------|
> | `<TabBar>` met 1 item | Geen zichtbare tabs |
> | `<TabBar>` met 2+ items | Tabs onderaan scherm |
>
> **Waarom dan toch TabBar?**
> Voorbereiding voor uitbreiding. Later een tweede tab toevoegen (bv. "Map") is dan simpel:
> ```xml
> <TabBar>
>     <ShellContent Title="Trips" ... />
>     <ShellContent Title="Map" ... />  <!-- Nieuwe tab -->
> </TabBar>
> ```

#### 4. OnPlatform

```xml
Icon="{OnPlatform 'dotnet_bot.png', iOS='dotnet_bot.png', MacCatalyst='dotnet_bot.png'}"
```

> [!info] Platform-specifieke waarden
> `OnPlatform` gebruikt **verschillende waarden per platform**.
>
> **Voorbeeld:**
> ```xml
> <Label Text="{OnPlatform Android='Android App', iOS='iOS App'}" />
> ```
>
> In dit geval gebruiken we dezelfde icon voor alle platforms, maar syntactisch tonen we hoe het werkt.

### Navigatie: NavigationService (niet Shell Routes!)

> [!warning] TripTracker gebruikt GEEN Shell Routes!
> Er zijn 2 navigatie-methodes in MAUI. TripTracker (en SafariSnap) gebruiken **NavigationService met DI**, niet Shell Routes.

#### Methode 1: Shell Routes (NIET gebruikt in TripTracker)

```csharp
// In AppShell.xaml.cs - NIET WAT WIJ DOEN
Routing.RegisterRoute("TripDetailPage", typeof(TripDetailPage));

// Navigeren met string
await Shell.Current.GoToAsync("TripDetailPage");
```

#### Methode 2: NavigationService met DI (WEL gebruikt!)

**Hoe werkt het? Stap voor stap:**

```
┌─────────────────────────────────────────────────────────────┐
│  1. USER TIKT OP "Bekijk Trip"                              │
│     ViewModel roept NavigationService aan                   │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  2. NAVIGATIONSERVICE                                       │
│     _serviceProvider.GetRequiredService<TripDetailPage>()   │
│     "Hé container, geef mij een TripDetailPage!"            │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  3. DI CONTAINER (IServiceProvider)                         │
│     - Maakt TripDetailPage aan                              │
│     - Ziet: constructor heeft TripDetailViewModel nodig     │
│     - Maakt TripDetailViewModel aan                         │
│     - Injecteert ViewModel in Page                          │
│     - Geeft complete Page terug                             │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  4. NAVIGATIONSERVICE                                       │
│     _navigation.PushAsync(page)                             │
│     Page verschijnt op scherm                               │
└─────────────────────────────────────────────────────────────┘
```

**AppShell.xaml.cs blijft LEEG:**
```csharp
public AppShell()
{
    InitializeComponent();
    // GEEN route registratie nodig!
}
```

**NavigationService.cs doet het werk:**
```csharp
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;  // ← DI container
    private INavigation _navigation;                     // ← MAUI navigation

    public async Task NavigateToTripDetailPageAsync()
    {
        // 1. Vraag Page aan container (inclusief ViewModel!)
        var page = _serviceProvider.GetRequiredService<TripDetailPage>();

        // 2. Toon de page
        await _navigation.PushAsync(page);
    }
}
```

> [!info] Eenvoudig gezegd
> **NavigationService** is een "tussenpersoon":
> - ViewModel zegt: "Ik wil naar TripDetailPage"
> - NavigationService vraagt de Page aan de DI container
> - Container maakt Page + ViewModel automatisch aan
> - NavigationService toont de Page
>
> **Voordeel:** Niemand hoeft te weten HOE pages gemaakt worden!

> [!tip] Examenvraag: Wat doet NavigationService en waarom gebruiken we het?
> **NavigationService** is een service die navigatie tussen pages afhandelt via DI.
>
> **Waarom een aparte service?**
> 1. **Separation of Concerns** - ViewModel hoeft niet te weten HOE navigatie werkt
> 2. **Testbaar** - Je kunt `INavigationService` mocken in unit tests
> 3. **DI integratie** - Pages krijgen automatisch hun ViewModel geïnjecteerd
>
> **Hoe gebruik je het?**
> ```csharp
> // In ViewModel - inject via constructor
> public TripsViewModel(INavigationService navigationService)
> {
>     _navigationService = navigationService;
> }
>
> // Navigeren = 1 regel
> await _navigationService.NavigateToTripDetailPageAsync();
> ```

---

## 8. NuGet Packages

TripTracker.App gebruikt de volgende key packages:

### CommunityToolkit.Mvvm

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
```

> [!tip] Examenvraag: Wat doet CommunityToolkit.Mvvm?
> **CommunityToolkit.Mvvm** (ook bekend als MVVM Toolkit) biedt:
>
> 1. **ObservableObject**: Base class voor models/viewmodels
> 2. **RelayCommand**: Attribute voor commands (`[RelayCommand]`)
> 3. **ObservableProperty**: Auto-generate properties (`[ObservableProperty]`)
> 4. **Messaging**: Cross-component communicatie
>
> **Zonder toolkit:**
> ```csharp
> // Handmatige property
> private string name;
> public string Name
> {
>     get => name;
>     set { name = value; OnPropertyChanged(); }
> }
> ```
>
> **Met toolkit:**
> ```csharp
> // SetProperty helper
> private string name;
> public string Name
> {
>     get => name;
>     set => SetProperty(ref name, value);
> }
> ```
>
> **Of nog korter met [ObservableProperty]:**
> ```csharp
> [ObservableProperty]
> private string name;
> // Genereert automatisch public Name property!
> ```

### Andere belangrijke packages

| Package | Doel | Versie |
|---------|------|--------|
| `Microsoft.Maui.*` | MAUI framework | 9.0+ |
| `Microsoft.Extensions.Logging.Debug` | Debug logging | 9.0+ |
| `CommunityToolkit.Maui` | Extra MAUI controls | 9.0+ |

---

## 9. Vergelijking API Models vs MAUI Models

> [!warning] LET OP: Verschillende Models!
> **API Models** (TripTracker.API/Entities) en **MAUI Models** (TripTracker.App/Models) lijken op elkaar maar dienen verschillende doeleinden.

| Aspect | API Entity | MAUI Model | Verschil |
|--------|------------|------------|----------|
| **Base class** | Geen (plain class) | `ObservableObject` | MAUI notificeert UI |
| **Properties** | Plain properties | `SetProperty(ref field, value)` | MAUI heeft PropertyChanged |
| **Data Annotations** | `[Required]`, `[MaxLength]` | Geen | API valideert input |
| **Collections** | `ICollection<T>` | `ObservableCollection<T>` | MAUI voor UI binding |
| **Purpose** | Database mapping (EF Core) | UI data binding (XAML) | Verschillende lagen |

### API Entity vs MAUI Model - Voorbeeld

**API Entity:**
```csharp
// TripTracker.API/Entities/Trip.cs
public class Trip
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();
}
```

**MAUI Model:**
```csharp
// TripTracker.App/Models/Trip.cs
public class Trip : ObservableObject
{
    private int id;
    public int Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    private string name = string.Empty;
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private ObservableCollection<TripStop> tripStops = new();
    public ObservableCollection<TripStop> TripStops
    {
        get => tripStops;
        set => SetProperty(ref tripStops, value);
    }
}
```

> [!info] Waarom dubbele models?
> **Separation of Concerns:**
> - **API Entity** → Optimaal voor database (EF Core annotations, navigation properties)
> - **MAUI Model** → Optimaal voor UI (ObservableObject, two-way binding)
>
> **Data flow:**
> 1. API haalt Entity op van database
> 2. API converteert Entity → DTO (via AutoMapper)
> 3. MAUI haalt DTO op van API
> 4. MAUI converteert DTO → Model (voor binding)

---

## 10. Examenvragen & Antwoorden

> [!tip] Examenvraag 1: Wat is .NET MAUI en wat zijn de voordelen?
> **Antwoord:**
> .NET MAUI is het cross-platform framework van Microsoft voor native mobile/desktop apps.
>
> **Voordelen:**
> - Eén codebase → meerdere platforms (Android, iOS, Windows, macOS)
> - Native performance (compileert naar platform-specifieke code)
> - Shared business logic
> - XAML voor UI declaratie
> - Integratie met .NET ecosystem

> [!tip] Examenvraag 2: Wat is MVVM en waarom gebruiken we het?
> **Antwoord:**
> MVVM = Model-View-ViewModel architectuur patroon.
>
> **3 Lagen:**
> - **Model**: Data + business logic
> - **View**: XAML UI
> - **ViewModel**: UI logic + commands
>
> **Voordelen:**
> - Separation of Concerns (UI gescheiden van logica)
> - Testability (ViewModels testbaar zonder UI)
> - Data binding (automatische UI updates)
> - Reusability (ViewModels herbruikbaar)

> [!tip] Examenvraag 3: Wat is ObservableObject en waarom erven models hiervan?
> **Antwoord:**
> `ObservableObject` is een base class die `INotifyPropertyChanged` implementeert.
>
> **Wat doet het:**
> - Triggert PropertyChanged events bij property wijzigingen
> - Maakt two-way data binding mogelijk
> - Automatische UI updates bij model changes
>
> **Zonder ObservableObject:** Property changes updaten UI NIET automatisch.
> **Met ObservableObject:** UI update automatisch via data binding.

> [!tip] Examenvraag 4: Wat doet SetProperty()?
> **Antwoord:**
> `SetProperty(ref field, value)` is een helper method van `ObservableObject`.
>
> **Het doet 3 dingen:**
> 1. Check of waarde veranderd is (performance)
> 2. Update backing field
> 3. Trigger PropertyChanged event → UI update
>
> **Voorbeeld:**
> ```csharp
> public string Name
> {
>     get => name;
>     set => SetProperty(ref name, value); // Automatische notificatie
> }
> ```

> [!tip] Examenvraag 5: Waarom ObservableCollection i.p.v. List?
> **Antwoord:**
> **`List<T>`:** Notificeert NIET bij Add/Remove → UI update NIET.
> **`ObservableCollection<T>`:** Notificeert bij collection changes → UI update WEL.
>
> **Use case:**
> XAML ListView/CollectionView binding vereist `ObservableCollection` voor automatische updates bij Add/Remove items.

> [!tip] Examenvraag 6: Wat is Dependency Injection in MAUI?
> **Antwoord:**
> DI registreert services/pages/viewmodels in `MauiProgram.cs` voor constructor injection.
>
> **Voordelen:**
> - Loose coupling (interfaces i.p.v. concrete types)
> - Testability (mock dependencies)
> - Lifetime management (Singleton, Transient, Scoped)
>
> **Voorbeeld uit TripTracker:**
> ```csharp
> // Registratie in MauiProgram.cs:
> builder.Services.AddSingleton<ITripDataService, TripDataService>();
> builder.Services.AddSingleton<ITripStopDataService, TripStopDataService>();
> builder.Services.AddSingleton<IPhotoService, PhotoService>();
> builder.Services.AddSingleton<IAnalyzeImageService, AnalyzeImageService>();
>
> // Gebruik in AddStopViewModel (constructor injection):
> public AddStopViewModel(
>     INavigationService navigationService,
>     IPhotoService photoService,
>     IGeolocationService geolocationService,
>     IGeocodingService geocodingService,
>     IAnalyzeImageService analyzeImageService,
>     ITripStopDataService tripStopDataService)
> {
>     _navigationService = navigationService;
>     _photoService = photoService;
>     _geolocationService = geolocationService;
>     _geocodingService = geocodingService;
>     _analyzeImageService = analyzeImageService;
>     _tripStopDataService = tripStopDataService;
> }
> ```
> **Let op:** AddStopViewModel krijgt 6 services geïnjecteerd - DI regelt dit automatisch!

> [!tip] Examenvraag 7: Wat is het verschil tussen Singleton en Transient?
> **Antwoord:**
> | Lifetime | Gedrag | Gebruik voor |
> |----------|--------|--------------|
> | **Singleton** | Eén instance gedurende app lifetime | Stateful services (PhotoService, AnalyzeImageService) |
> | **Transient** | Nieuwe instance per request | Stateless services, detail pages |
>
> **Voorbeeld uit TripTracker:**
> - `PhotoService` → **Singleton** (kan permissions/state cachen)
> - `TripsPage` → **Singleton** (state behouden tussen navigaties)
> - `TripDetailPage` → **Transient** (schone instance per trip)

> [!tip] Examenvraag 8: Waarom exception handlers in MauiProgram.cs?
> **Antwoord:**
> Exception handlers loggen ALLE exceptions naar debug console.
>
> **3 Types:**
> - **UnhandledException**: App crashes
> - **UnobservedTaskException**: Async tasks zonder await
> - **FirstChanceException**: Alle exceptions (zelfs gecaught)
>
> **Nuttig voor debugging:** Je ziet alle errors, ook die normaal "verdwijnen" in try-catch.

> [!tip] Examenvraag 9: Wat is AppShell en waarvoor dient het?
> **Antwoord:**
> `AppShell` is de root container voor navigatie.
>
> **Features:**
> - TabBar navigatie (bottom tabs)
> - Flyout menu (side menu)
> - Routes voor deep linking
> - Platform-specifieke rendering
>
> **Voorbeeld:**
> ```xml
> <TabBar>
>     <ShellContent Title="Trips" ContentTemplate="{DataTemplate views:TripsPage}" />
> </TabBar>
> ```

> [!tip] Examenvraag 10: Wat doet CommunityToolkit.Mvvm?
> **Antwoord:**
> MVVM Toolkit biedt helpers voor MVVM pattern:
>
> 1. **ObservableObject**: Base class voor models/viewmodels
> 2. **SetProperty()**: Property change notificatie
> 3. **[RelayCommand]**: Auto-generate commands
> 4. **[ObservableProperty]**: Auto-generate properties
> 5. **Messaging**: Cross-component communicatie
>
> **Reduces boilerplate code significantly.**

> [!tip] Examenvraag 11: Waarom zijn API Models en MAUI Models verschillend?
> **Antwoord:**
> **API Entity:**
> - Voor database (EF Core)
> - Data annotations voor validatie
> - `ICollection<T>` voor relaties
>
> **MAUI Model:**
> - Voor UI binding
> - `ObservableObject` voor PropertyChanged
> - `ObservableCollection<T>` voor UI updates
>
> **Separation of Concerns:** Elke laag optimaal voor zijn doel.

> [!tip] Examenvraag 12: Wat doet InitializeComponent()?
> **Antwoord:**
> `InitializeComponent()` is een gegenereerde method die XAML resources laadt.
>
> **Wordt aangeroepen in:**
> - App.xaml.cs → Laadt app resources
> - Pages → Laadt page UI
> - Custom controls → Laadt control UI
>
> **ALTIJD in constructor aanroepen VOOR andere code!**

---

## 11. Hoe uit te leggen aan de Docent

### Start met MVVM Pattern

> [!example] Leg MVVM uit
> "TripTracker gebruikt het MVVM pattern. Ik heb Models die erven van `ObservableObject` voor two-way data binding. ViewModels bevatten de UI logica en commands. Views zijn XAML pagina's zonder code-behind. Data binding verbindt View en ViewModel automatisch."

### Toon ObservableObject Pattern

> [!example] Demonstreer een Model
> ```csharp
> public class Trip : ObservableObject
> {
>     private string name = string.Empty;
>     public string Name
>     {
>         get => name;
>         set => SetProperty(ref name, value);
>     }
> }
> ```
>
> "Elke property gebruikt `SetProperty()` om de UI te notifieren bij changes. Dit maakt two-way binding mogelijk: UI changes updaten het model en vice versa."

### Uitleg Dependency Injection

> [!example] Toon MauiProgram.cs
> "In `MauiProgram.cs` registreer ik alle services, pages en viewmodels voor Dependency Injection. Hoofdpagina's zijn Singleton (state behouden), detail pagina's zijn Transient (nieuwe instance per navigatie). Services zoals `NavigationService` zijn Transient omdat ze stateless zijn."

### Demonstreer AppShell Navigatie

> [!example] Toon AppShell.xaml
> ```xml
> <TabBar>
>     <ShellContent Title="Trips" ContentTemplate="{DataTemplate views:TripsPage}" />
> </TabBar>
> ```
>
> "AppShell definieert de navigatie structuur. Ik gebruik een TabBar met één tab (Trips). Navigatie gebeurt via `NavigationService` met DI, niet via Shell routes - dit is beter testbaar en werkt naadloos met constructor injection."

### Vergelijk met SafariSnap

> [!example] Toon overeenkomsten
> "Net zoals SafariSnap uit Les 2:
> - ObservableObject voor models
> - SetProperty() voor PropertyChanged
> - Dependency Injection in MauiProgram.cs
> - Exception handlers voor debugging
>
> TripTracker voegt toe:
> - ObservableCollection voor child entities
> - Navigation properties voor relaties
> - Singleton vs Transient optimalisatie"

---

## 12. Checklist voor Examen

- [ ] Kan ik uitleggen wat .NET MAUI is en waarom we het gebruiken?
- [ ] Kan ik het MVVM pattern uitleggen (Model, View, ViewModel)?
- [ ] Kan ik uitleggen waarom code-behind leeg moet blijven?
- [ ] Kan ik uitleggen wat ObservableObject doet en waarom we het gebruiken?
- [ ] Kan ik de SetProperty() method uitleggen?
- [ ] Kan ik het verschil tussen List en ObservableCollection uitleggen?
- [ ] Kan ik uitleggen wat Dependency Injection is in MAUI?
- [ ] Kan ik het verschil tussen Singleton en Transient uitleggen?
- [ ] Kan ik uitleggen waarom we exception handlers toevoegen?
- [ ] Kan ik uitleggen wat AppShell doet en hoe navigatie werkt?
- [ ] Kan ik uitleggen wat CommunityToolkit.Mvvm biedt?
- [ ] Kan ik het verschil tussen API Models en MAUI Models uitleggen?
- [ ] Kan ik de project structuur uitleggen (Models/, ViewModels/, Views/, Services/)?
- [ ] Kan ik een Trip model tonen met ObservableObject pattern?
- [ ] Kan ik MauiProgram.cs configuratie uitleggen?
- [ ] Kan ik tonen hoe pages/viewmodels geregistreerd worden voor DI?

---

## 13. Volgende Stappen

**Voor MVVM implementatie:**
- [ ] ViewModels bouwen met Commands (zie 03-viewmodels-commands.md)
- [ ] Views bouwen met XAML data binding (zie 04-views-xaml.md)
- [ ] ApiService implementeren voor API calls (zie 05-api-integratie.md)
- [ ] NavigationService implementeren voor routing (zie 06-navigatie.md)

**Zie:** Les 2 & Les 3 documentatie voor MVVM patterns en API integratie.

---

**Documentatie gegenereerd:** 2025-12-20
