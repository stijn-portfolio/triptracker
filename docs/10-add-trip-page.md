---
fase: 10
status: Voltooid
tags:
  - addtrippage
  - mvvm
  - navigation
  - photo
created: 2025-12-20
---

# Fase 10: AddTripPage

## Overzicht

In deze fase hebben we een aparte pagina gemaakt voor het toevoegen van trips. Dit vervangt de simpele prompt dialoog met een volledig formulier.

---

## Waarom een aparte pagina?

| Prompt Dialoog | AddTripPage |
|----------------|-------------|
| Alleen titel | Naam, beschrijving, datums, foto |
| Geen validatie UI | Save button disabled tot naam ingevuld |
| Geen foto support | Foto picker met preview |

---

## MVVM architectuur

```
┌─────────────────────────────────────────────────────────────┐
│                    IAddTripViewModel                         │
│                      (Interface)                             │
├─────────────────────────────────────────────────────────────┤
│  Properties: Name, Description, StartDate, EndDate          │
│  Properties: PhotoPreview, HasPhoto                         │
│  Commands: SaveCommand, CancelCommand, PickPhotoCommand     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    AddTripViewModel                          │
│                    (Implementatie)                           │
├─────────────────────────────────────────────────────────────┤
│  • INavigationService (DI)                                  │
│  • IPhotoService (DI)                                       │
│  • TripDataService.PostAsync() (generieke ApiService)       │
│  • RefreshDataMessage na save                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    AddTripPage.xaml                          │
│                    (View - code-behind leeg)                 │
├─────────────────────────────────────────────────────────────┤
│  • Foto frame met preview                                   │
│  • Select Photo button                                      │
│  • Name Entry (verplicht)                                   │
│  • Description Editor                                       │
│  • Start/End DatePickers                                    │
│  • Save/Cancel buttons                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## Generieke ApiService hergebruik

De `TripDataService` erft van de generieke `ApiService<Trip>`:

```csharp
// TripDataService.cs
public class TripDataService : ApiService<Trip>
{
    protected override string EndPoint => "trips";
}

// In AddTripViewModel.cs
var tripService = new TripDataService();
await tripService.PostAsync(newTrip);  // Roept ApiService<Trip>.PostAsync() aan
```

Dit volgt exact het cursuspatroon uit Les 3 (SafariSnap DataServices).

---

## Save button validatie

De Save button is disabled tot de gebruiker een naam invult:

```csharp
// CanSave wordt gecheckt door AsyncRelayCommand
private bool CanSave()
{
    return !string.IsNullOrWhiteSpace(Name);
}

// Name setter roept NotifyCanExecuteChanged aan
public string Name
{
    get => name;
    set
    {
        if (SetProperty(ref name, value))
        {
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
        }
    }
}
```

---

## Navigatie flow

```
TripsPage
    │
    │ Tap + button
    │ AddTripCommand
    ▼
NavigationService.NavigateToAddTripPageAsync()
    │
    │ GetRequiredService<AddTripPage>()
    ▼
AddTripPage
    │
    │ User fills form
    │ SaveCommand
    ▼
TripDataService.PostAsync(newTrip)
    │
    │ API: POST /api/trips
    ▼
RefreshDataMessage
    │
    │ TripsViewModel.Receive()
    ▼
NavigateBackAsync()
    │
    ▼
TripsPage (refreshed)
```

---

## Full XAML voorbeelden

### AddTripPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TripTracker.App.Views.AddTripPage"
             Title="New Trip">

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
                        <Label Text="Take or pick a photo"
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

            <!-- Camera/Gallery buttons -->
            <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
                <Button Text="Camera"
                        Command="{Binding CapturePhotoCommand}"
                        BackgroundColor="#512BD4"/>
                <Button Text="Gallery"
                        Command="{Binding PickPhotoCommand}"
                        Grid.Column="1"
                        BackgroundColor="#512BD4"/>
            </Grid>

            <!-- Trip naam (verplicht) -->
            <VerticalStackLayout>
                <Label Text="Name *" FontAttributes="Bold"/>
                <Entry Text="{Binding Name}"
                       Placeholder="Enter trip name"
                       FontSize="18"/>
            </VerticalStackLayout>

            <!-- Beschrijving -->
            <VerticalStackLayout>
                <Label Text="Description" FontAttributes="Bold"/>
                <Editor Text="{Binding Description}"
                        Placeholder="Enter trip description"
                        HeightRequest="100"
                        AutoSize="TextChanges"/>
            </VerticalStackLayout>

            <!-- Datums -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="15">
                <!-- Start datum -->
                <VerticalStackLayout>
                    <Label Text="Start Date" FontAttributes="Bold"/>
                    <DatePicker Date="{Binding StartDate}"
                                Format="dd MMM yyyy"/>
                </VerticalStackLayout>

                <!-- Eind datum -->
                <VerticalStackLayout Grid.Column="1">
                    <Label Text="End Date" FontAttributes="Bold"/>
                    <DatePicker Date="{Binding EndDate}"
                                Format="dd MMM yyyy"/>
                </VerticalStackLayout>
            </Grid>

            <!-- Save/Cancel knoppen -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="10" Margin="0,20,0,0">
                <Button Text="Cancel"
                        Command="{Binding CancelCommand}"
                        BackgroundColor="LightGray"
                        TextColor="Black"/>
                <Button Text="Save Trip"
                        Command="{Binding SaveCommand}"
                        Grid.Column="1"
                        BackgroundColor="#512BD4"/>
            </Grid>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

---

### EditTripPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TripTracker.App.Views.EditTripPage"
             Title="Edit Trip">

    <ScrollView Padding="15">
        <VerticalStackLayout Spacing="15">

            <!-- FOTO SECTIE (zelfde als AddTripPage) -->
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

            <!-- VERSCHIL: Alleen "Change Photo" button (geen Camera) -->
            <Button Text="Change Photo"
                    Command="{Binding PickPhotoCommand}"
                    BackgroundColor="#512BD4"/>

            <!-- Trip naam (verplicht) - zelfde als AddTripPage -->
            <VerticalStackLayout>
                <Label Text="Name *" FontAttributes="Bold"/>
                <Entry Text="{Binding Name}"
                       Placeholder="Enter trip name"
                       FontSize="18"/>
            </VerticalStackLayout>

            <!-- Beschrijving - zelfde als AddTripPage -->
            <VerticalStackLayout>
                <Label Text="Description" FontAttributes="Bold"/>
                <Editor Text="{Binding Description}"
                        Placeholder="Enter trip description"
                        HeightRequest="100"
                        AutoSize="TextChanges"/>
            </VerticalStackLayout>

            <!-- Datums - zelfde als AddTripPage -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="15">
                <VerticalStackLayout>
                    <Label Text="Start Date" FontAttributes="Bold"/>
                    <DatePicker Date="{Binding StartDate}"
                                Format="dd MMM yyyy"/>
                </VerticalStackLayout>

                <VerticalStackLayout Grid.Column="1">
                    <Label Text="End Date" FontAttributes="Bold"/>
                    <DatePicker Date="{Binding EndDate}"
                                Format="dd MMM yyyy"/>
                </VerticalStackLayout>
            </Grid>

            <!-- VERSCHIL: "Save Changes" ipv "Save Trip" -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="10" Margin="0,20,0,0">
                <Button Text="Cancel"
                        Command="{Binding CancelCommand}"
                        BackgroundColor="LightGray"
                        TextColor="Black"/>
                <Button Text="Save Changes"
                        Command="{Binding SaveCommand}"
                        Grid.Column="1"
                        BackgroundColor="#512BD4"/>
            </Grid>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

---

### Verschil add vs edit

| Aspect | AddTripPage | EditTripPage |
|--------|-------------|--------------|
| **Title** | "New Trip" | "Edit Trip" |
| **Placeholder tekst** | "Take or pick a photo" | "Tap to change photo" |
| **Photo buttons** | Camera + Gallery (2 knoppen) | "Change Photo" (1 knop) |
| **Save button** | "Save Trip" | "Save Changes" |
| **API Call** | `PostAsync()` (CREATE) | `PutAsync()` (UPDATE) |
| **Data bron** | Lege form | Ingevuld vanuit TripEditMessage |

> [!tip] Hergebruik Pattern
> EditTripPage is bijna identiek aan AddTripPage. Bij grotere apps zou je kunnen overwegen om één pagina te maken met een "mode" parameter. Voor TripTracker houden we ze gescheiden voor duidelijkheid.

---

## Bestanden

### Nieuw gemaakt

| Bestand | Doel |
|---------|------|
| `ViewModels/IAddTripViewModel.cs` | Interface voor DI |
| `ViewModels/AddTripViewModel.cs` | ViewModel met logica |
| `Views/AddTripPage.xaml` | UI layout |
| `Views/AddTripPage.xaml.cs` | Minimale code-behind |

### Gewijzigd

| Bestand | Wijziging |
|---------|-----------|
| `MauiProgram.cs` | DI registratie AddTripPage |
| `Services/INavigationService.cs` | NavigateToAddTripPageAsync() |
| `Services/NavigationService.cs` | Implementatie navigatie |
| `ViewModels/TripsViewModel.cs` | Navigatie ipv prompt |

---

## Cursus compliance

| Vereiste | Status |
|----------|--------|
| MVVM Architecture | ✅ Code-behind leeg |
| Generieke ApiService | ✅ TripDataService.PostAsync() |
| DI Pattern | ✅ AddTransient in MauiProgram |
| NavigationService | ✅ Via IServiceProvider |
| Messages | ✅ RefreshDataMessage |

---

## Examenvragen

### Vraag 1: CanSave pattern

**Vraag:** Implementeer een Save button die alleen enabled is als de naam niet leeg is.

**Antwoord:**
```csharp
// 1. Command met CanExecute delegate
SaveCommand = new AsyncRelayCommand(SaveTrip, CanSave);

// 2. CanSave method
private bool CanSave()
{
    return !string.IsNullOrWhiteSpace(Name);
}

// 3. NotifyCanExecuteChanged bij property change
public string Name
{
    get => name;
    set
    {
        if (SetProperty(ref name, value))
        {
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
        }
    }
}
```

---

### Vraag 2: generieke ApiService

**Vraag:** Hoe maak je een service voor Trips die de generieke ApiService hergebruikt?

**Antwoord:**
```csharp
// TripDataService.cs
public class TripDataService : ApiService<Trip>
{
    protected override string EndPoint => "trips";
}

// Gebruik:
var tripService = new TripDataService();
await tripService.PostAsync(newTrip);  // POST /api/trips
await tripService.GetAllAsync();       // GET /api/trips
```

Dit volgt het **DRY principe** - alle CRUD logica zit in de base class.

---

### Vraag 3: RefreshDataMessage

**Vraag:** Hoe zorg je dat de trips lijst herlaadt na het toevoegen van een nieuwe trip?

**Antwoord:**
```csharp
// In AddTripViewModel.SaveTrip():
await tripService.PostAsync(newTrip);

// Stuur refresh message
WeakReferenceMessenger.Default.Send(new RefreshDataMessage(true));

// Navigeer terug
await _navigationService.NavigateBackAsync();

// In TripsViewModel (receiver):
public void Receive(RefreshDataMessage message)
{
    _ = LoadTripsAsync();  // Herlaad trips
}
```

---

### Vraag 4: photo lokaal opslaan

**Vraag:** Hoe sla je een foto lokaal op in de app data folder?

**Antwoord:**
```csharp
private async Task<string> SavePhotoLocally()
{
    // 1. Maak Photos folder
    var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
    Directory.CreateDirectory(photosDir);

    // 2. Unieke bestandsnaam
    var fileName = $"trip_{Guid.NewGuid()}.jpg";
    var filePath = Path.Combine(photosDir, fileName);

    // 3. Schrijf bytes
    await File.WriteAllBytesAsync(filePath, PhotoData);

    return filePath;
}
```

**Waarom lokaal?** API kan geen grote binaire bestanden (base64) efficient verwerken. Lokaal opslaan + pad in database is beter.

---

### Vraag 5: DI registratie

**Vraag:** Hoe registreer je AddTripPage in dependency injection?

**Antwoord:**
```csharp
// MauiProgram.cs
builder.Services.AddTransient<AddTripPage>();
builder.Services.AddTransient<IAddTripViewModel, AddTripViewModel>();
```

**Transient** omdat elke keer dat je naar de pagina navigeert, je een schone instantie wilt.

---

## Samenvatting

- AddTripPage met foto, naam, beschrijving, datums
- Save button validatie (naam verplicht)
- Foto lokaal opgeslagen (zoals AddStopPage)
- Hergebruik generieke ApiService<Trip>
- 100% MVVM compliant

---

## Referenties

- **CommunityToolkit.Mvvm AsyncRelayCommand**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
- **FileSystem.AppDataDirectory**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-system-helpers)
- **Cursus**: Les 3 - DataServices (SafariSnap)
