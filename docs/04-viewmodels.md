---
fase: 04 - ViewModels
status: Voltooid
tags:
  - triptracker
  - mvvm
  - viewmodels
  - messaging
  - interfaces
  - examen
created: 2025-12-20
---

# Fase 4: ViewModels met Interfaces

## Overzicht

Deze fase implementeert alle **ViewModels** volgens het **MVVM pattern** met de **CommunityToolkit.Mvvm**. ViewModels zijn de "brains" van de app - ze bevatten alle business logic, commands en data binding voor de Views.

> [!info] MVVM = Model-View-ViewModel
> - **Model**: Data (Trip, TripStop)
> - **View**: UI (XAML pages)
> - **ViewModel**: Logic + Commands + Property Notification

**Waarom interfaces?**
- Dependency Injection (DI) vereist interfaces
- Testbaarheid (mock interfaces in unit tests)
- Loose coupling (Views kennen alleen interface, niet implementatie)

---

## Architectuur: ObservableRecipient Pattern

Alle ViewModels erven van `ObservableRecipient`, die combineert:
1. `ObservableObject`: Property change notification (`INotifyPropertyChanged`)
2. `IRecipient<TMessage>`: Messaging tussen ViewModels

```csharp
// ObservableRecipient = ObservableObject + Messaging support
public class TripsViewModel : ObservableRecipient, IRecipient<RefreshDataMessage>, ITripsViewModel
{
    // Property change notification via SetProperty()
    private ObservableCollection<Trip> trips = new();
    public ObservableCollection<Trip> Trips
    {
        get => trips;
        set => SetProperty(ref trips, value);  // Automatische PropertyChanged event
    }

    // Message handling via Receive()
    public void Receive(RefreshDataMessage message)
    {
        _ = LoadTripsAsync();  // Herlaad data
    }
}
```

> [!tip] Examenvraag
> **Vraag:** Wat doet `SetProperty()` en waarom gebruiken we het?
>
> **Antwoord:** `SetProperty()` (van `ObservableObject`) doet 3 dingen:
> 1. Controleert of de nieuwe waarde verschillend is van de oude
> 2. Wijzigt de backing field (`ref trips`)
> 3. Triggert `PropertyChanged` event zodat UI automatisch update
>
> Dit is het hart van **data binding** in MVVM!

### ObservableObject vs ObservableRecipient

> [!tip] Examenvraag
> **Vraag:** Wat is het verschil tussen `ObservableObject` en `ObservableRecipient`?

**Hiërarchie:**

```
ObservableObject          ← Basis: property change notifications
       ↑
       │ erft van
       │
ObservableRecipient       ← Uitbreiding: + messaging support
```

**Vergelijking:**

| Feature | `ObservableObject` | `ObservableRecipient` |
|---------|-------------------|----------------------|
| `SetProperty()` | ✅ | ✅ (geërfd) |
| `OnPropertyChanged()` | ✅ | ✅ (geërfd) |
| `Messenger.Register()` | ❌ | ✅ |
| `IRecipient<T>` interface | ❌ | ✅ |

**Wanneer welke gebruiken?**

| Class | Gebruik voor | Voorbeeld |
|-------|--------------|-----------|
| `ObservableObject` | **Models** - alleen data binding | `Trip`, `TripStop`, `YearFilterItem` |
| `ObservableRecipient` | **ViewModels** - data binding + messaging | `TripsViewModel`, `TripDetailViewModel` |

**Code verschil:**

```csharp
// MODEL - alleen properties, geen messaging nodig
public class Trip : ObservableObject
{
    private string name;
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);  // ✅ Werkt
    }
    // ❌ Geen Messenger.Register mogelijk
}

// VIEWMODEL - properties + messaging nodig
public class TripsViewModel : ObservableRecipient, IRecipient<RefreshDataMessage>
{
    public TripsViewModel()
    {
        // ✅ Messaging mogelijk (komt van ObservableRecipient)
        Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));
    }

    public void Receive(RefreshDataMessage message)
    {
        _ = LoadTripsAsync();
    }
}
```

**In het kort:** `ObservableRecipient` = `ObservableObject` + messaging

---

## Messages Uitgelegd

### Wat is een Message?

Een message is een **container voor data** die van ViewModel A naar ViewModel B wordt gestuurd, zonder dat ze elkaar hoeven te kennen.

```
┌─────────────────┐         TripSelectedMessage         ┌─────────────────┐
│ TripDetailVM    │  ────────────────────────────────►  │ AddStopViewModel│
│                 │   WeakReferenceMessenger.Send()     │                 │
│ "User wil stop  │                                     │ Receive() wordt │
│  toevoegen aan  │                                     │ aangeroepen met │
│  deze trip"     │                                     │ Trip object     │
└─────────────────┘                                     └─────────────────┘
```

### De Register-lijn uitgelegd

```csharp
Messenger.Register<AddStopViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));
//                 ↑                 ↑                     ↑     ↑    ↑    ↑
//                 1                 2                     3     4    5    6
```

| # | Code | Betekenis |
|---|------|-----------|
| 1 | `AddStopViewModel` | **Wie** luistert (dit ViewModel) |
| 2 | `TripSelectedMessage` | **Waarnaar** je luistert (message type) |
| 3 | `this` | **Deze instantie** van het ViewModel |
| 4 | `r` | **r**ecipient = de ontvanger (this) |
| 5 | `m` | **m**essage = de binnengekomen message |
| 6 | `r.Receive(m)` | Roep `Receive()` aan met de message |

**In mensentaal:**
> "Hey Messenger, registreer **mij** (`this`) als luisteraar.
> Als er een `TripSelectedMessage` binnenkomt,
> roep dan mijn `Receive()` method aan met die message."

### De reis van een Message

**Stap 1: STUREN** (in TripDetailViewModel)
```csharp
private async Task GoToAddStop()
{
    // Eerst navigeren, dan message sturen
    await _navigationService.NavigateToAddStopPageAsync();

    // Stuur message met hele Trip object (niet alleen ID!)
    if (CurrentTrip != null)
    {
        WeakReferenceMessenger.Default.Send(new TripSelectedMessage(CurrentTrip));
    }
}
```

**Stap 2: ONTVANGEN** (in AddStopViewModel)
```csharp
public void Receive(TripSelectedMessage message)
{
    // Haal Trip object uit message via .Value
    CurrentTrip = message.Value;

    // Nu heb ik het HELE trip object - geen extra fetch nodig!
}
```

**Stap 3: GEBRUIKEN**
```csharp
private async Task SaveStop()
{
    var stop = new TripStop
    {
        TripId = CurrentTrip.Id,  // Trip object heeft alle info
        Title = Title,
        Description = Description
    };

    await _tripStopDataService.PostAsync(stop);
}
```

### Wat zit er in een Message?

```csharp
// Messages/TripSelectedMessage.cs
public class TripSelectedMessage : ValueChangedMessage<Trip>
{
    // Value bevat het hele Trip object - geen extra fetch nodig!
    public TripSelectedMessage(Trip trip) : base(trip) { }
}
```

**Overzicht message types:**

| Message | Data | Gebruik |
|---------|------|---------|
| `TripSelectedMessage` | `Trip` (object) | Geselecteerde trip doorgeven |
| `StopSelectedMessage` | `TripStop` (object) | Geselecteerde stop doorgeven |
| `RefreshDataMessage` | `bool` | Trigger om data te herladen |
| `TripEditMessage` | `Trip` (object) | Trip om te bewerken |
| `StopEditMessage` | `TripStop` (object) | Stop om te bewerken |
| `ShowStopsOnMapMessage` | `(List<TripStop>, string)` tuple | Stops tonen op kaart |

### Message Class Implementaties

Alle messages erven van `ValueChangedMessage<T>`:

```csharp
// Messages/RefreshDataMessage.cs
public class RefreshDataMessage : ValueChangedMessage<bool>
{
    public RefreshDataMessage(bool value) : base(value) { }
}

// Messages/TripSelectedMessage.cs
public class TripSelectedMessage : ValueChangedMessage<Trip>
{
    public TripSelectedMessage(Trip trip) : base(trip) { }
}

// Messages/StopSelectedMessage.cs
public class StopSelectedMessage : ValueChangedMessage<TripStop>
{
    public StopSelectedMessage(TripStop stop) : base(stop) { }
}

// Messages/TripEditMessage.cs
public class TripEditMessage : ValueChangedMessage<Trip>
{
    public TripEditMessage(Trip trip) : base(trip) { }
}

// Messages/StopEditMessage.cs
public class StopEditMessage : ValueChangedMessage<TripStop>
{
    public StopEditMessage(TripStop stop) : base(stop) { }
}

// Messages/ShowStopsOnMapMessage.cs - met TUPLE!
public class ShowStopsOnMapMessage : ValueChangedMessage<(List<TripStop> Stops, string Title)>
{
    public ShowStopsOnMapMessage(List<TripStop> stops, string title)
        : base((stops, title)) { }
}
```

> [!tip] Examenvraag: Tuple in Message
> `ShowStopsOnMapMessage` gebruikt een **tuple** als value type.
> - Voordeel: Meerdere waarden in één message
> - Nadeel: Complexere syntax: `message.Value.Stops` en `message.Value.Title`

### Waarom `(r, m) =>` lambda?

Dit zorgt voor **weak reference** - als het ViewModel wordt opgeruimd, geen memory leak:

```csharp
// ✅ Veilig (weak reference)
Messenger.Register<AddStopViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));
```

---

## ViewModel Anatomie - De 8 Blokken

Een ViewModel bestaat uit **8 logische blokken**. Hier leggen we elk blok uit.

### Visueel overzicht

```csharp
public class AddStopViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>, IAddStopViewModel
{                                                    // ─── 1️⃣ CLASS DECLARATION

    private readonly IPhotoService _photoService;
    private readonly INavigationService _navigationService;
                                                     // ─── 2️⃣ PRIVATE FIELDS

    private string title = string.Empty;
    public string Title { get => title; set => SetProperty(ref title, value); }
                                                     // ─── 3️⃣ PROPERTIES

    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }
                                                     // ─── 4️⃣ COMMANDS

    public AddStopViewModel(IPhotoService photoService, ...) { ... }
                                                     // ─── 5️⃣ CONSTRUCTOR

    public void Receive(TripSelectedMessage message) { ... }
                                                     // ─── 6️⃣ MESSAGE HANDLERS

    private void BindCommands() { ... }
                                                     // ─── 7️⃣ COMMAND BINDING

    private async Task Save() { ... }
                                                     // ─── 8️⃣ COMMAND HANDLERS
}
```

---

### 1️⃣ Class Declaration

```csharp
public class AddStopViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>, IAddStopViewModel
//           ↑                   ↑                    ↑                               ↑
//           Naam                Base class           Message interface               Eigen interface
```

| Onderdeel | Wat het doet |
|-----------|--------------|
| `AddStopViewModel` | Naam van de class |
| `: ObservableRecipient` | Erft SetProperty() + Messenger |
| `, IRecipient<TripSelectedMessage>` | "Ik ontvang dit message type" |
| `, IAddStopViewModel` | Eigen interface voor DI |

**Waarom al deze onderdelen?**

```csharp
// ObservableRecipient geeft je:
SetProperty(ref field, value);     // Property change notifications
Messenger.Register<...>(...);      // Message ontvangst

// IRecipient<T> dwingt je om te implementeren:
public void Receive(TripSelectedMessage message) { }

// IAddStopViewModel is voor:
builder.Services.AddTransient<IAddStopViewModel, AddStopViewModel>();  // DI registratie
```

**Meerdere messages ontvangen?**

```csharp
public class TripDetailViewModel : ObservableRecipient,
    IRecipient<TripSelectedMessage>,   // Message 1
    IRecipient<RefreshDataMessage>,    // Message 2
    ITripDetailViewModel
```

---

### 2️⃣ Private Fields (Services)

```csharp
private readonly INavigationService _navigationService;
private readonly IPhotoService _photoService;
private readonly IApiService _apiService;
```

**Wat zijn dit?**
- Plekken om geïnjecteerde services op te slaan
- `readonly` = kan alleen in constructor worden gezet
- `_underscore` = conventie voor private fields

**Waarom `readonly`?**

```csharp
// ✅ GOED - readonly beschermt tegen per ongeluk overschrijven
private readonly IPhotoService _photoService;

// ❌ FOUT - kan per ongeluk overschreven worden
private IPhotoService _photoService;

// Ergens anders in de code:
_photoService = null;  // readonly voorkomt dit!
```

**Naamconventie:**

| Type | Patroon | Voorbeeld |
|------|---------|-----------|
| Private field | `_camelCase` | `_navigationService` |
| Parameter | `camelCase` | `navigationService` |
| Property | `PascalCase` | `NavigationService` |

---

### 3️⃣ Properties

```csharp
private string title = string.Empty;
public string Title
{
    get => title;
    set => SetProperty(ref title, value);
}
```

**De 3 onderdelen:**

```csharp
private string title = string.Empty;    // 1. Backing field (opslag)
public string Title                      // 2. Property naam (XAML bindt hieraan)
{
    get => title;                        // 3a. Getter - geef waarde terug
    set => SetProperty(ref title, value);// 3b. Setter - sla op + notify UI
}
```

**Wat doet `SetProperty()`?**

```csharp
set => SetProperty(ref title, value);
//     ↑
//     Doet 3 dingen:
//     1. Check of value != title (skip als gelijk)
//     2. title = value (opslaan)
//     3. PropertyChanged event (UI update)
```

**Zonder SetProperty zou je dit moeten schrijven:**

```csharp
// ❌ Lang en foutgevoelig
public string Title
{
    get => title;
    set
    {
        if (title != value)
        {
            title = value;
            OnPropertyChanged(nameof(Title));
        }
    }
}

// ✅ Kort met SetProperty
public string Title
{
    get => title;
    set => SetProperty(ref title, value);
}
```

**Computed Property (geen setter):**

```csharp
// Property die afhangt van andere properties
public string LatitudeDisplay => Latitude.ToString("F6");

// Als Latitude verandert, moet je UI handmatig notifyen:
public double Latitude
{
    get => latitude;
    set
    {
        if (SetProperty(ref latitude, value))
        {
            OnPropertyChanged(nameof(LatitudeDisplay));  // Ook deze updaten!
        }
    }
}
```

---

### 4️⃣ Commands (Declaratie)

```csharp
public ICommand SaveCommand { get; set; }
public ICommand CancelCommand { get; set; }
public ICommand CapturePhotoCommand { get; set; }
```

**Wat is een Command?**
- Een actie die vanuit XAML kan worden aangeroepen
- Koppelt Button/TapGesture aan een method

**Hoe werkt binding?**

```
XAML                                    ViewModel
─────────────────────────────────────────────────────
<Button Command="{Binding SaveCommand}"/>
              │
              └──────► SaveCommand (ICommand)
                              │
                              └──────► Save() method
```

**Waarom `ICommand` en niet gewoon een method?**

```csharp
// XAML kan GEEN methods aanroepen:
<Button Click="Save"/>  // ❌ Dit werkt niet in MVVM!

// XAML kan WEL binden aan ICommand:
<Button Command="{Binding SaveCommand}"/>  // ✅ Dit werkt!
```

---

### 5️⃣ Constructor

```csharp
public AddStopViewModel(
    INavigationService navigationService,      // ─┐
    IPhotoService photoService,                //  ├── Parameters (DI)
    IApiService apiService)                    // ─┘
{
    // A. Services opslaan
    _navigationService = navigationService;
    _photoService = photoService;
    _apiService = apiService;

    // B. Message registratie
    Messenger.Register<AddStopViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));

    // C. Commands koppelen
    BindCommands();
}
```

**Waar komen de parameters vandaan?**

```csharp
// MauiProgram.cs - DI registratie
builder.Services.AddSingleton<INavigationService, NavigationService>();
builder.Services.AddTransient<IPhotoService, PhotoService>();
builder.Services.AddTransient<IApiService, ApiService>();

// .NET MAUI ziet de constructor en injecteert automatisch!
```

**De 3 stappen in de constructor:**

| Stap | Code | Doel |
|------|------|------|
| A | `_service = service` | Services opslaan voor later gebruik |
| B | `Messenger.Register<...>` | Luisteren naar messages |
| C | `BindCommands()` | Commands koppelen aan methods |

---

### 6️⃣ Message Handlers

```csharp
public void Receive(TripSelectedMessage message)
{
    CurrentTrip = message.Value;  // Trip object via .Value

    // Fire-and-forget: start async zonder te wachten
    _ = LoadTripStopsAsync();
}
```

**Wat gebeurt hier?**

1. Message komt binnen (verstuurd door ander ViewModel)
2. Object uit message halen via `.Value` property
3. Opslaan in property (CurrentTrip)
4. Optioneel: gerelateerde data laden

**Fire-and-forget patroon:**

```csharp
_ = LoadStopDataAsync();
// ↑
// Underscore = "Ik weet dat dit async is, maar ik wacht niet"
```

**Waarom geen `await`?**

```csharp
// ❌ FOUT - Receive is NIET async
public async void Receive(TripSelectedMessage message)  // async void = gevaarlijk!
{
    await LoadStopDataAsync();
}

// ✅ GOED - Fire-and-forget
public void Receive(TripSelectedMessage message)
{
    _ = LoadStopDataAsync();  // Start loading, wacht niet
}
```

**Meerdere message handlers:**

```csharp
// Elke IRecipient<T> vereist een Receive method
public void Receive(TripSelectedMessage message)
{
    CurrentTrip = message.Value;  // Trip object
    _ = LoadTripStopsAsync();
}

public void Receive(RefreshDataMessage message)
{
    _ = RefreshTripAndStopsAsync();  // Herlaad data
}
```

---

### 7️⃣ Command Binding

```csharp
private void BindCommands()
{
    SaveCommand = new AsyncRelayCommand(Save, CanSave);
    CancelCommand = new AsyncRelayCommand(Cancel);
    CapturePhotoCommand = new AsyncRelayCommand(CapturePhoto);
    PickPhotoCommand = new AsyncRelayCommand(PickPhoto);
}
```

**Wat doet dit?**

```csharp
SaveCommand = new AsyncRelayCommand(Save, CanSave);
//            ↑                      ↑     ↑
//            Type                   │     └── Wanneer enabled? (optioneel)
//                                   └── Welke method aanroepen
```

**Command types:**

| Type | Wanneer gebruiken |
|------|-------------------|
| `RelayCommand` | Synchrone methods (geen await) |
| `AsyncRelayCommand` | Async methods (met await) |
| `RelayCommand<T>` | Met parameter (bv. item uit lijst) |
| `AsyncRelayCommand<T>` | Async + parameter |

**Met CanExecute (button enabled/disabled):**

```csharp
SaveCommand = new AsyncRelayCommand(Save, CanSave);

private bool CanSave()
{
    // Button alleen enabled als titel ingevuld EN foto aanwezig
    return !string.IsNullOrEmpty(Title) && HasPhoto;
}
```

**CanExecute updaten:**

```csharp
public string Title
{
    get => title;
    set
    {
        if (SetProperty(ref title, value))
        {
            // Hercheck of Save button enabled moet zijn
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
        }
    }
}
```

---

### 8️⃣ Command Handlers

```csharp
private async Task Save()
{
    try
    {
        IsLoading = true;

        var stop = new TripStop
        {
            TripId = _tripId,
            Title = Title,
            Description = Description
        };

        await _apiService.CreateStopAsync(stop);

        // Stuur refresh message naar andere ViewModels
        WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));

        await _navigationService.NavigateBackAsync();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Save error: {ex.Message}");
    }
    finally
    {
        IsLoading = false;  // ALTIJD uitvoeren, ook bij error
    }
}
```

**De onderdelen:**

| Onderdeel | Code | Doel |
|-----------|------|------|
| Try | `try { ... }` | Normale flow |
| Loading state | `IsLoading = true` | Toon loading indicator |
| Data bouwen | `new TripStop { ... }` | Object maken voor API |
| API call | `await _apiService.CreateStopAsync()` | Data opslaan |
| Message sturen | `Send(new RefreshDataMessage())` | Andere VMs notifyen |
| Navigatie | `NavigateBackAsync()` | Terug naar vorige pagina |
| Catch | `catch (Exception)` | Error handling |
| Finally | `finally { IsLoading = false }` | Cleanup (altijd!) |

**Waarom `finally`?**

```csharp
// ❌ FOUT - bij error blijft IsLoading true!
try
{
    IsLoading = true;
    await _apiService.CreateStopAsync(stop);
    IsLoading = false;  // Wordt niet bereikt bij error!
}
catch { }

// ✅ GOED - finally wordt ALTIJD uitgevoerd
try
{
    IsLoading = true;
    await _apiService.CreateStopAsync(stop);
}
finally
{
    IsLoading = false;  // Altijd, ook bij error
}
```

---

### Samenvatting: De 8 Blokken

```
┌─────────────────────────────────────────────────────────────────┐
│                      VIEWMODEL STRUCTUUR                         │
├─────────────────────────────────────────────────────────────────┤
│  1️⃣ CLASS DECLARATION                                            │
│     class Name : ObservableRecipient, IRecipient<T>, IInterface  │
├─────────────────────────────────────────────────────────────────┤
│  2️⃣ PRIVATE FIELDS                                               │
│     private readonly IService _service;                          │
├─────────────────────────────────────────────────────────────────┤
│  3️⃣ PROPERTIES                                                   │
│     public string Name { get; set => SetProperty(...); }         │
├─────────────────────────────────────────────────────────────────┤
│  4️⃣ COMMANDS                                                     │
│     public ICommand SaveCommand { get; set; }                    │
├─────────────────────────────────────────────────────────────────┤
│  5️⃣ CONSTRUCTOR                                                  │
│     Services opslaan → Messages registreren → BindCommands()     │
├─────────────────────────────────────────────────────────────────┤
│  6️⃣ MESSAGE HANDLERS                                             │
│     public void Receive(Message m) { ... }                       │
├─────────────────────────────────────────────────────────────────┤
│  7️⃣ COMMAND BINDING                                              │
│     SaveCommand = new AsyncRelayCommand(Save, CanSave);          │
├─────────────────────────────────────────────────────────────────┤
│  8️⃣ COMMAND HANDLERS                                             │
│     private async Task Save() { try/catch/finally }              │
└─────────────────────────────────────────────────────────────────┘
```

> [!tip] Examenvraag
> **Vraag:** Waarom gebruiken we `SetProperty()` in plaats van directe assignment?
>
> **Antwoord:** `SetProperty()` doet 3 dingen:
> 1. Check of waarde gewijzigd is (skip als gelijk)
> 2. Update de backing field
> 3. Trigger `PropertyChanged` event → UI update automatisch

> [!tip] Examenvraag
> **Vraag:** Waarom staat command binding in een aparte `BindCommands()` method?
>
> **Antwoord:**
> 1. **Organisatie** - Houdt constructor overzichtelijk
> 2. **Leesbaarheid** - Alle commands bij elkaar
> 3. **Conventie** - Consistent patroon door hele codebase

---

## 1. TripsViewModel - Overzichtspagina

**Interface:** `ITripsViewModel.cs`

```csharp
public interface ITripsViewModel
{
    bool IsLoading { get; set; }
    ObservableCollection<Trip> Trips { get; set; }
    ObservableCollection<Trip> FilteredTrips { get; set; }
    ObservableCollection<YearFilterItem> YearFilters { get; set; }
    int? SelectedYear { get; set; }
    Trip? SelectedTrip { get; set; }
    ICommand ViewTripCommand { get; set; }
    ICommand AddTripCommand { get; set; }
    ICommand DeleteTripCommand { get; set; }
    ICommand EditTripCommand { get; set; }
    ICommand ShowAllOnMapCommand { get; set; }
    ICommand SelectYearCommand { get; set; }
}
```

**Implementatie:** `TripsViewModel.cs` (VOLLEDIGE CODE)

```csharp
public class TripsViewModel : ObservableRecipient, IRecipient<RefreshDataMessage>, ITripsViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    // ═══════════════════════════════════════════════════════════
    // PROPERTIES - Allemaal met SetProperty voor data binding
    // ═══════════════════════════════════════════════════════════

    private bool isLoading;
    public bool IsLoading
    {
        get => isLoading;
        set => SetProperty(ref isLoading, value);
    }

    private ObservableCollection<Trip> trips = new();
    public ObservableCollection<Trip> Trips
    {
        get => trips;
        set => SetProperty(ref trips, value);
    }

    private ObservableCollection<Trip> filteredTrips = new();
    public ObservableCollection<Trip> FilteredTrips
    {
        get => filteredTrips;
        set => SetProperty(ref filteredTrips, value);
    }

    private ObservableCollection<YearFilterItem> yearFilters = new();
    public ObservableCollection<YearFilterItem> YearFilters
    {
        get => yearFilters;
        set => SetProperty(ref yearFilters, value);
    }

    private int? selectedYear;
    public int? SelectedYear
    {
        get => selectedYear;
        set
        {
            if (SetProperty(ref selectedYear, value))
            {
                ApplyYearFilter();
                UpdateYearFilterSelection();
            }
        }
    }

    private Trip? selectedTrip;
    public Trip? SelectedTrip
    {
        get => selectedTrip;
        set => SetProperty(ref selectedTrip, value);
    }

    // ═══════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════

    public ICommand ViewTripCommand { get; set; }
    public ICommand AddTripCommand { get; set; }
    public ICommand DeleteTripCommand { get; set; }
    public ICommand EditTripCommand { get; set; }
    public ICommand ShowAllOnMapCommand { get; set; }
    public ICommand SelectYearCommand { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════

    public TripsViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

        LoadTripsAsync();  // Start laden (geen await in constructor)
        BindCommands();
    }

    // ═══════════════════════════════════════════════════════════
    // MESSAGE HANDLER
    // ═══════════════════════════════════════════════════════════

    public void Receive(RefreshDataMessage message)
    {
        _ = LoadTripsAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════

    private async Task LoadTripsAsync()
    {
        IsLoading = true;
        try
        {
            var tripService = new TripDataService();
            var tripList = await tripService.GetAllAsync();

            // UI updates MOETEN op MainThread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Trips.Clear();
                foreach (var trip in tripList)
                {
                    Trips.Add(trip);
                }
                UpdateAvailableYears();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading trips: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // COMMAND BINDING
    // ═══════════════════════════════════════════════════════════

    private void BindCommands()
    {
        ViewTripCommand = new AsyncRelayCommand<Trip>(GoToTripDetail);
        AddTripCommand = new AsyncRelayCommand(AddNewTrip);
        DeleteTripCommand = new AsyncRelayCommand<Trip>(DeleteTrip);
        EditTripCommand = new AsyncRelayCommand<Trip>(EditTrip);
        ShowAllOnMapCommand = new AsyncRelayCommand(ShowAllOnMap);
        SelectYearCommand = new RelayCommand<object>(SelectYear);
    }

    // ═══════════════════════════════════════════════════════════
    // YEAR FILTER HELPERS
    // ═══════════════════════════════════════════════════════════

    private void SelectYear(object? yearObj)
    {
        // CommandParameter is altijd YearFilterItem (vanuit XAML DataTemplate)
        if (yearObj is YearFilterItem item)
            SelectedYear = item.Year;
    }

    private void UpdateAvailableYears()
    {
        var years = Trips
            .Select(t => t.StartDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        YearFilters.Clear();
        YearFilters.Add(new YearFilterItem { Year = null, IsSelected = false });
        foreach (var year in years)
        {
            YearFilters.Add(new YearFilterItem { Year = year, IsSelected = false });
        }

        var currentYear = DateTime.Now.Year;
        var newSelectedYear = years.Contains(currentYear) ? currentYear : (int?)null;

        if (SelectedYear != newSelectedYear)
        {
            SelectedYear = newSelectedYear;
        }
        else
        {
            ApplyYearFilter();
            UpdateYearFilterSelection();
        }
    }

    private void UpdateYearFilterSelection()
    {
        foreach (var item in YearFilters)
        {
            var shouldBeSelected = item.Year == SelectedYear;
            if (item.IsSelected != shouldBeSelected)
            {
                item.IsSelected = shouldBeSelected;
            }
        }
    }

    private void ApplyYearFilter()
    {
        FilteredTrips.Clear();

        var tripsToShow = SelectedYear == null
            ? Trips
            : Trips.Where(t => t.StartDate.Year == SelectedYear);

        foreach (var trip in tripsToShow)
        {
            FilteredTrips.Add(trip);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // COMMAND HANDLERS
    // ═══════════════════════════════════════════════════════════

    private async Task GoToTripDetail(Trip? trip)
    {
        if (trip != null)
        {
            SelectedTrip = trip;
            await _navigationService.NavigateToTripDetailPageAsync();
            WeakReferenceMessenger.Default.Send(new TripSelectedMessage(trip));
            SelectedTrip = null;
        }
    }

    private async Task AddNewTrip()
    {
        await _navigationService.NavigateToAddTripPageAsync();
    }

    private async Task DeleteTrip(Trip? trip)
    {
        if (trip == null) return;

        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Delete Trip",
            $"Are you sure you want to delete '{trip.Name}'?",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            var tripService = new TripDataService();
            await tripService.DeleteAsync(trip.Id);
            Trips.Remove(trip);
            ApplyYearFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting trip: {ex.Message}");
        }
    }

    private async Task EditTrip(Trip? trip)
    {
        if (trip == null) return;

        await _navigationService.NavigateToEditTripPageAsync();
        WeakReferenceMessenger.Default.Send(new TripEditMessage(trip));
    }

    private async Task ShowAllOnMap()
    {
        IsLoading = true;
        try
        {
            var allStops = new List<TripStop>();
            var tripService = new TripDataService();

            foreach (var trip in Trips)
            {
                var stops = await tripService.GetTripStopsAsync(trip.Id);
                allStops.AddRange(stops);
            }

            if (!allStops.Any())
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "No Stops", "There are no stops to show on the map.", "OK");
                return;
            }

            await _navigationService.NavigateToMapPageAsync();
            WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(allStops, "All Stops"));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### Key Concepts

**1. ObservableCollection vs List**
```csharp
// ✅ CORRECT: ObservableCollection triggert UI updates bij Add/Remove
public ObservableCollection<Trip> Trips { get; set; }

// ❌ FOUT: List triggert GEEN updates
public List<Trip> Trips { get; set; }
```

**2. AsyncRelayCommand met parameter**
```csharp
// Command met parameter (Trip)
ViewTripCommand = new AsyncRelayCommand<Trip>(GoToTripDetail);

// XAML binding:
<Button Command="{Binding ViewTripCommand}"
        CommandParameter="{Binding .}" />
```

**3. Message registratie**
```csharp
// Registreer in constructor
Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));
//                 ↑ Deze class    ↑ Type message              ↑ Handler method

// Handler implementeren
public void Receive(RefreshDataMessage message)
{
    _ = LoadTripsAsync();  // Fire-and-forget async
}
```

> [!warning] Fire-and-Forget Pattern
> `_ = LoadTripsAsync();` start async method ZONDER te wachten op resultaat.
> - Gebruik dit ALLEEN in event handlers waar wachten niet nodig is
> - Voor normale code: gebruik `await LoadTripsAsync();`

---

## 2. TripDetailViewModel - Detail pagina

**Interface:** `ITripDetailViewModel.cs`

```csharp
public interface ITripDetailViewModel
{
    bool IsLoading { get; set; }
    Trip? CurrentTrip { get; set; }
    ObservableCollection<TripStop> TripStops { get; set; }
    TripStop? SelectedStop { get; set; }
    ICommand AddStopCommand { get; set; }
    ICommand ViewStopCommand { get; set; }
    ICommand EditStopCommand { get; set; }
    ICommand DeleteStopCommand { get; set; }
    ICommand GoBackCommand { get; set; }
    ICommand ShowTripOnMapCommand { get; set; }
}
```

**Implementatie:** `TripDetailViewModel.cs` (VOLLEDIGE CODE)

```csharp
public class TripDetailViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>, IRecipient<RefreshDataMessage>, ITripDetailViewModel
{
    private readonly INavigationService _navigationService;

    // ═══════════════════════════════════════════════════════════
    // PROPERTIES - Allemaal met SetProperty voor data binding
    // ═══════════════════════════════════════════════════════════

    private bool isLoading;
    public bool IsLoading
    {
        get => isLoading;
        set => SetProperty(ref isLoading, value);
    }

    private Trip? currentTrip;
    public Trip? CurrentTrip
    {
        get => currentTrip;
        set => SetProperty(ref currentTrip, value);
    }

    private ObservableCollection<TripStop> tripStops = new();
    public ObservableCollection<TripStop> TripStops
    {
        get => tripStops;
        set => SetProperty(ref tripStops, value);
    }

    private TripStop? selectedStop;
    public TripStop? SelectedStop
    {
        get => selectedStop;
        set => SetProperty(ref selectedStop, value);
    }

    // ═══════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════

    public ICommand AddStopCommand { get; set; }
    public ICommand ViewStopCommand { get; set; }
    public ICommand EditStopCommand { get; set; }
    public ICommand DeleteStopCommand { get; set; }
    public ICommand GoBackCommand { get; set; }
    public ICommand ShowTripOnMapCommand { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════

    public TripDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        // Registreer voor TripSelectedMessage
        Messenger.Register<TripDetailViewModel, TripSelectedMessage>(this, (r, m) => r.Receive(m));

        // Registreer voor RefreshDataMessage (herlaad stops na opslaan nieuwe stop)
        Messenger.Register<TripDetailViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

        BindCommands();
    }

    // ═══════════════════════════════════════════════════════════
    // MESSAGE HANDLERS
    // ═══════════════════════════════════════════════════════════

    public void Receive(TripSelectedMessage message)
    {
        CurrentTrip = message.Value;
        _ = LoadTripStopsAsync();
    }

    public void Receive(RefreshDataMessage message)
    {
        // Herlaad stops wanneer een nieuwe stop is opgeslagen
        _ = LoadTripStopsAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════

    private async Task LoadTripStopsAsync()
    {
        if (CurrentTrip != null)
        {
            IsLoading = true;
            try
            {
                var tripService = new TripDataService();
                var stops = await tripService.GetTripStopsAsync(CurrentTrip.Id);

                // UI updates MOETEN op MainThread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TripStops = new ObservableCollection<TripStop>(stops);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading trip stops: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // COMMAND BINDING
    // ═══════════════════════════════════════════════════════════

    private void BindCommands()
    {
        AddStopCommand = new AsyncRelayCommand(GoToAddStop);
        ViewStopCommand = new AsyncRelayCommand<TripStop>(GoToStopDetail);
        EditStopCommand = new AsyncRelayCommand<TripStop>(EditStop);
        DeleteStopCommand = new AsyncRelayCommand<TripStop>(DeleteStop);
        GoBackCommand = new AsyncRelayCommand(GoBack);
        ShowTripOnMapCommand = new AsyncRelayCommand(ShowTripOnMap);
    }

    // ═══════════════════════════════════════════════════════════
    // COMMAND HANDLERS
    // ═══════════════════════════════════════════════════════════

    private async Task GoToAddStop()
    {
        await _navigationService.NavigateToAddStopPageAsync();
        // Stuur CurrentTrip mee zodat AddStopViewModel weet bij welke trip de stop hoort
        if (CurrentTrip != null)
        {
            WeakReferenceMessenger.Default.Send(new TripSelectedMessage(CurrentTrip));
        }
    }

    private async Task GoToStopDetail(TripStop? stop)
    {
        if (stop != null)
        {
            SelectedStop = stop;
            await _navigationService.NavigateToStopDetailPageAsync();
            WeakReferenceMessenger.Default.Send(new StopSelectedMessage(stop));
            SelectedStop = null;
        }
    }

    private async Task EditStop(TripStop? stop)
    {
        if (stop != null)
        {
            await _navigationService.NavigateToEditStopPageAsync();
            WeakReferenceMessenger.Default.Send(new StopEditMessage(stop));
        }
    }

    private async Task DeleteStop(TripStop? stop)
    {
        if (stop != null)
        {
            try
            {
                var stopService = new TripStopDataService();
                await stopService.DeleteAsync(stop.Id);
                TripStops.Remove(stop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting stop: {ex.Message}");
            }
        }
    }

    private async Task GoBack()
    {
        await _navigationService.NavigateBackAsync();
    }

    private async Task ShowTripOnMap()
    {
        if (CurrentTrip == null || !TripStops.Any())
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "No Stops",
                "This trip has no stops to show on the map.",
                "OK");
            return;
        }

        // EERST navigeren, DAN message sturen (zodat MapViewModel al geregistreerd is)
        await _navigationService.NavigateToMapPageAsync();
        WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(TripStops.ToList(), CurrentTrip.Name));
    }
}
```

### Key Concepts

**1. Meerdere IRecipient interfaces**
```csharp
// ViewModel kan MEERDERE message types ontvangen
public class TripDetailViewModel : ObservableRecipient,
    IRecipient<TripSelectedMessage>,      // Ontvang trip data
    IRecipient<RefreshDataMessage>,      // Herlaad data
    ITripDetailViewModel
{
    // Implementeer BEIDE Receive() methods
    public void Receive(TripSelectedMessage message) { ... }
    public void Receive(RefreshDataMessage message) { ... }
}
```

**2. SelectedStop Pattern**
```csharp
// Zet SelectedStop VOOR navigatie
SelectedStop = stop;
await _navigationService.NavigateToStopDetailPageAsync();
WeakReferenceMessenger.Default.Send(new StopSelectedMessage(stop));
SelectedStop = null;  // Reset NA message verzenden
```

**3. ShowTripOnMap - Navigatie + Messaging Order**
```csharp
// BELANGRIJK: Navigeer EERST, dan pas message sturen!
// Anders is MapViewModel nog niet geregistreerd als listener
await _navigationService.NavigateToMapPageAsync();
WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(TripStops.ToList(), CurrentTrip.Name));
```

> [!tip] Examenvraag
> **Vraag:** Wat is het verschil tussen `List<T>` en `ObservableCollection<T>`?
>
> **Antwoord:**
> - `List<T>`: Geen UI notifications. Add/Remove triggert GEEN PropertyChanged.
> - `ObservableCollection<T>`: Implementeert `INotifyCollectionChanged`. Elke wijziging (Add/Remove/Clear) update de UI automatisch.
>
> **Regel:** Gebruik ALTIJD `ObservableCollection<T>` voor properties die binden aan ListView/CollectionView!

---

## 3. AddStopViewModel - Nieuwe stop toevoegen

**Interface:** `IAddStopViewModel.cs`

```csharp
public interface IAddStopViewModel
{
    // Trip waar we stop aan toevoegen
    Trip? CurrentTrip { get; set; }

    // Foto data
    ImageSource? PhotoPreview { get; set; }
    byte[]? PhotoData { get; set; }
    bool IsAnalyzing { get; set; }
    bool HasPhoto { get; }

    // Stop details (worden door AI ingevuld, bewerkbaar door gebruiker)
    string Title { get; set; }
    string? Description { get; set; }
    double Latitude { get; set; }
    double Longitude { get; set; }
    string LatitudeDisplay { get; }   // Computed: voor UI binding
    string LongitudeDisplay { get; }  // Computed: voor UI binding
    string? Address { get; set; }
    string? PhotoUrl { get; set; }
    string? Country { get; set; }

    // Commands
    ICommand CapturePhotoCommand { get; }
    ICommand PickPhotoCommand { get; }
    ICommand AnalyzePhotoCommand { get; }
    ICommand SaveCommand { get; }
    ICommand CancelCommand { get; }
}
```

> [!tip] LatitudeDisplay / LongitudeDisplay
> Computed properties voor UI binding (string i.p.v. double).
> Staan in de interface als `{ get; }` only - geen setter want ze worden berekend.

**Implementatie:** `AddStopViewModel.cs` (VOLLEDIGE CODE)

```csharp
public class AddStopViewModel : ObservableRecipient, IRecipient<TripSelectedMessage>, IAddStopViewModel
{
    // ═══════════════════════════════════════════════════════════
    // SERVICES (DI) - 5 services!
    // ═══════════════════════════════════════════════════════════

    private readonly INavigationService _navigationService;
    private readonly IPhotoService _photoService;
    private readonly IGeolocationService _geolocationService;
    private readonly IGeocodingService _geocodingService;
    private readonly IAnalyzeImageService _analyzeImageService;

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
    public string LatitudeDisplay => Latitude != 0 ? Latitude.ToString("F6") : "Fetching...";

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

            var stopService = new TripStopDataService();
            await stopService.PostAsync(newStop);

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
            // Maak Photos folder in app data
            var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
            Directory.CreateDirectory(photosDir);

            // Unieke bestandsnaam
            var fileName = $"stop_{Guid.NewGuid()}.jpg";
            var filePath = Path.Combine(photosDir, fileName);

            // Schrijf bytes naar bestand
            await File.WriteAllBytesAsync(filePath, PhotoData);

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
```

### Key Concepts

**1. 5 Services via Dependency Injection**
```csharp
// AddStopViewModel heeft VEEL services nodig - allemaal via constructor injection
public AddStopViewModel(
    INavigationService navigationService,     // Navigatie
    IPhotoService photoService,               // Foto capture/pick
    IGeolocationService geolocationService,   // GPS
    IGeocodingService geocodingService,       // Adres lookup
    IAnalyzeImageService analyzeImageService) // AI analyse
```
Dit is een goed voorbeeld van **Separation of Concerns** - elke service doet één ding.

---

**2. Smart Stop Capture Flow (Parallel Operations)**
```
Gebruiker maakt foto
        │
        ├──→ ProcessPhoto() ──┬──→ Toon preview (direct)
        │                     ├──→ Update commands (direct)
        │                     └──→ _ = GetLocationAndGeocode() (fire-and-forget)
        │                              │
        │                              ├──→ GPS ophalen (async)
        │                              └──→ Reverse geocoding (async)
        │
        └──→ Gebruiker klikt "Analyze"
                    │
                    └──→ AI analyse (async) → vult Title + Description in
```

---

**3. Fire-and-Forget voor GPS**
```csharp
private async Task ProcessPhoto(byte[] bytes)
{
    PhotoData = bytes;
    PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));

    // GPS ophalen ZONDER te wachten - gebruiker kan alvast verder
    _ = GetLocationAndGeocode();  // Fire-and-forget!
}
```
**Waarom?** GPS + Geocoding duurt lang. Gebruiker hoeft niet te wachten om foto te zien of AI te starten.

---

**4. Computed Property met OnPropertyChanged**
```csharp
// HasPhoto is een COMPUTED property (geen setter)
public bool HasPhoto => PhotoPreview != null;

// Maar UI weet niet dat HasPhoto veranderd als PhotoPreview verandert!
public ImageSource? PhotoPreview
{
    set
    {
        if (SetProperty(ref photoPreview, value))
        {
            OnPropertyChanged(nameof(HasPhoto));  // HANDMATIG triggeren!
        }
    }
}
```

---

**5. AsyncRelayCommand met CanExecute**
```csharp
// Command is alleen enabled als aan voorwaarden voldaan
SaveCommand = new AsyncRelayCommand(SaveStop, CanSave);

private bool CanSave()
{
    return HasPhoto                        // Foto verplicht
        && !string.IsNullOrWhiteSpace(Title)  // Titel verplicht
        && !IsAnalyzing;                   // Niet tijdens analyse
}

// NA property wijziging: update button state handmatig!
((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
```

---

**6. MainThread voor UI updates (na async)**
```csharp
// GPS draait op background thread
var location = await _geolocationService.GetCurrentLocationAsync();

// UI updates MOETEN op MainThread!
MainThread.BeginInvokeOnMainThread(() =>
{
    Latitude = location.Latitude;
    Longitude = location.Longitude;
    Address = placemark?.Locality;
});
```

---

**7. Retry Pattern voor Android Camera**
```csharp
// In ViewModel - simpel!
private async Task CapturePhoto()
{
    var bytes = await _photoService.CapturePhotoAsync();  // Retry zit IN PhotoService
    if (bytes != null)
        await ProcessPhoto(bytes);
}

// In PhotoService - retry pattern verborgen
public async Task<byte[]?> CapturePhotoAsync()
{
    var photo = await MediaPicker.Default.CapturePhotoAsync();
    await Task.Delay(100);  // Android hersteltijd

    try {
        return await ResizePhotoStreamAsync(photo);
    } catch {
        await Task.Delay(200);
        return await ResizePhotoStreamAsync(photo);  // Retry!
    }
}
```
**Waarom in PhotoService?** Retry logica hoort bij de service, niet in elke ViewModel.

---

**8. finally Block voor Cleanup**
```csharp
private async Task AnalyzePhoto()
{
    try
    {
        IsAnalyzing = true;  // Toon spinner, disable buttons
        var analysis = await _analyzeImageService.AnalyzePhotoAsync(PhotoData);
        // ...
    }
    finally
    {
        // ALTIJD uitvoeren, ook bij exception!
        IsAnalyzing = false;
        ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
    }
}
```

---

**9. Lokale File Storage + Galerij**

Foto's worden op **twee plekken** opgeslagen:
1. **App folder** → voor de app zelf (snel laden, database referentie)
2. **Telefoon galerij** → voor de gebruiker (backup, delen, blijft na uninstall)

```csharp
using NativeMedia;  // Xamarin.MediaGallery package

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
        await MediaGallery.SaveAsync(MediaFileType.Image, PhotoData, fileName);

        return filePath;
    }
    catch
    {
        return string.Empty;
    }
}
```

**NuGet package:** `Xamarin.MediaGallery`

**Voordelen galerij opslag:**
- Foto's blijven bewaard na app uninstall
- Gebruiker kan foto's delen via andere apps
- Automatische backup naar cloud (Google Photos, iCloud)

---

**10. Graceful Fallbacks**
```csharp
// GPS mislukt? Geen probleem - gebruiker kan handmatig invullen
catch { /* Fallback: velden blijven leeg */ }

// AI mislukt? Fallback naar handmatige titel
if (analysis == null && string.IsNullOrEmpty(Title))
{
    Title = "Nieuwe stop";  // Fallback waarde
}
```

---

> [!warning] Common Pitfall: NotifyCanExecuteChanged
> Als CanExecute afhankelijk is van properties, moet je **handmatig** `NotifyCanExecuteChanged()` aanroepen bij property wijzigingen!
>
> ```csharp
> // FOUT: Button blijft disabled
> HasPhoto = true;
>
> // CORRECT: Button wordt enabled
> HasPhoto = true;
> ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
> ```

---

## 4. StopDetailViewModel - Stop detail

**Interface:** `IStopDetailViewModel.cs`

```csharp
public interface IStopDetailViewModel
{
    TripStop? CurrentStop { get; set; }
    ICommand GoBackCommand { get; set; }
    ICommand EditCommand { get; set; }
    ICommand DeleteCommand { get; set; }
    ICommand ShowStopOnMapCommand { get; set; }
}
```

**Implementatie:** `StopDetailViewModel.cs` (VOLLEDIGE CODE)

```csharp
public class StopDetailViewModel : ObservableRecipient, IRecipient<StopSelectedMessage>, IRecipient<RefreshDataMessage>, IStopDetailViewModel
{
    private readonly INavigationService _navigationService;

    // ═══════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════

    private TripStop? currentStop;
    public TripStop? CurrentStop
    {
        get => currentStop;
        set => SetProperty(ref currentStop, value);
    }

    // ═══════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════

    public ICommand GoBackCommand { get; set; }
    public ICommand EditCommand { get; set; }
    public ICommand DeleteCommand { get; set; }
    public ICommand ShowStopOnMapCommand { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════

    public StopDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        // Registreer voor messages
        Messenger.Register<StopDetailViewModel, StopSelectedMessage>(this, (r, m) => r.Receive(m));
        Messenger.Register<StopDetailViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

        BindCommands();
    }

    // ═══════════════════════════════════════════════════════════
    // MESSAGE HANDLERS
    // ═══════════════════════════════════════════════════════════

    public void Receive(StopSelectedMessage message)
    {
        CurrentStop = message.Value;
    }

    public void Receive(RefreshDataMessage message)
    {
        // Herlaad stop data na edit
        _ = RefreshCurrentStop();
    }

    private async Task RefreshCurrentStop()
    {
        if (CurrentStop == null) return;

        try
        {
            var stopService = new TripStopDataService();
            var refreshedStop = await stopService.GetAsync(CurrentStop.Id);
            if (refreshedStop != null)
            {
                CurrentStop = refreshedStop;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing stop: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // COMMAND BINDING
    // ═══════════════════════════════════════════════════════════

    private void BindCommands()
    {
        GoBackCommand = new AsyncRelayCommand(GoBack);
        EditCommand = new AsyncRelayCommand(EditStop);
        DeleteCommand = new AsyncRelayCommand(DeleteStop);
        ShowStopOnMapCommand = new AsyncRelayCommand(ShowStopOnMap);
    }

    // ═══════════════════════════════════════════════════════════
    // COMMAND HANDLERS
    // ═══════════════════════════════════════════════════════════

    private async Task GoBack()
    {
        await _navigationService.NavigateBackAsync();
    }

    private async Task EditStop()
    {
        if (CurrentStop == null) return;

        // Navigeer naar EditStopPage en stuur message met data
        await _navigationService.NavigateToEditStopPageAsync();
        WeakReferenceMessenger.Default.Send(new StopEditMessage(CurrentStop));
    }

    private async Task DeleteStop()
    {
        if (CurrentStop == null) return;

        // Bevestiging vragen
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Delete Stop",
            $"Are you sure you want to delete '{CurrentStop.Title}'?",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            var stopService = new TripStopDataService();
            await stopService.DeleteAsync(CurrentStop.Id);
            WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting stop: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                "Could not delete stop. Please try again.",
                "OK");
        }
    }

    private async Task ShowStopOnMap()
    {
        if (CurrentStop == null) return;

        // EERST navigeren, DAN message sturen (zodat MapViewModel al geregistreerd is)
        await _navigationService.NavigateToMapPageAsync();
        WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(
            new List<TripStop> { CurrentStop },
            CurrentStop.Title));
    }
}
```

### Key Concepts

**1. RefreshDataMessage voor data refresh**
```csharp
// StopDetailViewModel luistert naar RefreshDataMessage
// Na een edit in EditStopPage wordt data herladen
public void Receive(RefreshDataMessage message)
{
    _ = RefreshCurrentStop();
}
```

**2. Bevestigingsdialog voor Delete**
```csharp
// DisplayAlert met 2 buttons returnt bool
var confirm = await Application.Current!.MainPage!.DisplayAlert(
    "Delete Stop",                                    // Titel
    $"Are you sure you want to delete '{CurrentStop.Title}'?",  // Bericht
    "Delete",                                         // Accept button (true)
    "Cancel");                                        // Cancel button (false)

if (!confirm) return;  // User clicked Cancel
```

**3. ShowStopOnMap - Navigatie Order**
```csharp
// BELANGRIJK: Navigeer EERST, dan pas message sturen!
await _navigationService.NavigateToMapPageAsync();
WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(
    new List<TripStop> { CurrentStop },  // Single stop als List
    CurrentStop.Title));
```

---

## 5. Overige ViewModels (Fase-specifiek)

De volgende ViewModels volgen **dezelfde patronen** als hierboven, maar zijn gedocumenteerd in hun eigen fase-documenten:

| ViewModel | Fase | Document | Bijzonderheden |
|-----------|------|----------|----------------|
| **AddTripViewModel** | 10 | [[10-add-trip-page]] | Formulier voor nieuwe trip, PhotoService integratie |
| **EditTripViewModel** | 11 | [[11-trip-delete-edit]] | `IRecipient<TripEditMessage>`, PutAsync voor update |
| **EditStopViewModel** | 14 | [[14-stop-editing]] | Forward geocoding, AI Describe, DataTrigger |
| **MapViewModel** | 13 | [[13-mappage]] | `IRecipient<ShowStopsOnMapMessage>`, Mapsui integratie |

> [!tip] Examenvraag: Welke ViewModels gebruiken messaging?
> **Alle ViewModels** die data ontvangen via navigatie gebruiken `IRecipient<T>`:
> - `TripDetailViewModel` → `IRecipient<TripSelectedMessage>`
> - `AddStopViewModel` → `IRecipient<TripSelectedMessage>`
> - `StopDetailViewModel` → `IRecipient<StopSelectedMessage>`
> - `EditTripViewModel` → `IRecipient<TripEditMessage>`
> - `EditStopViewModel` → `IRecipient<StopEditMessage>`
> - `MapViewModel` → `IRecipient<ShowStopsOnMapMessage>`
>
> **Alleen TripsViewModel** ontvangt GEEN navigatie-data (het is de hoofdpagina), maar luistert wel naar `RefreshDataMessage`.

---

## 6. Message Flow Diagrams

> **Zie ook:** [[#Messages Uitgelegd]] voor uitleg over messages en [[#Message Class Implementaties]] voor de code.

### Flow Diagrams

**Basis Flow: Trip Detail bekijken**
```
TripsViewModel                      TripDetailViewModel
     │                                     │
     │ ViewTripCommand                     │
     ├─> NavigateToTripDetailPageAsync()   │
     └─> Send(TripSelectedMessage) ───────►│ Receive(TripSelected)
                                           │   → LoadTripStopsAsync()
```

**Add Stop Flow**
```
TripDetailViewModel                 AddStopViewModel
     │                                     │
     │ AddStopCommand                      │
     ├─> NavigateToAddStopPageAsync()      │
     └─> Send(TripSelectedMessage) ───────►│ Receive(TripSelected)
                                           │   → ProcessPhoto()
                                           │   → SaveStop()
                                           │   → Send(RefreshDataMessage)
                                           │              │
     │◄────────────────────────────────────┘              │
     │ Receive(RefreshTrips)                              │
     │   → LoadTripStopsAsync()                           │
```

**Add Trip Flow**
```
TripsViewModel                      AddTripViewModel
     │                                     │
     │ AddTripCommand                      │
     └─> NavigateToAddTripPageAsync()      │
                                           │ (geen message nodig - nieuw formulier)
                                           │   → CapturePhoto() / PickPhoto()
                                           │   → Vul Name, Description in
                                           │   → SaveTrip()
                                           │   → Send(RefreshDataMessage)
                                           │              │
     │◄────────────────────────────────────┘              │
     │ Receive(RefreshDataMessage)                        │
     │   → LoadTripsAsync()                               │
```

> [!note] Geen message bij Add Trip
> Anders dan Edit Trip, stuurt Add Trip **geen message** bij navigatie.
> Reden: er is geen bestaande trip om door te geven - het is een leeg formulier.

**Edit Trip Flow**
```
TripsViewModel                      EditTripViewModel
     │                                     │
     │ EditTripCommand                     │
     ├─> NavigateToEditTripPageAsync()     │
     └─> Send(TripEditMessage) ───────────►│ Receive(TripEditMessage)
                                           │   → SetTrip(trip data)
                                           │   → SaveTrip()
                                           │   → Send(RefreshDataMessage)
                                           │              │
     │◄────────────────────────────────────┘              │
     │ Receive(RefreshTrips)                              │
     │   → LoadTripsAsync()                               │
```

**Map Flow (vanuit 3 contexten)**
```
TripsViewModel ─────────────────┐
TripDetailViewModel ────────────┼──► MapViewModel
StopDetailViewModel ────────────┘         │
     │                                    │
     │ ShowAllOnMapCommand /              │
     │ ShowTripOnMapCommand /             │
     │ ShowStopOnMapCommand               │
     ├─> NavigateToMapPageAsync()         │
     └─> Send(ShowStopsOnMapMessage) ────►│ Receive(ShowStopsOnMapMessage)
                                          │   → DisplayStopsOnMap()
```

**Complete Message Overzicht**
```
┌──────────────────────────────────────────────────────────────────┐
│                    MESSAGE ROUTING TABLE                          │
├────────────────────────┬─────────────────────────────────────────┤
│ Message                │ Luisteraars (Receivers)                 │
├────────────────────────┼─────────────────────────────────────────┤
│ RefreshDataMessage    │ TripsViewModel, TripDetailViewModel,    │
│                        │ StopDetailViewModel                     │
├────────────────────────┼─────────────────────────────────────────┤
│ TripSelectedMessage    │ TripDetailViewModel, AddStopViewModel   │
├────────────────────────┼─────────────────────────────────────────┤
│ StopSelectedMessage    │ StopDetailViewModel                     │
├────────────────────────┼─────────────────────────────────────────┤
│ TripEditMessage        │ EditTripViewModel                       │
├────────────────────────┼─────────────────────────────────────────┤
│ StopEditMessage        │ EditStopViewModel                       │
├────────────────────────┼─────────────────────────────────────────┤
│ ShowStopsOnMapMessage  │ MapViewModel                            │
└────────────────────────┴─────────────────────────────────────────┘
```

> [!tip] Examenvraag
> **Vraag:** Wanneer gebruik je Messaging vs Navigation Parameters?
>
> **Antwoord:**
> - **Messaging**: Losse koppeling. Sender weet NIET wie luistert. Gebruikt voor:
>   - Refresh triggers (RefreshDataMessage)
>   - Broadcast events (meerdere listeners)
>   - Cross-ViewModel communicatie
>
> - **Navigation Parameters**: Directe koppeling. Gebruikt voor:
>   - Eenvoudige data passing (1 waarde)
>   - QueryProperty binding in .NET MAUI
>
> **TripTracker keuze:** Messaging, omdat meerdere ViewModels moeten refreshen.

---

## 6b. Fundamentele C# Concepten (Examenvragen!)

Deze sectie legt de **basis C# concepten** uit die je moet begrijpen voor ViewModels.

### Interface = Contract

Een interface is een **contract** - een belofte over wat een class kan doen.

```csharp
// Het CONTRACT (interface)
public interface ITripDataService
{
    Task<Trip?> GetAsync(int id);
    Task<List<Trip>> GetAllAsync();
    Task PostAsync(Trip trip);
}

// IMPLEMENTATIE (ondertekent het contract)
public class TripDataService : ITripDataService
{
    public async Task<Trip?> GetAsync(int id) { /* API call */ }
    public async Task<List<Trip>> GetAllAsync() { /* API call */ }
    public async Task PostAsync(Trip trip) { /* API call */ }
}
```

**Wat zegt het contract?**

| Interface zegt | Class moet leveren |
|----------------|-------------------|
| `Task<Trip?> GetAsync(int id)` | Een method die een Trip ophaalt |
| `Task<List<Trip>> GetAllAsync()` | Een method die alle Trips ophaalt |

**Alleen de handtekening** - niet HOE het moet werken.

> [!tip] Examenvraag
> **Vraag:** Waarom gebruiken we interfaces voor services?
>
> **Antwoord:**
> 1. **Testbaarheid** - Je kunt een fake implementatie maken voor tests
> 2. **Loose coupling** - ViewModel kent alleen het contract, niet de implementatie
> 3. **Dependency Injection** - DI container kan interface koppelen aan implementatie

---

### Dependency Injection = Constructor Parameters

**DI betekent:** "Vraag wat je nodig hebt via de constructor, iemand anders regelt het."

```csharp
public class TripsViewModel
{
    private readonly ITripDataService _tripDataService;      // Opslaan
    private readonly INavigationService _navigationService;  // Opslaan

    // Constructor: "Ik heb deze services NODIG"
    public TripsViewModel(ITripDataService tripDataService, INavigationService navigationService)
    {
        _tripDataService = tripDataService;      // Krijgt het van buitenaf
        _navigationService = navigationService;  // Flexibel!
    }
}
```

**Hoe werkt het?**

```
┌─────────────────────────────────────────────────────────────┐
│  DI CONTAINER (MauiProgram.cs)                              │
│                                                             │
│  Registraties:                                              │
│  ┌─────────────────────┬──────────────────────┐            │
│  │ ITripDataService    │ → TripDataService    │            │
│  │ INavigationService  │ → NavigationService  │            │
│  │ ITripsViewModel     │ → TripsViewModel     │            │
│  └─────────────────────┴──────────────────────┘            │
└─────────────────────────────────────────────────────────────┘
                          │
                          │ "Geef me ITripsViewModel"
                          ▼
┌─────────────────────────────────────────────────────────────┐
│  Container maakt AUTOMATISCH:                               │
│                                                             │
│  new TripsViewModel(                                        │
│      container.Get<ITripDataService>(),  ← haalt op        │
│      container.Get<INavigationService>() ← haalt op        │
│  )                                                          │
└─────────────────────────────────────────────────────────────┘
```

**Zonder DI (de oude, slechte manier):**

```csharp
// ❌ ZONDER DI - ViewModel maakt zelf dependencies
public TripsViewModel()
{
    _tripDataService = new TripDataService();      // Hardcoded!
    _navigationService = new NavigationService();  // Niet testbaar!
}
```

**Met DI (wat wij gebruiken):**

```csharp
// ✅ MET DI - ViewModel vraagt dependencies
public TripsViewModel(ITripDataService tripDataService, INavigationService navigationService)
{
    _tripDataService = tripDataService;      // Krijgt het van buitenaf
    _navigationService = navigationService;  // Kan fake zijn voor tests!
}
```

> [!tip] Examenvraag
> **Vraag:** Wat is Dependency Injection en waarom gebruiken we het?
>
> **Antwoord:** DI = services worden via de constructor geïnjecteerd (meegegeven) in plaats van zelf aangemaakt. Voordelen:
> 1. **Testbaarheid** - Inject fake services in unit tests
> 2. **Loose coupling** - Class kent alleen interface, niet implementatie
> 3. **Flexibiliteit** - Makkelijk implementatie verwisselen

---

### C# Syntax: Generics `<T>`

Generics = een "variabele" voor types. De `<T>` is een placeholder.

```csharp
// ValueChangedMessage is gedefinieerd als:
public class ValueChangedMessage<T>  // T = "vul later in welk type"
{
    public T Value { get; }          // Value is van type T
}

// Wanneer jij schrijft <Trip>:
ValueChangedMessage<Trip>   // T wordt vervangen door Trip
// Nu is .Value van type Trip!

// Andere voorbeelden:
List<string>                // Een lijst van strings
List<int>                   // Een lijst van ints
ValueChangedMessage<bool>   // .Value is bool
ValueChangedMessage<Trip>   // .Value is Trip
```

---

### C# Syntax: Inheritance `: base()`

```csharp
public class TripSelectedMessage : ValueChangedMessage<Trip>
{
    public TripSelectedMessage(Trip trip) : base(trip) { }
}
```

**Stuk voor stuk:**

| Code | Betekenis |
|------|-----------|
| `: ValueChangedMessage<Trip>` | "Ik erf van ValueChangedMessage" |
| `(Trip trip)` | Constructor parameter |
| `: base(trip)` | "Geef `trip` door aan parent constructor" |

**Waarom `: base(trip)`?**

```csharp
// Parent class (ValueChangedMessage) heeft een constructor die T verwacht:
public class ValueChangedMessage<T>
{
    public T Value { get; }

    public ValueChangedMessage(T value)  // ← DIT roep je aan met base()
    {
        Value = value;  // Slaat de waarde op
    }
}

// Jouw class MOET die parent constructor aanroepen:
public TripSelectedMessage(Trip trip) : base(trip) { }
//                                      ↑
//              "Roep ValueChangedMessage(trip) aan"
```

**Resultaat:**

```csharp
var msg = new TripSelectedMessage(mijnTrip);
msg.Value  // → mijnTrip (opgeslagen door parent class)
```

> [!tip] Examenvraag
> **Vraag:** Leg uit wat `: base(trip)` doet in een Message class.
>
> **Antwoord:** `: base(trip)` roept de constructor van de parent class (`ValueChangedMessage<T>`) aan en geeft de `trip` parameter mee. De parent class slaat deze waarde op in de `.Value` property, zodat ontvangers van de message er toegang toe hebben.

---

## 7. Dependency Injection Registratie

**MauiProgram.cs** - ViewModels als services (VOLLEDIGE CODE)

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp() // Vereist voor Mapsui
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ===== Services registreren =====

        // Navigation service (Transient = nieuwe instantie per request)
        builder.Services.AddTransient<INavigationService, NavigationService>();

        // Smart Stop Capture services (Singleton = hergebruik instantie)
        builder.Services.AddSingleton<IPhotoService, PhotoService>();
        builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
        builder.Services.AddSingleton<IGeocodingService, GeocodingService>();
        builder.Services.AddSingleton<IAnalyzeImageService, AnalyzeImageService>();

        // ===== Pages en ViewModels registreren =====

        // Trips pagina (hoofdpagina - Singleton)
        builder.Services.AddSingleton<TripsPage>();
        builder.Services.AddSingleton<ITripsViewModel, TripsViewModel>();

        // Trip detail pagina (Transient)
        builder.Services.AddTransient<TripDetailPage>();
        builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();

        // Add stop pagina (Transient)
        builder.Services.AddTransient<AddStopPage>();
        builder.Services.AddTransient<IAddStopViewModel, AddStopViewModel>();

        // Add trip pagina (Transient)
        builder.Services.AddTransient<AddTripPage>();
        builder.Services.AddTransient<IAddTripViewModel, AddTripViewModel>();

        // Edit trip pagina (Transient)
        builder.Services.AddTransient<EditTripPage>();
        builder.Services.AddTransient<IEditTripViewModel, EditTripViewModel>();

        // Stop detail pagina (Transient)
        builder.Services.AddTransient<StopDetailPage>();
        builder.Services.AddTransient<IStopDetailViewModel, StopDetailViewModel>();

        // Edit stop pagina (Transient)
        builder.Services.AddTransient<EditStopPage>();
        builder.Services.AddTransient<IEditStopViewModel, EditStopViewModel>();

        // Map pagina (Transient)
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<IMapViewModel, MapViewModel>();

        return builder.Build();
    }
}
```

### Overzicht: Alle Geregistreerde Pages & ViewModels

| Page | ViewModel | Lifetime | Reden |
|------|-----------|----------|-------|
| TripsPage | TripsViewModel | **Singleton** | Hoofdpagina, behoudt state |
| TripDetailPage | TripDetailViewModel | Transient | Schone state per trip |
| AddTripPage | AddTripViewModel | Transient | Nieuw formulier |
| EditTripPage | EditTripViewModel | Transient | Schone state per edit |
| AddStopPage | AddStopViewModel | Transient | Nieuw formulier |
| StopDetailPage | StopDetailViewModel | Transient | Schone state per stop |
| EditStopPage | EditStopViewModel | Transient | Schone state per edit |
| MapPage | MapViewModel | Transient | Schone kaart per view |

### Service Lifetimes

| Lifetime | Beschrijving | Gebruik voor |
|----------|-------------|-------------|
| **Singleton** | 1 instantie voor hele app | Services, hoofdpagina's (TripsPage) |
| **Transient** | Nieuwe instantie bij elke request | Detail pagina's, formulieren |
| **Scoped** | 1 instantie per scope | (Niet gebruikt in MAUI) |

**Waarom Singleton voor TripsPage?**
- TripsViewModel behoudt state (Trips lijst blijft in geheugen)
- Geen herlaad nodig bij terugkeren naar hoofdpagina
- Betere performance

**Waarom Transient voor TripDetailPage?**
- Nieuwe instantie = schone state
- Voorkomt stale data van vorige navigatie
- Vermindert geheugen gebruik

> [!warning] Common Mistake: Singleton vs Transient
> ```csharp
> // FOUT: Detail page als Singleton
> builder.Services.AddSingleton<TripDetailPage>();
> // Probleem: Oude data blijft hangen bij navigatie naar andere trip!
>
> // CORRECT: Detail page als Transient
> builder.Services.AddTransient<TripDetailPage>();
> // Elke navigatie = schone state
> ```

---

## 8. AsyncRelayCommand Deep Dive

**AsyncRelayCommand** = async versie van RelayCommand (CommunityToolkit.Mvvm)

### Basic Usage

```csharp
// Zonder parameter
public ICommand SaveCommand { get; set; }
SaveCommand = new AsyncRelayCommand(SaveAsync);

private async Task SaveAsync()
{
    await _apiService.SaveDataAsync();
}
```

### Met Parameter

```csharp
// Met parameter (Trip)
public ICommand ViewTripCommand { get; set; }
ViewTripCommand = new AsyncRelayCommand<Trip>(GoToDetailAsync);

private async Task GoToDetailAsync(Trip? trip)
{
    if (trip != null)
    {
        await _navigationService.NavigateAsync(trip.Id);
    }
}
```

### Met CanExecute

```csharp
// CanExecute delegate
public ICommand SaveCommand { get; set; }
SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);

private bool CanSave()
{
    return !string.IsNullOrWhiteSpace(Title) && !IsSaving;
}

// Update button state bij property wijzigingen
private string title = "";
public string Title
{
    get => title;
    set
    {
        if (SetProperty(ref title, value))
        {
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
        }
    }
}
```

### Error Handling

```csharp
private async Task SaveAsync()
{
    try
    {
        IsSaving = true;
        await _apiService.SaveAsync();
    }
    catch (Exception ex)
    {
        // Log error
        System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");

        // Toon gebruiker feedback (optioneel)
        await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
    }
    finally
    {
        IsSaving = false;
        ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
    }
}
```

> [!tip] Examenvraag
> **Vraag:** Wat is het verschil tussen `RelayCommand` en `AsyncRelayCommand`?
>
> **Antwoord:**
> - **RelayCommand**: Synchrone methode (`void Execute()`)
>   - Gebruik voor: UI state changes, navigatie zonder API calls
>
> - **AsyncRelayCommand**: Asynchrone methode (`Task ExecuteAsync()`)
>   - Gebruik voor: API calls, database operations, file I/O
>   - Ondersteunt `await` zonder blocking UI thread
>
> **BELANGRIJK:** Gebruik ALTIJD `AsyncRelayCommand` voor methods met `await`!

---

## Examenvragen - ViewModels

### Vraag 1: ObservableRecipient

**Vraag:** Wat zijn de 2 hoofdfuncties van `ObservableRecipient`?

**Antwoord:**
1. **Property Change Notification** (via `ObservableObject`)
   - `SetProperty()` triggert `PropertyChanged` events
   - UI bindings updaten automatisch
2. **Messaging Support** (via `IRecipient<TMessage>`)
   - `Messenger.Register()` voor message subscriptions
   - `Receive()` method voor message handling

---

### Vraag 2: SetProperty Pattern

**Vraag:** Schrijf de correcte implementatie van een property `Title` met SetProperty.

**Antwoord:**
```csharp
private string title = string.Empty;
public string Title
{
    get => title;
    set => SetProperty(ref title, value);
}
```

**Wat gebeurt er:**
1. `SetProperty` vergelijkt oude en nieuwe waarde
2. Als verschillend: `title = value`
3. Triggert `PropertyChanged` event
4. UI bindings updaten

---

### Vraag 3: ObservableCollection

**Vraag:** Waarom gebruiken we `ObservableCollection<T>` in plaats van `List<T>` voor UI bindings?

**Antwoord:**
- `List<T>`: Geen notifications. Add/Remove triggert GEEN UI update.
- `ObservableCollection<T>`: Implementeert `INotifyCollectionChanged`. Elke wijziging (Add/Remove/Clear) triggert UI update.

**Voorbeeld:**
```csharp
// ✅ CORRECT
public ObservableCollection<Trip> Trips { get; set; }
Trips.Add(newTrip);  // CollectionView update automatisch

// ❌ FOUT
public List<Trip> Trips { get; set; }
Trips.Add(newTrip);  // UI blijft leeg!
```

---

### Vraag 4: Message Registration

**Vraag:** Hoe registreer je een ViewModel voor een RefreshDataMessage?

**Antwoord:**
```csharp
// In constructor:
Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

// Implementeer handler:
public void Receive(RefreshDataMessage message)
{
    _ = LoadTripsAsync();
}
```

**Parameters uitleg:**
- `TripsViewModel`: De recipient class (deze ViewModel)
- `RefreshDataMessage`: Type message om naar te luisteren
- `this`: De instantie die registreert
- `(r, m) => r.Receive(m)`: Lambda die Receive() aanroept

---

### Vraag 5: WeakReferenceMessenger

**Vraag:** Hoe verstuur je een RefreshDataMessage naar alle listeners?

**Antwoord:**
```csharp
WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
```

**Belangrijk:**
- `WeakReferenceMessenger.Default`: Globale messenger instantie
- `Send()`: Broadcast naar ALLE geregistreerde listeners
- Sender weet NIET wie luistert (loose coupling)

---

### Vraag 6: AsyncRelayCommand CanExecute

**Vraag:** Implementeer een SaveCommand die alleen enabled is als Title niet leeg is en IsSaving false.

**Antwoord:**
```csharp
public ICommand SaveCommand { get; set; }

// In BindCommands():
SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);

private bool CanSave()
{
    return !string.IsNullOrWhiteSpace(Title) && !IsSaving;
}

// Update na property wijziging:
public string Title
{
    get => title;
    set
    {
        if (SetProperty(ref title, value))
        {
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
        }
    }
}
```

---

### Vraag 7: Dependency Injection

**Vraag:** Wat is het verschil tussen Singleton en Transient service lifetime?

**Antwoord:**

| Lifetime | Instanties | Gebruik voor | Voorbeeld |
|----------|-----------|-------------|-----------|
| **Singleton** | 1 voor hele app | Services, hoofdpagina's | TripsPage, PhotoService |
| **Transient** | Nieuwe per request | Detail pagina's | TripDetailPage |

**Code:**
```csharp
// Singleton: Behoud state
builder.Services.AddSingleton<ITripsViewModel, TripsViewModel>();

// Transient: Schone state bij elke navigatie
builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();
```

---

### Vraag 8: Computed Properties

**Vraag:** Implementeer een computed property `HasPhoto` die true is als PhotoPreview niet null is. Zorg voor correcte PropertyChanged notifications.

**Antwoord:**
```csharp
// Computed property (geen setter)
public bool HasPhoto => PhotoPreview != null;

// PhotoPreview triggert HasPhoto update
private ImageSource? photoPreview;
public ImageSource? PhotoPreview
{
    get => photoPreview;
    set
    {
        if (SetProperty(ref photoPreview, value))
        {
            OnPropertyChanged(nameof(HasPhoto));  // BELANGRIJK!
        }
    }
}
```

---

### Vraag 9: Fire-and-Forget Pattern

**Vraag:** Wat betekent `_ = LoadTripsAsync();` en wanneer gebruik je dit?

**Antwoord:**
- **Betekenis:** Start async method ZONDER te wachten op resultaat
- `_` = discard operator (negeer return value)
- **Gebruik voor:**
  - Event handlers (Receive() methods)
  - Background tasks waar resultaat niet nodig is
  - Non-blocking UI updates

**Wanneer NIET gebruiken:**
```csharp
// ❌ FOUT: Verwacht resultaat
_ = await LoadTripsAsync();  // await + _ = zinloos

// ✅ CORRECT: Wacht op resultaat
await LoadTripsAsync();

// ✅ CORRECT: Fire-and-forget in event handler
public void Receive(RefreshDataMessage message)
{
    _ = LoadTripsAsync();  // Non-blocking
}
```

---

### Vraag 10: MainThread Updates

**Vraag:** Waarom moet je MainThread.BeginInvokeOnMainThread() gebruiken voor UI updates vanuit async methods?

**Antwoord:**
UI updates MOETEN draaien op de **UI thread** (main thread). Async methods draaien vaak op **background threads**.

**Probleem:**
```csharp
// FOUT: Crash als dit op background thread draait!
var location = await _geolocationService.GetCurrentLocationAsync();
Latitude = location.Latitude;  // ❌ Not on UI thread!
```

**Oplossing:**
```csharp
var location = await _geolocationService.GetCurrentLocationAsync();

MainThread.BeginInvokeOnMainThread(() =>
{
    Latitude = location.Latitude;   // ✅ Safe: UI thread
    Longitude = location.Longitude;
});
```

---

### Vraag 11: SafariSnap vs TripTracker - Waarom MainThread?

**Vraag:** SafariSnap uit de cursus gebruikt geen `MainThread.BeginInvokeOnMainThread()`. Waarom heeft TripTracker het wel nodig?

**Antwoord:**

| Aspect | SafariSnap | TripTracker |
|--------|------------|-------------|
| Data bron | Hardcoded lijst | Echte API (HTTP) |
| Method type | Synchrone (`return data`) | Async (`await GetAllAsync()`) |
| Thread switch | Nee | Ja (na await) |
| MainThread nodig | ❌ Nee | ✅ Ja |

**SafariSnap - Geen MainThread nodig:**
```csharp
// SYNCHROON - geen await, geen thread switch
public static BigFiveAnimal? GetBigFiveAnimalByTag(string tag)
{
    return new List<BigFiveAnimal> { ... }.FirstOrDefault(a => a.Tag == tag);
}
// Draait volledig op main thread → UI update veilig
```

**TripTracker - MainThread WEL nodig:**
```csharp
// ASYNC - await kan thread switchen!
var stops = await tripService.GetTripStopsAsync(CurrentTrip.Id);
// Na await: mogelijk op background thread!

MainThread.BeginInvokeOnMainThread(() =>
{
    TripStops = new ObservableCollection<TripStop>(stops);  // Veilig
});
```

**Visueel:**
```
SafariSnap:        Main Thread → GetByTag() → UI update (alles main thread)

TripTracker:       Main Thread → await API ──→ Background Thread
                                                     │
                                   MainThread.Begin... ← terug naar UI thread
```

**Regel:** Gebruik `MainThread` alleen bij **echte async operaties** (API, GPS, file I/O).

---

## Samenvatting

### ViewModels Checklist

- ✅ Erven van `ObservableRecipient`
- ✅ Implementeren interface (ITripsViewModel, etc.)
- ✅ Properties met `SetProperty()` voor data binding
- ✅ `ObservableCollection<T>` voor lijsten
- ✅ `AsyncRelayCommand` voor async operations
- ✅ Message registration in constructor
- ✅ `WeakReferenceMessenger.Default.Send()` voor messaging
- ✅ DI registratie in MauiProgram.cs
- ✅ Singleton voor hoofdpagina's, Transient voor details
- ✅ `NotifyCanExecuteChanged()` na property wijzigingen
- ✅ `MainThread.BeginInvokeOnMainThread()` voor UI updates

### Common Patterns

**1. Property met SetProperty**
```csharp
private string title = "";
public string Title
{
    get => title;
    set => SetProperty(ref title, value);
}
```

**2. Command binding**
```csharp
public ICommand SaveCommand { get; set; }
SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
```

**3. Message handling**
```csharp
// Register
Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));

// Receive
public void Receive(RefreshDataMessage message)
{
    _ = LoadTripsAsync();
}

// Send
WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));
```

**4. DI registratie**
```csharp
builder.Services.AddSingleton<ITripsViewModel, TripsViewModel>();
builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();
```

**5. Navigatie + Message Order** (BELANGRIJK!)
```csharp
// ALTIJD: eerst navigeren, dan message sturen
await _navigationService.NavigateToMapPageAsync();
WeakReferenceMessenger.Default.Send(new ShowStopsOnMapMessage(stops, title));
// Anders is de target ViewModel nog niet geregistreerd!
```

**6. Bevestigingsdialog**
```csharp
var confirm = await Application.Current!.MainPage!.DisplayAlert(
    "Delete", "Are you sure?", "Delete", "Cancel");
if (!confirm) return;
```

**7. Computed property met OnPropertyChanged**
```csharp
public bool HasPhoto => PhotoPreview != null;

public ImageSource? PhotoPreview
{
    get => photoPreview;
    set
    {
        if (SetProperty(ref photoPreview, value))
            OnPropertyChanged(nameof(HasPhoto));  // Trigger dependent property!
    }
}
```

---

## Referenties

- **CommunityToolkit.Mvvm Docs**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
- **ObservableRecipient**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observablerecipient
- **WeakReferenceMessenger**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger
- **Cursus**: Les 3 - ViewModels (SafariSnap)

---

**Volgende Fase:** [[05-views-navigatie]] - Views en XAML binding
