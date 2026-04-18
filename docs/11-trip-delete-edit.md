---
fase: 11
status: Voltooid
tags:
  - swipeview
  - delete
  - edit
  - messaging
  - mvvm
created: 2025-12-20
---

# Fase 11: SwipeView delete & edit

## Overzicht

In deze fase hebben we **SwipeView** geïmplementeerd voor het verwijderen en bewerken van trips en stops. Dit is een veelgebruikt mobile UI pattern dat intuïtieve gestures biedt.

> [!info] Cursus Referentie
> SwipeView is onderdeel van .NET MAUI UI controls. Het combineert gestures met commands - een toepassing van MVVM in mobile UI.

---

## Waarom SwipeView?

| Traditioneel | SwipeView |
|--------------|-----------|
| Aparte delete button per item | Verborgen tot swipe |
| Rommelige UI | Cleane interface |
| Geen gesture support | Native mobile feeling |
| Extra screen space nodig | Ruimtebesparend |

**UX Conventie:**
- **Swipe links (←)** = Destructieve actie (Delete, rood)
- **Swipe rechts (→)** = Bewerk actie (Edit, paars)

---

## MVVM architectuur

```
┌─────────────────────────────────────────────────────────────┐
│                    SwipeView Pattern                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   TripsPage.xaml                  TripsViewModel.cs          │
│   ┌─────────────────┐             ┌─────────────────┐       │
│   │ <SwipeView>     │             │ DeleteCommand   │       │
│   │   LeftItems:    │───Binding──▶│   └─DeleteTrip()│       │
│   │     Edit button │             │                 │       │
│   │   RightItems:   │             │ EditCommand     │       │
│   │     Delete btn  │───Binding──▶│   └─EditTrip()  │       │
│   │   <Content>     │             │                 │       │
│   │     Trip card   │             │ Trips collection│       │
│   └─────────────────┘             └─────────────────┘       │
│                                           │                 │
│                                           │ Send Message    │
│                                           ▼                 │
│                               ┌─────────────────────┐       │
│                               │  TripEditMessage    │       │
│                               │  ValueChangedMessage│       │
│                               └─────────────────────┘       │
│                                           │                 │
│                                           │ Receive         │
│                                           ▼                 │
│                               ┌─────────────────────┐       │
│                               │ EditTripViewModel   │       │
│                               │ IRecipient<T>       │       │
│                               └─────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

---

## SwipeView XAML pattern

### Basisstructuur

```xml
<SwipeView>
    <!-- LeftItems = Swipe RECHTS (onthult links) -->
    <SwipeView.LeftItems>
        <SwipeItems>
            <SwipeItem Text="Edit"
                       BackgroundColor="#512BD4"
                       IconImageSource="edit_icon.png"
                       Command="{Binding Source={RelativeSource
                           AncestorType={x:Type viewmodels:TripsViewModel}},
                           Path=EditCommand}"
                       CommandParameter="{Binding .}"/>
        </SwipeItems>
    </SwipeView.LeftItems>

    <!-- RightItems = Swipe LINKS (onthult rechts) -->
    <SwipeView.RightItems>
        <SwipeItems>
            <SwipeItem Text="Delete"
                       BackgroundColor="Red"
                       IconImageSource="delete_icon.png"
                       Command="{Binding Source={RelativeSource
                           AncestorType={x:Type viewmodels:TripsViewModel}},
                           Path=DeleteCommand}"
                       CommandParameter="{Binding .}"/>
        </SwipeItems>
    </SwipeView.RightItems>

    <!-- De content die geswiped wordt -->
    <Frame Padding="10" Margin="5">
        <StackLayout>
            <Label Text="{Binding Name}" FontSize="18"/>
            <Label Text="{Binding Description}" FontSize="14"/>
        </StackLayout>
    </Frame>
</SwipeView>
```

> [!warning] Let Op: LeftItems vs RightItems
> - **LeftItems** = Buttons die **links** verschijnen (swipe naar **rechts**)
> - **RightItems** = Buttons die **rechts** verschijnen (swipe naar **links**)
>
> Dit is contra-intuïtief! Denk eraan: de naam verwijst naar WAAR de buttons komen, niet de swipe richting.

---

## Delete implementatie

### 1. Command in ViewModel

```csharp
// TripsViewModel.cs
public ICommand DeleteCommand { get; private set; }

private void BindCommands()
{
    DeleteCommand = new AsyncRelayCommand<Trip>(DeleteTrip);
    // ... andere commands
}

private async Task DeleteTrip(Trip? trip)
{
    if (trip == null) return;

    // Bevestiging vragen
    var confirm = await Application.Current!.MainPage!.DisplayAlert(
        "Delete Trip",
        $"Are you sure you want to delete '{trip.Name}'?",
        "Delete", "Cancel");

    if (!confirm) return;

    // API call
    var tripService = new TripDataService();
    await tripService.DeleteAsync(trip.Id);

    // Lokaal verwijderen uit collectie
    Trips.Remove(trip);
}
```

### 2. Interface update

```csharp
// ITripsViewModel.cs
public interface ITripsViewModel
{
    ObservableCollection<Trip> Trips { get; set; }
    ObservableCollection<Trip> FilteredTrips { get; set; }
    ICommand DeleteTripCommand { get; set; }
    ICommand EditTripCommand { get; set; }
    // + IsLoading, YearFilters, SelectedYear, ViewTripCommand, etc.
}
```

### Waarom Trips.Remove()?

Na `DeleteAsync()` verwijderen we het item ook uit de lokale `ObservableCollection`:
- UI update is instant (geen API refresh nodig)
- Betere UX (geen flicker)
- `ObservableCollection` triggert automatisch UI update

---

## Edit implementatie met messages

### Probleem: data overdracht bij navigatie

Bij Edit willen we de **complete Trip** doorgeven, niet alleen een ID. Query parameters zijn beperkt tot strings.

### Oplossing: ValueChangedMessage pattern

```csharp
// Messages/TripEditMessage.cs
public class TripEditMessage : ValueChangedMessage<Trip>
{
    public TripEditMessage(Trip trip) : base(trip) { }
}
```

### Flow: edit command → message → EditTripViewModel

```csharp
// TripsViewModel.cs - VERSTUREN
private async Task EditTrip(Trip? trip)
{
    if (trip == null) return;

    // 1. Navigeer naar edit page
    await _navigationService.NavigateToEditTripPageAsync();

    // 2. Stuur trip data via message
    WeakReferenceMessenger.Default.Send(new TripEditMessage(trip));
}

// EditTripViewModel.cs - ONTVANGEN
public class EditTripViewModel : ObservableRecipient, IRecipient<TripEditMessage>
{
    public EditTripViewModel(INavigationService nav, IPhotoService photo)
    {
        // Registreer voor messages
        Messenger.Register<EditTripViewModel, TripEditMessage>(
            this, (r, m) => r.Receive(m));
    }

    public void Receive(TripEditMessage message)
    {
        var trip = message.Value;
        // Vul form in met trip data
        SetTrip(trip.Id, trip.Name, trip.Description,
                trip.StartDate, trip.EndDate, trip.ImageUrl);
    }
}
```

> [!tip] Waarom ObservableRecipient?
> `ObservableRecipient` is een base class die:
> - `ObservableObject` uitbreidt (INotifyPropertyChanged)
> - `Messenger` property biedt voor DI
> - `IsActive` property heeft (aan/uit zetten van messaging)

---

## RelativeSource binding in DataTemplate

Binnen een `DataTemplate` (zoals in CollectionView) is de `BindingContext` het **item** (Trip), niet de ViewModel.

Om commands in de parent ViewModel aan te roepen:

```xml
<!-- FOUT: Bindt aan Trip.DeleteCommand (bestaat niet!) -->
<SwipeItem Command="{Binding DeleteCommand}"/>

<!-- CORRECT: Navigeer naar parent ViewModel -->
<SwipeItem Command="{Binding Source={RelativeSource
    AncestorType={x:Type viewmodels:TripsViewModel}},
    Path=DeleteCommand}"
    CommandParameter="{Binding .}"/>
```

**Uitleg:**
- `Source={RelativeSource AncestorType=...}` → Zoek parent van type X
- `Path=DeleteCommand` → Property op die parent
- `CommandParameter="{Binding .}"` → Stuur huidig item (Trip) mee

---

## Bestanden

### Nieuw gemaakt

| Bestand | Doel |
|---------|------|
| `Messages/TripEditMessage.cs` | Message voor trip data overdracht |
| `ViewModels/IEditTripViewModel.cs` | Interface voor DI |
| `ViewModels/EditTripViewModel.cs` | ViewModel met PutAsync |
| `Views/EditTripPage.xaml` | Edit formulier (zelfde als AddTrip) |
| `Views/EditTripPage.xaml.cs` | Minimale code-behind |

### Gewijzigd

| Bestand | Wijziging |
|---------|-----------|
| `Views/TripsPage.xaml` | SwipeView toegevoegd |
| `Views/TripDetailPage.xaml` | SwipeView voor stops |
| `ViewModels/TripsViewModel.cs` | Delete/Edit commands |
| `ViewModels/ITripsViewModel.cs` | Interface update |
| `Services/INavigationService.cs` | NavigateToEditTripPageAsync() |
| `Services/NavigationService.cs` | Implementatie |
| `MauiProgram.cs` | DI registratie EditTripPage |

---

## API endpoints

| Actie | Endpoint | Method |
|-------|----------|--------|
| Delete trip | `/api/trips/{id}` | DELETE |
| Update trip | `/api/trips/{id}` | PUT |
| Delete stop | `/api/tripstops/{id}` | DELETE |

---

## Cursus compliance

| Vereiste | Status |
|----------|--------|
| MVVM Architecture | ✅ Code-behind leeg |
| Commands met parameters | ✅ AsyncRelayCommand<T> |
| Messaging | ✅ TripEditMessage |
| Generieke ApiService | ✅ DeleteAsync(), PutAsync() |
| NavigationService | ✅ Via DI |

---

## Examenvragen

### Vraag 1: SwipeView LeftItems vs RightItems

**Vraag:** Wat is het verschil tussen `SwipeView.LeftItems` en `SwipeView.RightItems`?

**Antwoord:**
- **LeftItems**: Buttons die aan de **linkerkant** verschijnen wanneer je naar **rechts** swipet
- **RightItems**: Buttons die aan de **rechterkant** verschijnen wanneer je naar **links** swipet

De naam verwijst naar WAAR de buttons komen te staan, niet de richting van de swipe.

**Conventie:**
- Delete (destructief) → RightItems (swipe links, rode kleur)
- Edit (wijzigen) → LeftItems (swipe rechts, primaire kleur)

---

### Vraag 2: RelativeSource in DataTemplate

**Vraag:** Waarom werkt `Command="{Binding DeleteCommand}"` niet binnen een CollectionView DataTemplate?

**Antwoord:**
Binnen een DataTemplate is de BindingContext het **item** (bijv. Trip), niet de ViewModel. De Trip heeft geen DeleteCommand.

**Oplossing: RelativeSource**
```xml
<SwipeItem Command="{Binding Source={RelativeSource
    AncestorType={x:Type viewmodels:TripsViewModel}},
    Path=DeleteCommand}"
    CommandParameter="{Binding .}"/>
```

Dit zoekt naar een parent van type `TripsViewModel` en bindt aan diens `DeleteCommand`. Het huidige item wordt meegegeven via `CommandParameter`.

---

### Vraag 3: ValueChangedMessage pattern

**Vraag:** Waarom gebruiken we `TripEditMessage` in plaats van query parameters voor edit?

**Antwoord:**
```csharp
public class TripEditMessage : ValueChangedMessage<Trip>
{
    public TripEditMessage(Trip trip) : base(trip) { }
}
```

**Redenen:**
1. **Complete objecten**: Query parameters zijn alleen strings. Een Trip heeft meerdere properties.
2. **Type safety**: Message bevat een getypt Trip object, geen strings om te parsen.
3. **Losse koppeling**: TripsViewModel kent EditTripViewModel niet direct.

**Gebruik:**
```csharp
// Versturen (na navigatie)
WeakReferenceMessenger.Default.Send(new TripEditMessage(trip));

// Ontvangen
public void Receive(TripEditMessage message)
{
    var trip = message.Value;
}
```

---

### Vraag 4: ObservableRecipient vs ObservableObject

**Vraag:** Wanneer gebruik je `ObservableRecipient` in plaats van `ObservableObject`?

**Antwoord:**
| Base Class | Gebruik |
|------------|---------|
| `ObservableObject` | ViewModels die alleen properties/commands hebben |
| `ObservableRecipient` | ViewModels die messages ontvangen |

**ObservableRecipient biedt extra:**
- `Messenger` property (voor `Messenger.Register<T>()`)
- `IsActive` property (aan/uitzetten van message ontvangst)
- Erft van ObservableObject (dus ook SetProperty, etc.)

```csharp
public class EditTripViewModel : ObservableRecipient, IRecipient<TripEditMessage>
{
    public EditTripViewModel()
    {
        // Registreer message handler
        Messenger.Register<EditTripViewModel, TripEditMessage>(
            this, (r, m) => r.Receive(m));
    }
}
```

---

### Vraag 5: DisplayAlert voor bevestiging

**Vraag:** Hoe vraag je bevestiging voordat een item wordt verwijderd?

**Antwoord:**
```csharp
private async Task DeleteTrip(Trip? trip)
{
    if (trip == null) return;

    // DisplayAlert met 2 buttons returnt bool
    var confirm = await Application.Current!.MainPage!.DisplayAlert(
        "Delete Trip",                              // Titel
        $"Are you sure you want to delete '{trip.Name}'?",  // Bericht
        "Delete",                                   // Accept button (true)
        "Cancel");                                  // Cancel button (false)

    if (!confirm) return;  // User clicked Cancel

    // Proceed with delete...
    await tripService.DeleteAsync(trip.Id);
    Trips.Remove(trip);
}
```

**Belangrijk:** De method is `async Task` zodat we kunnen awaiten op de user response.

---

### Vraag 6: lokale collectie update

**Vraag:** Waarom roepen we `Trips.Remove(trip)` aan na `DeleteAsync()`?

**Antwoord:**
```csharp
await tripService.DeleteAsync(trip.Id);  // API call
Trips.Remove(trip);                       // Lokale update
```

**Redenen:**
1. **Instant UI feedback**: Geen wachten op API refresh
2. **Betere UX**: Item verdwijnt direct, geen flicker
3. **ObservableCollection**: Triggert automatisch CollectionChanged event
4. **Efficiëntie**: Geen extra GET request nodig

Als we alleen `DeleteAsync()` doen, blijft het item zichtbaar tot de volgende refresh.

---

## Samenvatting

- **SwipeView** voor intuïtieve mobile delete/edit
- **LeftItems** = buttons links (swipe rechts)
- **RightItems** = buttons rechts (swipe links)
- **RelativeSource** voor binding naar parent ViewModel
- **ValueChangedMessage** voor complexe data overdracht
- **ObservableRecipient** voor message ontvangst
- **DisplayAlert** voor bevestiging dialoog

---

## Referenties

- **SwipeView**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/swipeview)
- **RelativeSource Binding**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/relative-bindings)
- **WeakReferenceMessenger**: [CommunityToolkit.Mvvm docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger)
- **Cursus**: Les 2 - MAUI & MVVM (Messages)
