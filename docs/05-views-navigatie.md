---
fase: 5
title: Views en Navigatie
status: completed
tags: [xaml, views, navigation, data-binding, mvvm]
created: 2025-12-20
---

# Fase 5: Views en Navigatie

> [!info] Overzicht
> Deze fase implementeert alle XAML views met data binding en navigatie. Het is de presentatielaag van de app die de MVVM pattern voltooit. **Cruciale examenvereiste:** Code-behind bestanden blijven volledig LEEG!

## Inhoudsopgave

1. [Shell Navigatie](#shell-navigatie)
2. [Views Overzicht](#views-overzicht)
3. [Complete Flow: TripsPage → TripDetailPage](#complete-flow-tripspage--tripdetailpage)
4. [TripsPage - Lijst van Trips](#tripspage---lijst-van-trips)
5. [TripDetailPage - Trip met Stops](#tripdetailpage---trip-met-stops)
6. [AddStopPage - Stop Toevoegen](#addstoppage---stop-toevoegen)
7. [StopDetailPage - Stop Details](#stopdetailpage---stop-details)
8. [Value Converters](#value-converters)
9. [Code-Behind Pattern](#code-behind-pattern)
10. [Dependency Injection Setup](#dependency-injection-setup)
11. [Examenvragen](#examenvragen)

---

## Shell Navigatie

> [!tip] Examenvraag
> **Vraag:** Wat is het verschil tussen TabBar en FlyoutItem in Shell navigatie?
>
> **Antwoord:** TabBar toont tabs onderaan/bovenaan het scherm (voor hoofdnavigatie zoals in Instagram). FlyoutItem gebruikt een hamburger menu dat van links naar binnen schuift. Voor apps met 2-5 hoofdsecties gebruik je TabBar, voor apps met veel secties of hiërarchie gebruik je FlyoutItem.

### AppShell.xaml

**Doel:** Definieer de hoofdnavigatie structuur van de app

```xaml
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

**Belangrijke eigenschappen:**

| Eigenschap | Waarde | Uitleg |
|------------|--------|--------|
| `Shell.FlyoutBehavior` | `Disabled` | Geen hamburger menu, alleen tabs |
| `ContentTemplate` | `{DataTemplate views:TripsPage}` | Lazy loading - page wordt pas gemaakt bij eerste gebruik |

### AppShell.xaml.cs

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

> [!info] Route Registratie
> Voor TripTracker gebruiken we GEEN Shell routes (zoals `Routing.RegisterRoute("details", typeof(DetailsPage))`). In plaats daarvan gebruiken we de `NavigationService` met `PushAsync` voor detail pages. Dit is eenvoudiger voor kleine apps met lineaire navigatie.

---

## Views Overzicht

**Projectstructuur (8 Views):**

```
Views/
├── TripsPage.xaml          # Hoofdpagina - lijst van alle trips
├── TripsPage.xaml.cs       # LEEG (alleen DI)
├── TripDetailPage.xaml     # Trip details + stops lijst
├── TripDetailPage.xaml.cs  # LEEG
├── AddTripPage.xaml        # Nieuwe trip toevoegen
├── AddTripPage.xaml.cs     # LEEG
├── EditTripPage.xaml       # Trip bewerken
├── EditTripPage.xaml.cs    # LEEG
├── AddStopPage.xaml        # Nieuwe stop toevoegen (Smart Capture)
├── AddStopPage.xaml.cs     # LEEG
├── EditStopPage.xaml       # Stop bewerken
├── EditStopPage.xaml.cs    # LEEG
├── StopDetailPage.xaml     # Stop details bekijken
├── StopDetailPage.xaml.cs  # LEEG
├── MapPage.xaml            # Kaart met stops (Mapsui)
└── MapPage.xaml.cs         # LEEG
```

**Navigatieflow (volledig):**

```
                              TripsPage (tab)
                                    │
              ┌─────────────────────┼─────────────────────┐
              │                     │                     │
         tap trip              tap "+"             tap "🌍"
              │                     │                     │
              ▼                     ▼                     ▼
       TripDetailPage         AddTripPage            MapPage
              │                     │              (alle trips)
    ┌─────────┼─────────┐     save/cancel
    │         │         │           │
 tap stop  tap "+"   swipe         ▼
    │         │      edit      TripsPage
    ▼         ▼         │
StopDetailPage  AddStopPage   EditTripPage
    │              │              │
 swipe edit   save/cancel    save/cancel
    │              │              │
    ▼              ▼              ▼
EditStopPage  TripDetailPage  TripsPage
```

**Overzicht per View:**

| View | Doel | ViewModel | Lifetime |
|------|------|-----------|----------|
| TripsPage | Hoofdlijst trips + jaar filter | TripsViewModel | Singleton |
| TripDetailPage | Trip info + stops lijst | TripDetailViewModel | Transient |
| AddTripPage | Nieuwe trip aanmaken | AddTripViewModel | Transient |
| EditTripPage | Trip bewerken | EditTripViewModel | Transient |
| AddStopPage | Nieuwe stop (Smart Capture) | AddStopViewModel | Transient |
| EditStopPage | Stop bewerken | EditStopViewModel | Transient |
| StopDetailPage | Stop details bekijken | StopDetailViewModel | Transient |
| MapPage | Kaart met Mapsui | MapViewModel | Transient |

---

## Complete Flow: TripsPage → TripDetailPage

> [!tip] Examenvraag
> **Vraag:** Beschrijf de volledige flow wanneer een gebruiker op een Trip tikt in de lijst.
>
> **Antwoord:** Zie onderstaand diagram - van View naar ViewModel naar Service naar API en terug.

### Stap-voor-stap met Code Locaties

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 1: User tikt op Trip in TripsPage                                       ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 Views/TripsPage.xaml:91-94                                                ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  <Border.GestureRecognizers>                                                  ║
║      <TapGestureRecognizer                                                    ║
║          Command="{Binding Source={RelativeSource                             ║
║              AncestorType={x:Type viewmodels:TripsViewModel}},                ║
║              Path=ViewTripCommand}"        ← Command in ViewModel             ║
║          CommandParameter="{Binding .}"/>  ← Het Trip object                  ║
║  </Border.GestureRecognizers>                                                 ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
                                      │
                                      ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 2: TripsViewModel ontvangt command                                      ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 ViewModels/TripsViewModel.cs:205-213                                      ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  private async Task GoToTripDetail(Trip? trip)                                ║
║  {                                                                            ║
║      if (trip != null)                                                        ║
║      {                                                                        ║
║          SelectedTrip = trip;                                                 ║
║          await _navigationService.NavigateToTripDetailPageAsync();  ← STAP 3  ║
║          WeakReferenceMessenger.Default.Send(                                 ║
║              new TripSelectedMessage(trip));                        ← STAP 5  ║
║          SelectedTrip = null;                                                 ║
║      }                                                                        ║
║  }                                                                            ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
                                      │
                        ┌─────────────┴─────────────┐
                        ▼                           ▼
╔════════════════════════════════╗    ╔════════════════════════════════════════╗
║  STAP 3: NavigationService     ║    ║  STAP 5: Message versturen              ║
╠════════════════════════════════╣    ╚════════════════════════════════════════╝
║  📁 Services/NavigationService ║                      │
║     .cs:19-22                  ║                      │
╠════════════════════════════════╣                      │
║                                ║                      │
║  public async Task             ║                      │
║  NavigateToTripDetailPageAsync ║                      │
║  {                             ║                      │
║    await _navigation.PushAsync(║                      │
║      _serviceProvider          ║                      │
║        .GetRequiredService     ║                      │
║        <TripDetailPage>());    ║                      │
║  }          │                  ║                      │
║             │                  ║                      │
╚═════════════│══════════════════╝                      │
              ▼                                         │
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 4: DI Container maakt TripDetailPage + ViewModel                        ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 MauiProgram.cs:62-63 (registratie)                                        ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  builder.Services.AddTransient<TripDetailPage>();                             ║
║  builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();  ║
║                                                                               ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 Views/TripDetailPage.xaml.cs:8-12 (constructor injection)                 ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  public TripDetailPage(ITripDetailViewModel viewModel)  ← DI injecteert VM    ║
║  {                                                                            ║
║      InitializeComponent();                                                   ║
║      BindingContext = viewModel;  ← View gebonden aan ViewModel               ║
║  }                                                                            ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
              │                                         │
              │ Page is nu zichtbaar                    │ Message komt aan
              ▼                                         ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 6: TripDetailViewModel ontvangt message                                 ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 ViewModels/TripDetailViewModel.cs:58-70                                   ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  // In constructor (regel 59):                                                ║
║  Messenger.Register<TripDetailViewModel, TripSelectedMessage>(                ║
║      this, (r, m) => r.Receive(m));                                           ║
║                                                                               ║
║  // Receive methode (regel 67-71):                                            ║
║  public void Receive(TripSelectedMessage message)                             ║
║  {                                                                            ║
║      CurrentTrip = message.Value;     ← Trip opslaan                          ║
║      _ = LoadTripStopsAsync();        ← Stops laden                           ║
║  }                                                                            ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
                                      │
                                      ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 7: LoadTripStopsAsync haalt data op                                     ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 ViewModels/TripDetailViewModel.cs:79-93                                   ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  private async Task LoadTripStopsAsync()                                      ║
║  {                                                                            ║
║      IsLoading = true;                          ← UI toont spinner            ║
║      var tripService = new TripDataService();   ← Cursus patroon              ║
║      var stops = await tripService.GetTripStopsAsync(CurrentTrip.Id);         ║
║                                                            │                  ║
║      MainThread.BeginInvokeOnMainThread(() =>              │                  ║
║      {                                                     │                  ║
║          TripStops = new ObservableCollection              │                  ║
║              <TripStop>(stops);                            │                  ║
║      });                                                   │                  ║
║      IsLoading = false;                                    │                  ║
║  }                                                         │                  ║
║                                                            │                  ║
╚════════════════════════════════════════════════════════════│══════════════════╝
                                                             │
                                                             ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 8: TripDataService → HTTP Request                                       ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 Models/TripDataService.cs:12-21                                           ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  public async Task<List<TripStop>> GetTripStopsAsync(int tripId)              ║
║  {                                                                            ║
║      var response = await client.GetAsync(                                    ║
║          $"{BASE_URL}/trips/{tripId}/tripstops");                             ║
║                     │                                                         ║
║                     └──► HTTP GET https://xxx.ngrok.io/api/trips/5/tripstops  ║
║                                                            │                  ║
║      var jsonData = await response.Content.ReadAsStringAsync();               ║
║      return JsonConvert.DeserializeObject<List<TripStop>>(jsonData);          ║
║  }                                                         │                  ║
║                                                            │                  ║
╚════════════════════════════════════════════════════════════│══════════════════╝
                                                             │
                                                             ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 9: API Controller verwerkt request                                      ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 TripTracker.API/Controllers/TripsController.cs:48-57                      ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  [HttpGet("{tripId}/tripstops")]                                              ║
║  public async Task<ActionResult<IEnumerable<TripStopDto>>> GetTripStops(...)  ║
║  {                                                                            ║
║      var tripStopsFromRepo = await _tripStopRepository                        ║
║          .GetTripStopsAsync(tripId);        ← Database query                  ║
║                                                                               ║
║      return Ok(_mapper.Map<IEnumerable<TripStopDto>>(tripStopsFromRepo));     ║
║  }                      │                                                     ║
║                         └── AutoMapper: Entity → DTO (geen circular refs)     ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
                                      │
                                      │ JSON response terug
                                      ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║  STAP 10: UI Update via Data Binding                                          ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  📁 Views/TripDetailPage.xaml:50-106                                          ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  <!-- Trip header bindt aan CurrentTrip -->                                   ║
║  <Label Text="{Binding CurrentTrip.Name}"/>                                   ║
║  <Image Source="{Binding CurrentTrip.ImageUrl}"/>                             ║
║                                                                               ║
║  <!-- Stops lijst bindt aan TripStops -->                                     ║
║  <CollectionView ItemsSource="{Binding TripStops}">  ← ObservableCollection   ║
║      <DataTemplate>                                                           ║
║          <Image Source="{Binding PhotoUrl}"/>        ← Per TripStop           ║
║          <Label Text="{Binding Title}"/>                                      ║
║          <Label Text="{Binding Address}"/>                                    ║
║          <Label Text="{Binding DateTime}"/>                                   ║
║      </DataTemplate>                                                          ║
║  </CollectionView>                                                            ║
║                                                                               ║
║  <!-- Loading spinner bindt aan IsLoading -->                                 ║
║  <ActivityIndicator IsVisible="{Binding IsLoading}"/>                         ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

### Samenvatting Tabel

| Stap | Bestand | Regel | Wat gebeurt |
|------|---------|-------|-------------|
| 1 | `TripsPage.xaml` | 93 | User tikt → `ViewTripCommand` |
| 2 | `TripsViewModel.cs` | 205 | `GoToTripDetail(trip)` |
| 3 | `NavigationService.cs` | 21 | `PushAsync(TripDetailPage)` |
| 4 | `MauiProgram.cs` | 62-63 | DI maakt Page + ViewModel |
| 4b | `TripDetailPage.xaml.cs` | 11 | `BindingContext = viewModel` |
| 5 | `TripsViewModel.cs` | 211 | `Send(TripSelectedMessage)` |
| 6 | `TripDetailViewModel.cs` | 67-70 | `Receive()` → `LoadTripStopsAsync()` |
| 7 | `TripDetailViewModel.cs` | 86-87 | `new TripDataService()` |
| 8 | `TripDataService.cs` | 14 | HTTP GET request |
| 9 | `TripsController.cs` | 56-57 | DB → DTO → JSON |
| 10 | `TripDetailPage.xaml` | 51 | `{Binding TripStops}` → UI |

### Visueel Overzicht

```
┌────────────────┐     ┌─────────────────┐     ┌──────────────────┐
│   TripsPage    │     │  TripsViewModel │     │NavigationService │
│    (XAML)      │     │                 │     │                  │
│                │     │                 │     │                  │
│  [Tap Trip] ───┼────►│ GoToTripDetail()│────►│ PushAsync(       │
│                │     │                 │     │  TripDetailPage) │
└────────────────┘     │                 │     └────────┬─────────┘
                       │ Send(Message) ──┼──────────┐   │
                       └─────────────────┘          │   │ DI Container
                                                    ▼   ▼
┌────────────────┐     ┌─────────────────┐     ┌──────────────────┐
│TripDetailPage  │◄────│TripDetailVM     │◄────│   MauiProgram    │
│    (XAML)      │     │                 │     │   (DI Setup)     │
│                │     │ Receive(msg)    │     └──────────────────┘
│ {Binding       │◄────│ LoadTripStops() │
│  TripStops}    │     │      │          │
└────────────────┘     └──────┼──────────┘
                              │
                              ▼
                       ┌─────────────────┐     ┌──────────────────┐
                       │ TripDataService │────►│   API Controller │
                       │  HTTP GET       │     │   + Database     │
                       └─────────────────┘     └──────────────────┘
```

### Waarom ObservableCollection?

```
ViewModel                              View (XAML)
─────────────────────────────────────────────────────────────────

TripStops = new ObservableCollection   ──► ItemsSource="{Binding TripStops}"
    │                                          │
    └─ [Stop1, Stop2, Stop3]                   └─► CollectionView rendert 3 items
                                                       │
                                                       ├─ Stop1.Title → Label
                                                       ├─ Stop1.PhotoUrl → Image
                                                       └─ Stop1.Address → Label

ObservableCollection<TripStop>
        │
        └─► Implementeert INotifyCollectionChanged
                │
                └─► MAUI detecteert Add/Remove/Clear
                        │
                        └─► UI update AUTOMATISCH
```

---

## TripsPage - Lijst van Trips

> [!tip] Examenvraag
> **Vraag:** Waarom gebruik je `RelativeSource AncestorType` in een CollectionView DataTemplate?
>
> **Antwoord:** Binnen een DataTemplate is de BindingContext het individuele list item (bijv. één Trip). Om een command van de parent ViewModel aan te roepen, moet je met `RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}` naar de ViewModel van de parent page verwijzen.

### TripsPage.xaml

**Locatie:** `Views/TripsPage.xaml`

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:viewmodels="clr-namespace:TripTracker.App.ViewModels"
             x:Class="TripTracker.App.Views.TripsPage"
             Title="My Trips">

    <Grid RowDefinitions="Auto, Auto, *" Padding="10">

        <!-- Header met map en add button -->
        <HorizontalStackLayout HorizontalOptions="End" Spacing="10">
            <Button Text="🌍"
                    Command="{Binding ShowAllOnMapCommand}"
                    FontSize="22"
                    BackgroundColor="#512BD4"
                    WidthRequest="50" HeightRequest="50"
                    CornerRadius="25" Padding="0"/>
            <Button Text="+"
                    Command="{Binding AddTripCommand}"
                    FontSize="22"
                    WidthRequest="50" HeightRequest="50"
                    CornerRadius="25" Padding="0"/>
        </HorizontalStackLayout>

        <!-- Jaar filter tabs -->
        <ScrollView Grid.Row="1" Orientation="Horizontal" Margin="0,10,0,5">
            <StackLayout BindableLayout.ItemsSource="{Binding YearFilters}"
                         Orientation="Horizontal" Spacing="8">
                <BindableLayout.ItemTemplate>
                    <DataTemplate>
                        <Button Text="{Binding DisplayText}"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}}, Path=SelectYearCommand}"
                                CommandParameter="{Binding}"
                                Padding="15,8" CornerRadius="15" FontSize="14"
                                BackgroundColor="#E0E0E0" TextColor="Black">
                            <Button.Triggers>
                                <DataTrigger TargetType="Button" Binding="{Binding IsSelected}" Value="True">
                                    <Setter Property="BackgroundColor" Value="#512BD4"/>
                                    <Setter Property="TextColor" Value="White"/>
                                </DataTrigger>
                            </Button.Triggers>
                        </Button>
                    </DataTemplate>
                </BindableLayout.ItemTemplate>
            </StackLayout>
        </ScrollView>

        <!-- CollectionView met SwipeView voor edit/delete -->
        <CollectionView Grid.Row="2"
                        ItemsSource="{Binding FilteredTrips}"
                        SelectionMode="None">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <SwipeView>
                        <!-- Swipe rechts → Edit -->
                        <SwipeView.LeftItems>
                            <SwipeItems>
                                <SwipeItem Text="Edit" BackgroundColor="#512BD4"
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}}, Path=EditTripCommand}"
                                    CommandParameter="{Binding .}"/>
                            </SwipeItems>
                        </SwipeView.LeftItems>

                        <!-- Swipe links → Delete -->
                        <SwipeView.RightItems>
                            <SwipeItems>
                                <SwipeItem Text="Delete" BackgroundColor="Red"
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}}, Path=DeleteTripCommand}"
                                    CommandParameter="{Binding .}"/>
                            </SwipeItems>
                        </SwipeView.RightItems>

                        <!-- Trip card (Border ipv Frame) -->
                        <Border Margin="0,5" Padding="15"
                                StrokeShape="RoundRectangle 10" Stroke="LightGray"
                                BackgroundColor="White">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}}, Path=ViewTripCommand}"
                                    CommandParameter="{Binding .}"/>
                            </Border.GestureRecognizers>
                            <Grid ColumnDefinitions="Auto, *" ColumnSpacing="15">
                                <Image Source="{Binding ImageUrl}"
                                       WidthRequest="80" HeightRequest="80"
                                       Aspect="AspectFill"/>
                                <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                                    <Label Text="{Binding Name}"
                                           FontSize="18" FontAttributes="Bold" TextColor="Black"/>
                                    <Label Text="{Binding Description}"
                                           FontSize="14" TextColor="Gray" MaxLines="2"/>
                                    <Label FontSize="12" TextColor="DarkGray">
                                        <Label.FormattedText>
                                            <FormattedString>
                                                <Span Text="{Binding StartDate, StringFormat='{0:dd MMM yyyy}'}"/>
                                                <Span Text=" - "/>
                                                <Span Text="{Binding EndDate, StringFormat='{0:dd MMM yyyy}'}"/>
                                            </FormattedString>
                                        </Label.FormattedText>
                                    </Label>
                                </VerticalStackLayout>
                            </Grid>
                        </Border>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>

            <CollectionView.EmptyView>
                <VerticalStackLayout VerticalOptions="Center" HorizontalOptions="Center">
                    <Label Text="No trips yet" FontSize="18" TextColor="Gray"/>
                    <Label Text="Tap + to add your first trip" FontSize="14" TextColor="LightGray"/>
                </VerticalStackLayout>
            </CollectionView.EmptyView>
        </CollectionView>

        <!-- Loading overlay -->
        <Grid Grid.RowSpan="3" BackgroundColor="#80000000" IsVisible="{Binding IsLoading}">
            <ActivityIndicator IsRunning="True" Color="White" HeightRequest="50" WidthRequest="50"
                               VerticalOptions="Center" HorizontalOptions="Center"/>
        </Grid>
    </Grid>
</ContentPage>
```

**Belangrijke patronen:**

1. **SwipeView voor Edit/Delete**
   - `LeftItems` = swipe naar rechts → Edit actie
   - `RightItems` = swipe naar links → Delete actie
   - Moderne UX, geen expliciete knoppen nodig

2. **Border vs Frame**
   - `Border` met `StrokeShape="RoundRectangle 10"` = afgeronde hoeken
   - Moderner dan Frame, meer controle over styling

3. **Year Filter met BindableLayout**
   - `BindableLayout.ItemsSource` bindt aan `YearFilters` collectie
   - `DataTrigger` wijzigt kleur als `IsSelected = true`

4. **FilteredTrips vs Trips**
   - Bindt aan `FilteredTrips` (gefilterde lijst)
   - ViewModel filtert op basis van `SelectedYear`

5. **Loading Overlay**
   - `Grid.RowSpan="3"` bedekt hele pagina
   - Semi-transparant: `#80000000` (50% opacity zwart)
   - `IsVisible="{Binding IsLoading}"` toont/verbergt

6. **FormattedString voor datum range**
   - Combineert meerdere Spans in één Label
   - `StartDate - EndDate` in één regel

### TripsPage.xaml.cs

**Locatie:** `Views/TripsPage.xaml.cs`

```csharp
using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind is LEEG - alle logica zit in ViewModel!
// Dit is een MUST voor MVVM (cursus vereiste)
public partial class TripsPage : ContentPage
{
    public TripsPage(ITripsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

> [!warning] MVVM Regel
> **Code-behind blijft LEEG!** Dit is een examen MUST. Alle logica hoort in de ViewModel. De enige code hier is:
> 1. Constructor met DI
> 2. `InitializeComponent()`
> 3. `BindingContext` toewijzen

---

## TripDetailPage - Trip met Stops

### TripDetailPage.xaml

**Locatie:** `Views/TripDetailPage.xaml`

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:viewmodels="clr-namespace:TripTracker.App.ViewModels"
             x:Class="TripTracker.App.Views.TripDetailPage"
             Title="{Binding CurrentTrip.Name}">

    <Grid RowDefinitions="Auto, Auto, *" Padding="10">

        <!-- Trip header -->
        <VerticalStackLayout Spacing="5">
            <Image Source="{Binding CurrentTrip.ImageUrl}"
                   HeightRequest="200" Aspect="AspectFill"/>
            <Label Text="{Binding CurrentTrip.Description}"
                   FontSize="14" TextColor="Gray"/>
            <HorizontalStackLayout Spacing="10">
                <Label Text="{Binding CurrentTrip.StartDate, StringFormat='Start: {0:dd MMM yyyy}'}"
                       FontSize="12"/>
                <Label Text="{Binding CurrentTrip.EndDate, StringFormat='End: {0:dd MMM yyyy}'}"
                       FontSize="12"/>
            </HorizontalStackLayout>
        </VerticalStackLayout>

        <!-- Stops header met map en add button -->
        <Grid Grid.Row="1" ColumnDefinitions="*, Auto, Auto" ColumnSpacing="10" Margin="0,15,0,10">
            <Label Text="Stops" FontSize="20" FontAttributes="Bold" VerticalOptions="Center"/>
            <Button Text="🌍"
                    Command="{Binding ShowTripOnMapCommand}"
                    Grid.Column="1"
                    FontSize="22" WidthRequest="50" HeightRequest="50"
                    CornerRadius="25" Padding="0" BackgroundColor="#512BD4"/>
            <Button Text="+ Add Stop"
                    Command="{Binding AddStopCommand}"
                    Grid.Column="2"/>
        </Grid>

        <!-- CollectionView met SwipeView voor edit/delete -->
        <CollectionView Grid.Row="2"
                        ItemsSource="{Binding TripStops}"
                        SelectionMode="None">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <SwipeView>
                        <!-- Swipe rechts → Edit -->
                        <SwipeView.LeftItems>
                            <SwipeItems>
                                <SwipeItem Text="Edit" BackgroundColor="#512BD4"
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripDetailViewModel}}, Path=EditStopCommand}"
                                    CommandParameter="{Binding .}"/>
                            </SwipeItems>
                        </SwipeView.LeftItems>

                        <!-- Swipe links → Delete -->
                        <SwipeView.RightItems>
                            <SwipeItems>
                                <SwipeItem Text="Delete" BackgroundColor="Red"
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripDetailViewModel}}, Path=DeleteStopCommand}"
                                    CommandParameter="{Binding .}"/>
                            </SwipeItems>
                        </SwipeView.RightItems>

                        <!-- Stop card -->
                        <Border Margin="0,5" Padding="10"
                                StrokeShape="RoundRectangle 8" Stroke="LightGray"
                                BackgroundColor="White">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripDetailViewModel}}, Path=ViewStopCommand}"
                                    CommandParameter="{Binding .}"/>
                            </Border.GestureRecognizers>
                            <Grid ColumnDefinitions="Auto, *" ColumnSpacing="10">
                                <Image Source="{Binding PhotoUrl}"
                                       WidthRequest="60" HeightRequest="60"
                                       Aspect="AspectFill"/>
                                <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                                    <Label Text="{Binding Title}"
                                           FontSize="16" FontAttributes="Bold" TextColor="Black"/>
                                    <Label Text="{Binding Address}"
                                           FontSize="12" TextColor="Gray"/>
                                    <Label Text="{Binding DateTime, StringFormat='{0:dd MMM yyyy HH:mm}'}"
                                           FontSize="10" TextColor="DarkGray"/>
                                </VerticalStackLayout>
                            </Grid>
                        </Border>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>

            <CollectionView.EmptyView>
                <VerticalStackLayout VerticalOptions="Center" HorizontalOptions="Center">
                    <Label Text="No stops yet" FontSize="18" TextColor="Gray"/>
                    <Label Text="Tap 'Add Stop' to add your first stop" FontSize="14" TextColor="LightGray"/>
                </VerticalStackLayout>
            </CollectionView.EmptyView>
        </CollectionView>

        <!-- Loading overlay -->
        <Grid Grid.RowSpan="3" BackgroundColor="#80000000" IsVisible="{Binding IsLoading}">
            <ActivityIndicator IsRunning="True" Color="White" HeightRequest="50" WidthRequest="50"
                               VerticalOptions="Center" HorizontalOptions="Center"/>
        </Grid>
    </Grid>
</ContentPage>
```

**Belangrijke patronen:**

1. **SwipeView voor Edit/Delete** (zelfde patroon als TripsPage)
   - Geen expliciete delete button meer in de lijst
   - Cleaner UI, moderne UX

2. **Map Button toegevoegd**
   - `ShowTripOnMapCommand` toont alleen stops van deze trip
   - 3-koloms Grid: `*, Auto, Auto` voor label + 2 buttons

3. **Border ipv Frame**
   - `StrokeShape="RoundRectangle 8"` voor afgeronde hoeken
   - Consistentie met TripsPage

### TripDetailPage.xaml.cs

```csharp
using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind is LEEG - alle logica zit in ViewModel!
public partial class TripDetailPage : ContentPage
{
    public TripDetailPage(ITripDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

---

## AddStopPage - Stop Toevoegen

> [!tip] Examenvraag
> **Vraag:** Hoe gebruik je een Value Converter in XAML?
>
> **Antwoord:**
> 1. Registreer converter in App.xaml: `<converters:InvertedBoolConverter x:Key="InvertedBoolConverter"/>`
> 2. Gebruik in binding: `IsVisible="{Binding HasPhoto, Converter={StaticResource InvertedBoolConverter}}"`
> 3. `StaticResource` verwijst naar de x:Key uit App.xaml

### AddStopPage.xaml

**Locatie:** `Views/AddStopPage.xaml`

```xaml
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

**Belangrijke patronen:**

1. **Conditional Visibility met Converter**
   - `IsVisible="{Binding HasPhoto, Converter={StaticResource InvertedBoolConverter}}"`
   - Toont placeholder als HasPhoto = false
   - InvertedBoolConverter keert boolean om

2. **Overlay Pattern**
   - Outer Grid met twee children: content + overlay
   - Overlay heeft semi-transparent background: `#80000000` (50% opacity zwart)
   - ActivityIndicator voor loading state

3. **Entry vs Editor**
   - `Entry`: Single-line text input
   - `Editor`: Multi-line text input met HeightRequest

4. **StaticResource voor Colors**
   - `TextColor="{StaticResource Gray600}"` verwijst naar Colors.xaml
   - Consistente kleuren door hele app

### AddStopPage.xaml.cs

```csharp
using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind is LEEG - alle logica zit in ViewModel!
public partial class AddStopPage : ContentPage
{
    public AddStopPage(IAddStopViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

---

## StopDetailPage - Stop Details

### StopDetailPage.xaml

**Locatie:** `Views/StopDetailPage.xaml`

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TripTracker.App.Views.StopDetailPage"
             Title="{Binding CurrentStop.Title}">

    <!-- Detail pagina voor een TripStop -->

    <ScrollView Padding="15">
        <VerticalStackLayout Spacing="15">

            <!-- Foto -->
            <Image Source="{Binding CurrentStop.PhotoUrl}"
                   HeightRequest="250"
                   Aspect="AspectFill"/>

            <!-- Title en beschrijving -->
            <Label Text="{Binding CurrentStop.Title}"
                   FontSize="24"
                   FontAttributes="Bold"/>

            <Label Text="{Binding CurrentStop.Description}"
                   FontSize="16"/>

            <!-- Locatie info -->
            <Frame BorderColor="LightGray" Padding="10">
                <VerticalStackLayout Spacing="5">
                    <Label Text="Location" FontSize="16" FontAttributes="Bold"/>

                    <Label Text="{Binding CurrentStop.Address}"
                           FontSize="14"/>

                    <Label Text="{Binding CurrentStop.Country}"
                           FontSize="14"
                           TextColor="Gray"/>

                    <HorizontalStackLayout Spacing="20">
                        <Label Text="{Binding CurrentStop.Latitude, StringFormat='Lat: {0:F6}'}"
                               FontSize="12"/>
                        <Label Text="{Binding CurrentStop.Longitude, StringFormat='Lng: {0:F6}'}"
                               FontSize="12"/>
                    </HorizontalStackLayout>
                </VerticalStackLayout>
            </Frame>

            <!-- DateTime -->
            <Label Text="{Binding CurrentStop.DateTime, StringFormat='Visited on: {0:dd MMMM yyyy at HH:mm}'}"
                   FontSize="14"
                   TextColor="Gray"/>

            <!-- Action buttons -->
            <Grid ColumnDefinitions="*, *" ColumnSpacing="10" Margin="0,20,0,0">
                <Button Text="Edit"
                        Command="{Binding EditCommand}"
                        BackgroundColor="DarkBlue"
                        TextColor="White"/>
                <Button Text="Delete"
                        Command="{Binding DeleteCommand}"
                        Grid.Column="1"
                        BackgroundColor="Red"
                        TextColor="White"/>
            </Grid>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

**Belangrijke patronen:**

1. **ScrollView voor Lange Content**
   - Altijd ScrollView als content mogelijk te lang is
   - Voorkomt dat content buiten scherm valt
   - Eén direct child (meestal VerticalStackLayout)

2. **StringFormat met Precision**
   - `StringFormat='Lat: {0:F6}'` = 6 decimalen voor latitude
   - `F6` = Fixed-point met 6 decimalen
   - Goed voor GPS coordinaten (bijv. 51.123456)

3. **Frame voor Grouping**
   - Frame groepeert gerelateerde info visueel
   - BorderColor, Padding, CornerRadius voor styling
   - Alternatief voor Border control

### StopDetailPage.xaml.cs

```csharp
using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

// Code-behind is LEEG - alle logica zit in ViewModel!
public partial class StopDetailPage : ContentPage
{
    public StopDetailPage(IStopDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

---

## Overige Pages (Kort Overzicht)

De volgende pages volgen dezelfde patronen als hierboven. Hier een kort overzicht:

### AddTripPage

**Doel:** Nieuwe trip aanmaken met naam, beschrijving, datums en optionele foto.

**Key bindings:**
```xaml
<Entry Text="{Binding Name}" Placeholder="Trip name"/>
<Editor Text="{Binding Description}"/>
<DatePicker Date="{Binding StartDate}"/>
<DatePicker Date="{Binding EndDate}"/>
<Button Command="{Binding PickPhotoCommand}"/>
<Button Command="{Binding SaveCommand}"/>
```

**Patroon:** Vergelijkbaar met AddStopPage maar zonder GPS/AI analyse.

---

### EditTripPage

**Doel:** Bestaande trip bewerken. Ontvangt trip via `TripEditMessage`.

**Key bindings:** Zelfde als AddTripPage, maar met bestaande data ingevuld.

**Code-behind:**
```csharp
public EditTripPage(IEditTripViewModel viewModel)
{
    InitializeComponent();
    BindingContext = viewModel;
}
```

---

### EditStopPage

**Doel:** Bestaande stop bewerken. Ontvangt stop via `StopEditMessage`.

**Key features:**
- Foto kan gewijzigd worden
- AI analyse beschikbaar voor nieuwe foto
- Adres wijzigen triggert forward geocoding
- GPS coördinaten blijven behouden tenzij adres wijzigt

**Patroon:** Combinatie van AddStopPage (foto/AI) en EditTripPage (bestaande data).

---

### MapPage

**Doel:** Toon stops op een interactieve kaart met Mapsui.

**Key features:**
```xaml
<!-- Mapsui MapControl -->
<mapsui:MapControl x:Name="MapControl"/>
```

**ViewModel ontvangt:** `ShowStopsOnMapMessage` met tuple `(List<TripStop>, string Title)`

**Mapsui setup:**
- OpenStreetMap tiles als achtergrond
- Pins voor elke stop met custom icon
- Zoom naar bounding box van alle stops

> [!info] Mapsui Documentatie
> Zie [mapsui.com](https://mapsui.com) voor uitgebreide documentatie.

---

## Value Converters

> [!info] Wat zijn Value Converters?
> Value Converters transformeren data tussen ViewModel en View. Gebruik ze voor:
> - Boolean inversie (IsVisible met NOT logic)
> - Kleur transformaties (status → color)
> - Format conversies (aantal → tekst)
>
> **BELANGRIJK:** Houd ze simpel! Complexe logica hoort in ViewModel.

### InvertedBoolConverter

**Locatie:** `Converters/InvertedBoolConverter.cs`

**Doel:** Keer boolean waarde om voor conditional visibility

```csharp
using System.Globalization;

namespace TripTracker.App.Converters
{
    // Converter om boolean waarden om te keren
    // Nodig voor IsVisible bindings (toon placeholder als GEEN foto)
    public class InvertedBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}
```

**Interface:**

```csharp
public interface IValueConverter
{
    // ViewModel → View (bijv. bij data binding)
    object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    // View → ViewModel (bijv. bij two-way binding)
    object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}
```

**Parameters uitleg:**

| Parameter | Type | Uitleg |
|-----------|------|--------|
| `value` | `object?` | De waarde uit de binding (bijv. `HasPhoto = true`) |
| `targetType` | `Type` | Het type waar je naartoe converteert (bijv. `bool` voor IsVisible) |
| `parameter` | `object?` | Optionele parameter vanuit XAML: `ConverterParameter="..."` |
| `culture` | `CultureInfo` | Culture info voor locale-specific formatting |

### Registreren in App.xaml

**Locatie:** `App.xaml`

```xaml
<?xml version = "1.0" encoding = "UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:TripTracker.App"
             xmlns:converters="clr-namespace:TripTracker.App.Converters"
             x:Class="TripTracker.App.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>

            <!-- Converters -->
            <converters:InvertedBoolConverter x:Key="InvertedBoolConverter"/>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Gebruik in XAML:**

```xaml
<!-- Toon placeholder als HasPhoto = false -->
<Label IsVisible="{Binding HasPhoto, Converter={StaticResource InvertedBoolConverter}}"
       Text="No photo yet"/>

<!-- Toon image als HasPhoto = true -->
<Image IsVisible="{Binding HasPhoto}"
       Source="{Binding PhotoUrl}"/>
```

---

## Code-Behind Pattern

> [!warning] EXAMENVEREISTE: Code-Behind blijft LEEG!
> Dit is een **absolute MUST** voor het examen. Alle views moeten het onderstaande pattern volgen.

**Het enige toegestane pattern:**

```csharp
using TripTracker.App.ViewModels;

namespace TripTracker.App.Views;

public partial class ExamplePage : ContentPage
{
    // Constructor met Dependency Injection
    public ExamplePage(IExampleViewModel viewModel)
    {
        InitializeComponent();           // XAML initialiseren
        BindingContext = viewModel;      // ViewModel koppelen
    }

    // GEEN andere code!
    // GEEN event handlers!
    // GEEN business logica!
}
```

**Waarom LEEG houden?**

| Verboden | Waarom | Alternatief |
|----------|--------|-------------|
| Event handlers | Tight coupling, niet testbaar | Commands in ViewModel |
| Business logica | Niet testbaar, niet herbruikbaar | Methods in ViewModel |
| Data manipulatie | Breekt MVVM pattern | Properties in ViewModel |
| Navigation logic | Hard to maintain | NavigationService |

**Examenvraag scenario:**

> "Je moet een button click afhandelen die data ophaalt van een API. Waar implementeer je dit?"
>
> **FOUT:** Event handler in code-behind
>
> **CORRECT:** `ICommand` property in ViewModel, gebind via `Command="{Binding LoadDataCommand}"` in XAML

---

## Dependency Injection Setup

> [!tip] Examenvraag
> **Vraag:** Wat is het verschil tussen AddSingleton en AddTransient?
>
> **Antwoord:**
> - **AddSingleton:** Eén instantie voor de hele app lifetime. Gebruik voor services die state delen (NavigationService, API clients)
> - **AddTransient:** Nieuwe instantie bij elke request. Gebruik voor pages/ViewModels die fresh state nodig hebben (detail pages)

### MauiProgram.cs

**Locatie:** `MauiProgram.cs`

```csharp
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;  // Voor Mapsui
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
            .UseSkiaSharp()  // Vereist voor Mapsui kaarten
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Debug exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Debug.WriteLine($"[AppDomain] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Debug.WriteLine($"[TaskScheduler] {e.Exception}");
            e.SetObserved();
        };

        // ===== Services registreren =====
        builder.Services.AddTransient<INavigationService, NavigationService>();

        // Smart Stop Capture services (Singleton)
        builder.Services.AddSingleton<IPhotoService, PhotoService>();
        builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
        builder.Services.AddSingleton<IGeocodingService, GeocodingService>();
        builder.Services.AddSingleton<IAnalyzeImageService, AnalyzeImageService>();

        // ===== Pages en ViewModels registreren (8 stuks) =====

        // Trips pagina (Singleton - hoofdpagina)
        builder.Services.AddSingleton<TripsPage>();
        builder.Services.AddSingleton<ITripsViewModel, TripsViewModel>();

        // Trip detail (Transient)
        builder.Services.AddTransient<TripDetailPage>();
        builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();

        // Add trip (Transient)
        builder.Services.AddTransient<AddTripPage>();
        builder.Services.AddTransient<IAddTripViewModel, AddTripViewModel>();

        // Edit trip (Transient)
        builder.Services.AddTransient<EditTripPage>();
        builder.Services.AddTransient<IEditTripViewModel, EditTripViewModel>();

        // Add stop (Transient)
        builder.Services.AddTransient<AddStopPage>();
        builder.Services.AddTransient<IAddStopViewModel, AddStopViewModel>();

        // Edit stop (Transient)
        builder.Services.AddTransient<EditStopPage>();
        builder.Services.AddTransient<IEditStopViewModel, EditStopViewModel>();

        // Stop detail (Transient)
        builder.Services.AddTransient<StopDetailPage>();
        builder.Services.AddTransient<IStopDetailViewModel, StopDetailViewModel>();

        // Map pagina (Transient) - met Mapsui
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<IMapViewModel, MapViewModel>();

        return builder.Build();
    }
}
```

**Overzicht registraties (8 pages):**

| Page | ViewModel | Lifetime | Reden |
|------|-----------|----------|-------|
| TripsPage | TripsViewModel | Singleton | Hoofdpagina, state behouden |
| TripDetailPage | TripDetailViewModel | Transient | Fresh state per trip |
| AddTripPage | AddTripViewModel | Transient | Lege form per keer |
| EditTripPage | EditTripViewModel | Transient | Andere trip per keer |
| AddStopPage | AddStopViewModel | Transient | Lege form per keer |
| EditStopPage | EditStopViewModel | Transient | Andere stop per keer |
| StopDetailPage | StopDetailViewModel | Transient | Fresh state per stop |
| MapPage | MapViewModel | Transient | Andere stops per keer |

**Lifetime keuze strategie:**

| Type | Lifetime | Reden |
|------|----------|-------|
| Main pages (tabs) | Singleton | Blijven bestaan, state behouden |
| Detail pages | Transient | Fresh state bij elke navigatie |
| Services (API, DB) | Singleton | Eén instantie delen, connection pooling |
| Navigation | Transient | Nieuwe context per navigatie |
| ViewModels (main) | Singleton | Gekoppeld aan singleton page |
| ViewModels (detail) | Transient | Gekoppeld aan transient page |

**Waarom page + ViewModel zelfde lifetime?**

- Page krijgt ViewModel via constructor
- Als page Singleton is, moet ViewModel ook Singleton zijn
- Als page Transient is, moet ViewModel ook Transient zijn
- Voorkomt lifetime mismatch errors

---

## Examenvragen

### Vraag 1: Data Binding

**Vraag:** Gegeven deze ViewModel property:

```csharp
public ObservableCollection<Trip> Trips { get; set; }
```

Schrijf de XAML voor een CollectionView die:
1. Bindt aan Trips
2. Toont Trip.Name in een Label
3. Heeft een TapGestureRecognizer die ViewTripCommand aanroept met de Trip als parameter

<details>
<summary>Antwoord</summary>

```xaml
<CollectionView ItemsSource="{Binding Trips}" SelectionMode="None">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <Frame>
                <Frame.GestureRecognizers>
                    <TapGestureRecognizer
                        Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}}, Path=ViewTripCommand}"
                        CommandParameter="{Binding .}"/>
                </Frame.GestureRecognizers>
                <Label Text="{Binding Name}"/>
            </Frame>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

**Uitleg:**
- `ItemsSource="{Binding Trips}"` bindt aan ViewModel property
- Binnen DataTemplate is BindingContext de Trip (niet ViewModel)
- `RelativeSource AncestorType` gaat terug naar parent ViewModel
- `CommandParameter="{Binding .}"` geeft hele Trip object mee
</details>

---

### Vraag 2: Value Converter

**Vraag:** Je hebt een boolean property `IsLoading` in de ViewModel. Je wilt een Button disablen tijdens loading. Schrijf:
1. De converter class
2. Registratie in App.xaml
3. Gebruik in XAML

<details>
<summary>Antwoord</summary>

**1. Converter (optie A: InvertedBoolConverter hergebruiken):**

Al geïmplementeerd, zie hierboven.

**2. Registratie App.xaml:**

```xaml
<Application.Resources>
    <ResourceDictionary>
        <converters:InvertedBoolConverter x:Key="InvertedBoolConverter"/>
    </ResourceDictionary>
</Application.Resources>
```

**3. Gebruik in XAML:**

```xaml
<Button Text="Save"
        IsEnabled="{Binding IsLoading, Converter={StaticResource InvertedBoolConverter}}"
        Command="{Binding SaveCommand}"/>
```

**Uitleg:** Button is enabled als IsLoading = false. Converter keert boolean om.
</details>

---

### Vraag 3: Code-Behind

**Vraag:** Waarom is deze code-behind FOUT volgens MVVM?

```csharp
public partial class TripsPage : ContentPage
{
    private readonly ApiService _apiService;

    public TripsPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnAddButtonClicked(object sender, EventArgs e)
    {
        var trip = await _apiService.AddTripAsync(new Trip { Name = "New Trip" });
        // Refresh lijst
    }
}
```

Wat zijn de problemen en hoe fix je het?

<details>
<summary>Antwoord</summary>

**Problemen:**

1. **Event handler in code-behind** → moet Command in ViewModel zijn
2. **Business logica in View** → moet in ViewModel
3. **Direct API call** → moet via service in ViewModel
4. **Geen DI voor ApiService** → moet geïnjecteerd worden
5. **Geen BindingContext** → ViewModel ontbreekt

**Correcte implementatie:**

**Code-behind (LEEG!):**

```csharp
public partial class TripsPage : ContentPage
{
    public TripsPage(ITripsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

**XAML:**

```xaml
<Button Text="Add" Command="{Binding AddTripCommand}"/>
```

**ViewModel:**

```csharp
public class TripsViewModel : ObservableRecipient, IRecipient<RefreshDataMessage>, ITripsViewModel
{
    private readonly INavigationService _navigationService;

    public ICommand AddTripCommand { get; }

    public TripsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Messenger.Register<TripsViewModel, RefreshDataMessage>(this, (r, m) => r.Receive(m));
        AddTripCommand = new AsyncRelayCommand(AddTripAsync);
    }

    private async Task AddTripAsync()
    {
        var trip = await _apiService.AddTripAsync(new Trip { Name = "New Trip" });
        Trips.Add(trip);
    }
}
```
</details>

---

### Vraag 4: Navigation

**Vraag:** Implementeer een NavigationService methode die navigeert naar TripDetailPage via Dependency Injection.

<details>
<summary>Antwoord</summary>

**NavigationService.cs:**

```csharp
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
        // Haal page op via DI - dit zorgt dat ViewModel automatisch geïnjecteerd wordt
        var page = _serviceProvider.GetRequiredService<TripDetailPage>();
        await _navigation.PushAsync(page);
    }
}
```

**MauiProgram.cs registratie:**

```csharp
// Service
builder.Services.AddTransient<INavigationService, NavigationService>();

// Page + ViewModel (Transient voor fresh state)
builder.Services.AddTransient<TripDetailPage>();
builder.Services.AddTransient<ITripDetailViewModel, TripDetailViewModel>();
```

**Gebruik in ViewModel:**

```csharp
private readonly INavigationService _navigationService;

public TripsViewModel(INavigationService navigationService)
{
    _navigationService = navigationService;
    ViewTripCommand = new AsyncRelayCommand<Trip>(ViewTripAsync);
}

private async Task ViewTripAsync(Trip trip)
{
    // Sla trip op in state (bijv. via MessagingCenter of shared service)
    await _navigationService.NavigateToTripDetailPageAsync();
}
```
</details>

---

### Vraag 5: Grid Layout

**Vraag:** Maak een Grid layout met:
- Rij 1: Auto height (header)
- Rij 2: Vult resterende ruimte (content)
- Kolom 1: 2/3 van breedte (main)
- Kolom 2: 1/3 van breedte (sidebar)

<details>
<summary>Antwoord</summary>

```xaml
<Grid RowDefinitions="Auto, *" ColumnDefinitions="2*, *">
    <!-- Rij 0, Kolom 0 (header spanning alle kolommen) -->
    <Label Grid.Row="0" Grid.ColumnSpan="2" Text="Header" FontSize="24"/>

    <!-- Rij 1, Kolom 0 (main content) -->
    <VerticalStackLayout Grid.Row="1" Grid.Column="0">
        <Label Text="Main content area"/>
    </VerticalStackLayout>

    <!-- Rij 1, Kolom 1 (sidebar) -->
    <VerticalStackLayout Grid.Row="1" Grid.Column="1" BackgroundColor="LightGray">
        <Label Text="Sidebar"/>
    </VerticalStackLayout>
</Grid>
```

**Uitleg:**
- `RowDefinitions="Auto, *"`: Auto height + fill remaining
- `ColumnDefinitions="2*, *"`: 2:1 verhouding (2/3 vs 1/3)
- `Grid.ColumnSpan="2"`: Element spanning 2 kolommen
- `*` betekent proportioneel, `2*` is 2x zo groot als `*`
</details>

---

### Vraag 6: String Formatting

**Vraag:** Gegeven een DateTime property `StartDate` in de ViewModel, schrijf de binding die toont als "Start: 20 Dec 2024".

<details>
<summary>Antwoord</summary>

```xaml
<Label Text="{Binding StartDate, StringFormat='Start: {0:dd MMM yyyy}'}"/>
```

**Format codes:**
- `dd` = dag met leading zero (01-31)
- `MMM` = afgekorte maand naam (Jan, Feb, ...)
- `yyyy` = jaar met 4 cijfers

**Alternatieve formats:**
- `{0:dd/MM/yyyy}` → "20/12/2024"
- `{0:dddd, MMMM dd}` → "Friday, December 20"
- `{0:HH:mm}` → "14:30" (24u format)
- `{0:hh:mm tt}` → "02:30 PM" (12u format)
</details>

---

## Conclusie

Deze fase implementeert de complete UI laag van TripTracker volgens MVVM best practices:

**Checklist:**

- [x] AppShell met TabBar navigatie
- [x] 4 ContentPages (Trips, TripDetail, AddStop, StopDetail)
- [x] Code-behind volledig LEEG (alleen DI constructor)
- [x] CollectionView met DataTemplate voor lijsten
- [x] Data binding met StringFormat
- [x] Command binding voor user interactions
- [x] Value Converter voor conditional visibility
- [x] Dependency Injection setup voor alle pages/ViewModels
- [x] Singleton vs Transient lifetime strategieën
- [x] NavigationService met DI

**Examen MUST-HAVES:**

1. Code-behind LEEG
2. BindingContext via constructor injection
3. Commands ipv event handlers
4. RelativeSource AncestorType voor parent bindings
5. Value Converters in App.xaml registreren

**Volgende fase:** Fase 6 - Smart Stop Capture (Camera, GPS, Geocoding)
