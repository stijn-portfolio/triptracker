---
fase: 14
status: Voltooid
tags:
  - editing
  - geocoding
  - ai
  - datatrigger
  - swipeview
created: 2025-12-20
---

# Fase 14: stop editing & AI describe

## Overzicht

In deze fase hebben we **EditStopPage** uitgebreid met foto-wijziging, AI-analyse, en forward geocoding. Dit combineert meerdere eerdere concepten in één workflow.

> [!info] Cursus Referentie
> Deze fase combineert concepts uit Les 3 (DataServices, AI, Geolocation) met MVVM patterns uit Les 2.

---

## Feature overzicht

| Feature | Beschrijving |
|---------|--------------|
| Foto wijzigen | Gallery picker in EditStopPage |
| AI Describe | OpenAI Vision analyse van nieuwe foto |
| Forward Geocoding | Adres → GPS coördinaten |
| Auto-refresh | Stop herlaadt na bewerking |
| Swipe-to-edit | Consistente UX met trips |

---

## MVVM architectuur

```
┌─────────────────────────────────────────────────────────────┐
│                 EditStopPage Architectuur                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  TripDetailPage                                              │
│      │ Swipe rechts op stop                                 │
│      │ EditStopCommand → StopEditMessage                    │
│      ▼                                                       │
│  EditStopPage                                                │
│      │                                                       │
│      ├── PickPhotoCommand → _photoService.PickPhotoAsync()  │
│      │                                                       │
│      ├── AnalyzePhotoCommand                                │
│      │       └── _analyzeImageService.AnalyzePhotoAsync()   │
│      │       └── Update Title, Description                   │
│      │                                                       │
│      └── SaveCommand                                         │
│              ├── Forward Geocoding (als adres gewijzigd)    │
│              │       └── Geocoding.GetLocationsAsync()      │
│              ├── SavePhotoLocally() (als foto gewijzigd)    │
│              ├── tripStopService.PutAsync()                 │
│              ├── RefreshDataMessage                         │
│              └── NavigateBackAsync()                         │
│                                                              │
│  StopDetailViewModel                                         │
│      └── Receive(RefreshDataMessage)                       │
│              └── RefreshCurrentStop()                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Forward geocoding

**Reverse Geocoding:** GPS → Adres (gebruikt bij AddStop)
**Forward Geocoding:** Adres → GPS (gebruikt bij EditStop)

```csharp
// EditStopViewModel.cs - SaveStop()
if (!string.IsNullOrWhiteSpace(Address) && Address != originalAddress)
{
    // Combineer adres + land voor betere resultaten
    var searchAddress = !string.IsNullOrWhiteSpace(Country)
        ? $"{Address}, {Country}"
        : Address;

    // Forward geocoding
    var locations = await Geocoding.Default.GetLocationsAsync(searchAddress);
    var location = locations?.FirstOrDefault();

    if (location != null)
    {
        latitude = location.Latitude;
        longitude = location.Longitude;
    }
    else
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Location Not Found",
            "Could not find coordinates for this address.",
            "OK");
    }
}
```

**Waarom forward geocoding bij edit?**
- Gebruiker kan adres handmatig wijzigen
- GPS coördinaten moeten consistent blijven
- Kaart moet correcte locatie tonen

---

## AI describe met loading state

### ViewModel implementation

```csharp
// EditStopViewModel.cs
private bool isAnalyzing;
public bool IsAnalyzing
{
    get => isAnalyzing;
    set => SetProperty(ref isAnalyzing, value);
}

private async Task AnalyzePhoto()
{
    if (PhotoData == null) return;

    try
    {
        IsAnalyzing = true;

        var analysis = await _analyzeImageService.AnalyzePhotoAsync(PhotoData);

        if (analysis != null)
        {
            Title = analysis.Title ?? Title;
            Description = analysis.Description ?? Description;
        }
    }
    finally
    {
        IsAnalyzing = false;  // Altijd resetten, ook bij error
    }
}
```

### DataTrigger voor button tekst

```xml
<!-- EditStopPage.xaml -->
<Button Command="{Binding AnalyzePhotoCommand}"
        BackgroundColor="#E91E63"
        IsVisible="{Binding HasPhoto}">
    <Button.Triggers>
        <DataTrigger TargetType="Button"
                     Binding="{Binding IsAnalyzing}"
                     Value="True">
            <Setter Property="Text" Value="Analyzing..."/>
        </DataTrigger>
        <DataTrigger TargetType="Button"
                     Binding="{Binding IsAnalyzing}"
                     Value="False">
            <Setter Property="Text" Value="AI Describe"/>
        </DataTrigger>
    </Button.Triggers>
</Button>
```

**DataTrigger uitleg:**
- `Binding="{Binding IsAnalyzing}"` → Observeert de property
- `Value="True"` → Wanneer IsAnalyzing true is
- `Setter` → Wijzigt property op het element

---

## Full EditStopPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TripTracker.App.Views.EditStopPage"
             Title="Edit Stop">

    <ScrollView Padding="15">
        <VerticalStackLayout Spacing="15">

            <!-- FOTO SECTIE -->
            <Border Stroke="LightGray"
                    Padding="0"
                    HeightRequest="200"
                    StrokeShape="RoundRectangle 10"
                    BackgroundColor="White">
                <Grid>
                    <!-- Placeholder als geen foto -->
                    <VerticalStackLayout IsVisible="{Binding HasPhoto,
                            Converter={StaticResource InvertedBoolConverter}}"
                                         VerticalOptions="Center"
                                         HorizontalOptions="Center"
                                         Spacing="10">
                        <Label Text="Tap to change photo"
                               FontSize="16"
                               TextColor="Gray"
                               HorizontalTextAlignment="Center"/>
                    </VerticalStackLayout>

                    <!-- Foto preview -->
                    <Image Source="{Binding PhotoPreview}"
                           Aspect="AspectFill"
                           IsVisible="{Binding HasPhoto}"/>

                    <!-- Tap gesture voor hele border -->
                    <Grid.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding PickPhotoCommand}"/>
                    </Grid.GestureRecognizers>
                </Grid>
            </Border>

            <!-- Photo buttons: Change Photo + AI Describe -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="10">
                <Button Text="Change Photo"
                        Command="{Binding PickPhotoCommand}"
                        BackgroundColor="#512BD4"/>

                <!-- AI Describe button met DataTrigger -->
                <Button Grid.Column="1"
                        Command="{Binding AnalyzePhotoCommand}"
                        BackgroundColor="#E91E63"
                        IsVisible="{Binding HasPhoto}">
                    <Button.Triggers>
                        <!-- Tekst wisselt op basis van IsAnalyzing -->
                        <DataTrigger TargetType="Button"
                                     Binding="{Binding IsAnalyzing}"
                                     Value="True">
                            <Setter Property="Text" Value="Analyzing..."/>
                        </DataTrigger>
                        <DataTrigger TargetType="Button"
                                     Binding="{Binding IsAnalyzing}"
                                     Value="False">
                            <Setter Property="Text" Value="AI Describe"/>
                        </DataTrigger>
                    </Button.Triggers>
                </Button>
            </Grid>

            <!-- Title -->
            <VerticalStackLayout Spacing="5">
                <Label Text="Title" FontAttributes="Bold"/>
                <Entry Text="{Binding Title}"
                       Placeholder="Enter title"/>
            </VerticalStackLayout>

            <!-- Description met Border (niet Frame!) -->
            <VerticalStackLayout Spacing="5">
                <Label Text="Description" FontAttributes="Bold"/>
                <Border Stroke="LightGray" StrokeThickness="1" Padding="5">
                    <Editor Text="{Binding Description}"
                            Placeholder="Enter description"
                            MinimumHeightRequest="150"
                            AutoSize="TextChanges"/>
                </Border>
            </VerticalStackLayout>

            <!-- Address (wijzigen triggert forward geocoding bij save) -->
            <VerticalStackLayout Spacing="5">
                <Label Text="Address" FontAttributes="Bold"/>
                <Entry Text="{Binding Address}"
                       Placeholder="Enter address"/>
            </VerticalStackLayout>

            <!-- Country -->
            <VerticalStackLayout Spacing="5">
                <Label Text="Country" FontAttributes="Bold"/>
                <Entry Text="{Binding Country}"
                       Placeholder="Enter country"/>
            </VerticalStackLayout>

            <!-- Date and Time (apart ipv DateTime) -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="15">
                <VerticalStackLayout Spacing="5">
                    <Label Text="Date" FontAttributes="Bold"/>
                    <DatePicker Date="{Binding VisitDate}"/>
                </VerticalStackLayout>
                <VerticalStackLayout Grid.Column="1" Spacing="5">
                    <Label Text="Time" FontAttributes="Bold"/>
                    <TimePicker Time="{Binding VisitTime}"/>
                </VerticalStackLayout>
            </Grid>

            <!-- Action buttons -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="10" Margin="0,20,0,0">
                <Button Text="Cancel"
                        Command="{Binding CancelCommand}"
                        BackgroundColor="Gray"
                        TextColor="White"/>
                <Button Text="Save"
                        Command="{Binding SaveCommand}"
                        Grid.Column="1"
                        BackgroundColor="#512BD4"
                        TextColor="White"/>
            </Grid>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### XAML highlights

| Element | Concept | Uitleg |
|---------|---------|--------|
| `Converter={StaticResource InvertedBoolConverter}` | Value Converter | Keert HasPhoto om voor placeholder |
| `<DataTrigger>` | Visuele triggers | Wijzigt button tekst op IsAnalyzing |
| `<Border>` i.p.v. `<Frame>` | Moderne MAUI | Lichter, ondersteunt transparantie |
| `DatePicker` + `TimePicker` | Separate controls | Datum en tijd apart (niet DateTime) |
| `AutoSize="TextChanges"` | Editor sizing | Groeit mee met inhoud |

> [!tip] DatePicker + TimePicker Pattern
> We gebruiken aparte controls omdat:
> - MAUI heeft geen DateTimePicker
> - Beter voor mobile UX (grote touch targets)
> - Makkelijker te stylen

---

## Auto-refresh met meerdere message types

StopDetailViewModel luistert naar meerdere message types:

```csharp
public class StopDetailViewModel : ObservableRecipient,
    IRecipient<StopSelectedMessage>,     // Navigatie
    IRecipient<RefreshDataMessage>      // Refresh
{
    public StopDetailViewModel(INavigationService nav)
    {
        // Registreer voor beide message types
        Messenger.Register<StopDetailViewModel, StopSelectedMessage>(
            this, (r, m) => r.Receive(m));
        Messenger.Register<StopDetailViewModel, RefreshDataMessage>(
            this, (r, m) => r.Receive(m));
    }

    public void Receive(StopSelectedMessage message)
    {
        CurrentStop = message.Value;
    }

    public void Receive(RefreshDataMessage message)
    {
        _ = RefreshCurrentStop();  // Fire-and-forget
    }

    private async Task RefreshCurrentStop()
    {
        if (CurrentStop == null) return;

        var stopService = new TripStopDataService();
        var refreshedStop = await stopService.GetAsync(CurrentStop.Id);

        if (refreshedStop != null)
        {
            CurrentStop = refreshedStop;
        }
    }
}
```

**Waarom `_ = RefreshCurrentStop()`?**
- `Receive()` is void, niet async
- We willen niet wachten op de refresh
- `_` negeert de returned Task (fire-and-forget pattern)

---

## Foto lokaal opslaan pattern

```csharp
private async Task<string> SavePhotoLocally()
{
    if (PhotoData == null)
        return string.Empty;

    // 1. Maak Photos folder aan
    var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
    Directory.CreateDirectory(photosDir);  // Idempotent - geen error als bestaat

    // 2. Genereer unieke bestandsnaam
    var fileName = $"stop_{Guid.NewGuid()}.jpg";
    var filePath = Path.Combine(photosDir, fileName);

    // 3. Schrijf bytes naar bestand
    await File.WriteAllBytesAsync(filePath, PhotoData);

    return filePath;
}
```

**Waarom lokaal opslaan?**
- API kan geen grote binaire data efficiënt verwerken
- Base64 encoding verdubbelt de grootte
- Lokaal pad werkt offline
- Snellere UI response

---

## Swipe acties overzicht

| Page | Swipe Rechts (←) | Swipe Links (→) |
|------|------------------|-----------------|
| TripsPage | Edit Trip | Delete Trip |
| TripDetailPage | Edit Stop | Delete Stop |

```xml
<!-- TripDetailPage.xaml -->
<SwipeView>
    <SwipeView.LeftItems>
        <SwipeItems>
            <SwipeItem Text="Edit"
                       BackgroundColor="#512BD4"
                       Command="{Binding Source={RelativeSource
                           AncestorType={x:Type viewmodels:TripDetailViewModel}},
                           Path=EditStopCommand}"
                       CommandParameter="{Binding .}"/>
        </SwipeItems>
    </SwipeView.LeftItems>

    <SwipeView.RightItems>
        <SwipeItems>
            <SwipeItem Text="Delete"
                       BackgroundColor="Red"
                       Command="{Binding Source={RelativeSource
                           AncestorType={x:Type viewmodels:TripDetailViewModel}},
                           Path=DeleteStopCommand}"
                       CommandParameter="{Binding .}"/>
        </SwipeItems>
    </SwipeView.RightItems>

    <Frame><!-- Stop content --></Frame>
</SwipeView>
```

---

## Bestanden

### Gewijzigd

| Bestand | Wijziging |
|---------|-----------|
| `ViewModels/EditStopViewModel.cs` | PickPhotoCommand, AnalyzePhotoCommand, forward geocoding |
| `ViewModels/IEditStopViewModel.cs` | HasPhoto, IsAnalyzing, nieuwe commands |
| `Views/EditStopPage.xaml` | Foto sectie, AI Describe button |
| `ViewModels/TripDetailViewModel.cs` | EditStopCommand toegevoegd |
| `ViewModels/ITripDetailViewModel.cs` | Interface update |
| `Views/TripDetailPage.xaml` | SwipeView.LeftItems voor Edit |
| `ViewModels/StopDetailViewModel.cs` | RefreshDataMessage handler |

---

## Cursus compliance

| Vereiste | Status |
|----------|--------|
| MVVM Architecture | ✅ Code-behind leeg |
| OpenAI Integration | ✅ AnalyzePhotoAsync() |
| Geocoding | ✅ Forward geocoding |
| Messaging | ✅ RefreshDataMessage |
| SwipeView | ✅ Consistente UX |

---

## Examenvragen

### Vraag 1: forward vs reverse geocoding

**Vraag:** Wat is het verschil tussen forward en reverse geocoding?

**Antwoord:**
| Type | Input | Output | Gebruik |
|------|-------|--------|---------|
| **Reverse** | GPS coördinaten | Adres | AddStopPage (na foto/locatie) |
| **Forward** | Adres tekst | GPS coördinaten | EditStopPage (na adres wijziging) |

```csharp
// Reverse: GPS → Adres
var placemarks = await Geocoding.GetPlacemarksAsync(lat, lon);

// Forward: Adres → GPS
var locations = await Geocoding.GetLocationsAsync("Amsterdam, NL");
```

---

### Vraag 2: DataTrigger voor loading state

**Vraag:** Hoe toon je "Analyzing..." tijdens een async operatie met DataTrigger?

**Antwoord:**
```xml
<Button Command="{Binding AnalyzePhotoCommand}">
    <Button.Triggers>
        <DataTrigger TargetType="Button"
                     Binding="{Binding IsAnalyzing}"
                     Value="True">
            <Setter Property="Text" Value="Analyzing..."/>
        </DataTrigger>
        <DataTrigger TargetType="Button"
                     Binding="{Binding IsAnalyzing}"
                     Value="False">
            <Setter Property="Text" Value="AI Describe"/>
        </DataTrigger>
    </Button.Triggers>
</Button>
```

**Componenten:**
1. `Binding` → Property om te observeren
2. `Value` → Wanneer de trigger activeert
3. `Setter` → Wat te wijzigen

---

### Vraag 3: fire-and-Forget pattern

**Vraag:** Wat betekent `_ = RefreshCurrentStop()` en waarom wordt dit gebruikt?

**Antwoord:**
```csharp
public void Receive(RefreshDataMessage message)
{
    _ = RefreshCurrentStop();  // Fire-and-forget
}
```

**Uitleg:**
- `Receive()` is `void` (niet `async`)
- `RefreshCurrentStop()` returnt een `Task`
- `_ =` negeert de Task expliciet
- De operatie start maar we wachten niet

**Waarom niet async void?**
- `async void` is alleen voor event handlers
- `IRecipient<T>.Receive()` moet `void` zijn (interface vereiste)
- Fire-and-forget is hier acceptabel

---

### Vraag 4: meerdere iRecipient interfaces

**Vraag:** Hoe laat je een ViewModel luisteren naar meerdere message types?

**Antwoord:**
```csharp
public class StopDetailViewModel : ObservableRecipient,
    IRecipient<StopSelectedMessage>,   // Interface 1
    IRecipient<RefreshDataMessage>    // Interface 2
{
    public StopDetailViewModel()
    {
        // Registreer voor elk type apart
        Messenger.Register<StopDetailViewModel, StopSelectedMessage>(
            this, (r, m) => r.Receive(m));
        Messenger.Register<StopDetailViewModel, RefreshDataMessage>(
            this, (r, m) => r.Receive(m));
    }

    // Implementeer beide Receive methods
    public void Receive(StopSelectedMessage message) { ... }
    public void Receive(RefreshDataMessage message) { ... }
}
```

**C# maakt dit mogelijk via:**
- Meerdere interface implementaties (comma-separated)
- Method overloading (zelfde naam, verschillende parameter types)

---

### Vraag 5: try-finally voor loading state

**Vraag:** Waarom staat `IsAnalyzing = false` in een `finally` block?

**Antwoord:**
```csharp
private async Task AnalyzePhoto()
{
    try
    {
        IsAnalyzing = true;
        // ... AI call die kan falen
    }
    finally
    {
        IsAnalyzing = false;  // ALTIJD uitgevoerd
    }
}
```

**Waarom `finally`?**
1. Als AI call slaagt → `finally` reset loading state
2. Als AI call faalt (exception) → `finally` reset loading state toch
3. Zonder `finally` → button blijft op "Analyzing..." bij error

**Regel:** Gebruik `finally` voor cleanup die ALTIJD moet gebeuren.

---

### Vraag 6: foto path vs bytes

**Vraag:** Waarom slaan we de foto lokaal op in plaats van base64 naar de API te sturen?

**Antwoord:**
| Aanpak | Voordelen | Nadelen |
|--------|-----------|---------|
| **Lokaal pad** | Sneller, kleiner, offline | Alleen lokaal beschikbaar |
| **Base64 naar API** | Centraal opgeslagen | Trager, 33% groter, meer API load |

```csharp
// Lokaal opslaan - Wat we doen
var filePath = await SavePhotoLocally();
updatedStop.ImageUrl = filePath;  // Lokaal pad in database

// Base64 - Niet efficiënt
var base64 = Convert.ToBase64String(PhotoData);  // +33% grootte!
```

**TripTracker keuze:** Lokaal opslaan is voldoende voor een mobile app.

---

## Samenvatting

- **Forward geocoding**: Adres → GPS (bij edit)
- **Reverse geocoding**: GPS → Adres (bij add)
- **DataTrigger**: Dynamic button text based on property
- **Fire-and-forget**: `_ = AsyncMethod()` voor void context
- **Meerdere IRecipient**: Implementeer meerdere interfaces
- **try-finally**: Garandeer cleanup van loading state
- **Lokaal foto opslaan**: Efficiënter dan base64 naar API

---

## Referenties

- **Geocoding**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/geocoding)
- **DataTrigger**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/triggers)
- **Cursus**: Les 3 - Geolocation & AI Integration
