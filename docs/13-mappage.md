---
fase: 13
status: Voltooid
tags:
  - mapsui
  - openstreetmap
  - mvvm
  - messaging
  - coordinates
created: 2025-12-20
---

# Fase 13: MapPage met Mapsui

## Overzicht

In deze fase hebben we een **interactieve kaart** geïmplementeerd met Mapsui (OpenStreetMap). De kaart toont TripStops met gekleurde pins per trip.

> [!info] Cursus Referentie
> Kaartintegratie combineert meerdere concepten: NuGet packages, coordinate projectie, en een gedocumenteerde MVVM uitzondering voor UI-specifieke code.

---

## Waarom Mapsui?

| Optie | Voordelen | Nadelen |
|-------|-----------|---------|
| **Mapsui** | Open source, OpenStreetMap, geen API key | Minder features dan Google |
| Google Maps | Meer features, Street View | API key nodig, kosten |
| Bing Maps | Microsoft integratie | API key nodig |

**Gekozen:** Mapsui - gratis, geen API key, voldoende voor onze use case.

---

## MVVM architectuur met event bridge

```
┌─────────────────────────────────────────────────────────────┐
│           MapPage Architectuur (MVVM + Event Bridge)        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  TripsPage / TripDetailPage / StopDetailPage                 │
│         │                                                    │
│         │ NavigateToMapPageAsync()                           │
│         │ WeakReferenceMessenger.Send(ShowStopsOnMapMessage) │
│         ▼                                                    │
│  ┌─────────────────────────────────────────────┐            │
│  │            MapViewModel.cs                   │            │
│  │  ┌────────────────────────────────────────┐ │            │
│  │  │ Receive(ShowStopsOnMapMessage)         │ │            │
│  │  │   ├── Stops.Clear() + Add()            │ │            │
│  │  │   ├── Title = mapTitle                 │ │            │
│  │  │   └── StopsUpdated?.Invoke() ──────────┼─┼────┐       │
│  │  └────────────────────────────────────────┘ │    │       │
│  │  ObservableCollection<TripStop> Stops       │    │       │
│  └─────────────────────────────────────────────┘    │       │
│                                                      │       │
│                                                      ▼       │
│  ┌─────────────────────────────────────────────┐            │
│  │            MapPage.xaml.cs                   │            │
│  │  ┌────────────────────────────────────────┐ │            │
│  │  │ OnStopsUpdated() - Event handler       │ │            │
│  │  │   ├── BuildFeatures(Stops)             │ │            │
│  │  │   ├── _pinsLayer.Features = features   │ │            │
│  │  │   ├── ZoomToExtent()                   │ │            │
│  │  │   └── mapControl.Refresh()             │ │            │
│  │  └────────────────────────────────────────┘ │            │
│  │  ⚠️ MVVM Exception: Mapsui vereist code    │            │
│  └─────────────────────────────────────────────┘            │
│                                                              │
│  ┌─────────────────────────────────────────────┐            │
│  │            MapPage.xaml                      │            │
│  │  <mapsui:MapControl x:Name="mapControl"/>   │            │
│  └─────────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────────┘
```

### MVVM exception uitleg

MapPage code-behind bevat Mapsui setup code. Dit is een **gedocumenteerde uitzondering** omdat:
1. Mapsui MapControl vereist programmatische initialisatie
2. Layer management is UI-specifiek, niet business logic
3. Alternatieven (custom renderer) zijn te complex

**Regel:** Documenteer uitzonderingen en beperk code-behind tot UI-specifieke operaties.

---

## Message pattern met tuple

### ShowStopsOnMapMessage

De message bevat meerdere waarden via een **tuple**:

```csharp
// Messages/ShowStopsOnMapMessage.cs
public class ShowStopsOnMapMessage : ValueChangedMessage<(List<TripStop> Stops, string Title)>
{
    public ShowStopsOnMapMessage(List<TripStop> stops, string title)
        : base((stops, title))
    {
    }
}
```

**Waarom tuple?**
- Meerdere gerelateerde waarden in één message
- Type-safe (geen losse properties)
- Destructuring in receiver:

```csharp
public void Receive(ShowStopsOnMapMessage message)
{
    var (stops, mapTitle) = message.Value;  // Tuple destructuring
    // ...
}
```

---

## Event bridge pattern

De ViewModel gebruikt een **event** om de View te notificeren:

```csharp
// MapViewModel.cs
public event Action? StopsUpdated;

public void Receive(ShowStopsOnMapMessage message)
{
    // Verwerk stops...
    StopsUpdated?.Invoke();  // Notificeer View
}

// MapPage.xaml.cs
private void OnStopsUpdated()
{
    var stops = _viewModel.Stops;
    UpdatePins(stops);  // UI-specifieke code
}
```

**Waarom niet PropertyChanged?**
- `Stops` is een `ObservableCollection` - triggert per item
- We willen één update na **alle** stops geladen zijn
- Event bridge geeft expliciete controle

---

## Coordinate projectie

GPS coördinaten (WGS84) moeten worden omgezet naar Web Mercator voor de kaart:

```csharp
// WGS84 (GPS) → Web Mercator (kaart)
var point = SphericalMercator.FromLonLat(stop.Longitude, stop.Latitude);
```

| Systeem | Gebruik | Eenheid |
|---------|---------|---------|
| WGS84 | GPS, Geolocation API | Graden (lat/lon) |
| Web Mercator | OpenStreetMap, Google Maps | Meters |

---

## ThemeStyle voor kleuren per trip

Mapsui's `ThemeStyle` bepaalt styling per feature:

```csharp
// Kleurenpalet (8 kleuren, herhalend)
private static readonly Color[] TripColors = new[]
{
    new Color(81, 43, 212),    // Paars
    new Color(233, 30, 99),    // Roze
    new Color(76, 175, 80),    // Groen
    new Color(255, 152, 0),    // Oranje
    new Color(33, 150, 243),   // Blauw
    new Color(156, 39, 176),   // Violet
    new Color(0, 188, 212),    // Cyan
    new Color(244, 67, 54),    // Rood
};

// Layer met ThemeStyle
_pinsLayer = new MemoryLayer
{
    Name = "Pins",
    Style = new ThemeStyle(CreatePinStyleFromFeature)  // Delegate!
};

// Per-feature styling functie
private IStyle CreatePinStyleFromFeature(IFeature feature)
{
    var colorIndex = 0;
    if (feature["TripId"] is int tripId &&
        _tripColorIndex.TryGetValue(tripId, out var idx))
    {
        colorIndex = idx;
    }

    var color = TripColors[colorIndex % TripColors.Length];  // Modulo voor herhaling

    return new SymbolStyle
    {
        SymbolScale = 1.2,
        Fill = new Brush(color),
        Outline = new Pen(Color.White, 3),
        SymbolType = SymbolType.Ellipse
    };
}
```

---

## Launch contexten

De MapPage kan vanuit 3 plekken worden geopend:

| Context | Wat wordt getoond | Titel |
|---------|-------------------|-------|
| TripsPage | Alle stops van alle trips | "All Stops" |
| TripDetailPage | Stops van één trip | "{TripName} Stops" |
| StopDetailPage | Eén stop | "{StopName}" |

```csharp
// Voorbeeld: TripDetailPage
private async Task ShowOnMap()
{
    await _navigationService.NavigateToMapPageAsync();

    var stops = TripStops.ToList();
    WeakReferenceMessenger.Default.Send(
        new ShowStopsOnMapMessage(stops, $"{Trip.Name} Stops"));
}
```

---

## NuGet dependencies

```xml
<!-- TripTracker.App.csproj -->
<PackageReference Include="Mapsui.Maui" Version="4.1.7" />
```

**MauiProgram.cs:**
```csharp
builder.UseMauiApp<App>()
       .UseSkiaSharp()  // Vereist voor Mapsui rendering
```

---

## Bestanden

### Nieuw gemaakt

| Bestand | Doel |
|---------|------|
| `Messages/ShowStopsOnMapMessage.cs` | Message met stops + titel |
| `ViewModels/IMapViewModel.cs` | Interface |
| `ViewModels/MapViewModel.cs` | Ontvangt message, beheert Stops |
| `Views/MapPage.xaml` | MapControl UI |
| `Views/MapPage.xaml.cs` | Mapsui setup (MVVM exception) |

### Gewijzigd

| Bestand | Wijziging |
|---------|-----------|
| `MauiProgram.cs` | UseSkiaSharp(), DI registratie |
| `Services/INavigationService.cs` | NavigateToMapPageAsync() |
| `ViewModels/TripsViewModel.cs` | ShowAllOnMapCommand |
| `ViewModels/TripDetailViewModel.cs` | ShowOnMapCommand |
| `ViewModels/StopDetailViewModel.cs` | ShowOnMapCommand |

---

## Cursus compliance

| Vereiste | Status |
|----------|--------|
| MVVM Architecture | ✅ Met gedocumenteerde exception |
| Messaging | ✅ ShowStopsOnMapMessage |
| NavigationService | ✅ Via DI |
| NuGet packages | ✅ Mapsui.Maui |

---

## Examenvragen

### Vraag 1: MVVM exception

**Vraag:** Waarom heeft MapPage code in de code-behind, en is dit een MVVM violation?

**Antwoord:**
MapPage.xaml.cs bevat Mapsui setup code (layers, features, styling). Dit is een **gedocumenteerde uitzondering** omdat:

1. Mapsui MapControl vereist **programmatische initialisatie**
2. Layer management is **UI-specifiek**, niet business logic
3. De ViewModel bevat nog steeds de **data** (Stops collectie)
4. Code-behind bevat alleen **rendering** logica

**Regel:** MVVM exceptions zijn acceptabel als:
- Ze zijn **gedocumenteerd**
- Business logic blijft in ViewModel
- Het alternatief is onredelijk complex

---

### Vraag 2: event bridge pattern

**Vraag:** Waarom gebruikt MapViewModel een `event Action` in plaats van PropertyChanged?

**Antwoord:**
```csharp
public event Action? StopsUpdated;

public void Receive(ShowStopsOnMapMessage message)
{
    foreach (var stop in stops) Stops.Add(stop);
    StopsUpdated?.Invoke();  // Eén event na alle items
}
```

**Redenen:**
1. `ObservableCollection` triggert **CollectionChanged per item**
2. We willen **één update** na alle stops geladen
3. `PropertyChanged` zou niet triggeren (collectie referentie verandert niet)
4. Explicit event geeft **controle over timing**

---

### Vraag 3: tuple in message

**Vraag:** Hoe stuur je meerdere waarden in één message?

**Antwoord:**
Gebruik een **tuple** als message value:

```csharp
// Message definitie
public class ShowStopsOnMapMessage :
    ValueChangedMessage<(List<TripStop> Stops, string Title)>
{
    public ShowStopsOnMapMessage(List<TripStop> stops, string title)
        : base((stops, title)) { }
}

// Versturen
WeakReferenceMessenger.Default.Send(
    new ShowStopsOnMapMessage(stops, "My Title"));

// Ontvangen met destructuring
var (stops, mapTitle) = message.Value;
```

---

### Vraag 4: coordinate projectie

**Vraag:** Waarom moet je `SphericalMercator.FromLonLat()` gebruiken voor kaartpunten?

**Antwoord:**
GPS coördinaten (WGS84) gebruiken graden, maar kaarten (OpenStreetMap) gebruiken meters (Web Mercator).

```csharp
// Conversie GPS → Kaart
var point = SphericalMercator.FromLonLat(stop.Longitude, stop.Latitude);
```

| Input | Output |
|-------|--------|
| Longitude: 4.8325 | X: 537847 meters |
| Latitude: 51.1653 | Y: 6643724 meters |

Zonder conversie zou de pin op de verkeerde locatie verschijnen.

---

### Vraag 5: ThemeStyle delegate

**Vraag:** Hoe geef je elke trip een andere kleur op de kaart?

**Antwoord:**
Gebruik Mapsui's `ThemeStyle` met een delegate:

```csharp
// Layer met styling delegate
_pinsLayer = new MemoryLayer
{
    Style = new ThemeStyle(CreatePinStyleFromFeature)
};

// Delegate die per feature wordt aangeroepen
private IStyle CreatePinStyleFromFeature(IFeature feature)
{
    var tripId = (int)feature["TripId"];
    var colorIndex = _tripColorIndex[tripId];
    var color = TripColors[colorIndex % TripColors.Length];

    return new SymbolStyle { Fill = new Brush(color) };
}
```

**Hoe het werkt:**
1. Elke feature slaat TripId op: `feature["TripId"] = stop.TripId`
2. ThemeStyle roept delegate aan per feature
3. Delegate kiest kleur op basis van TripId
4. Modulo (`%`) zorgt voor herhaling bij >8 trips

---

### Vraag 6: modulo voor kleur herhaling

**Vraag:** Wat doet `colorIndex % TripColors.Length` en waarom is het nodig?

**Antwoord:**
```csharp
var color = TripColors[colorIndex % TripColors.Length];
```

**Wat het doet:**
- `%` (modulo) geeft de rest na deling
- Als `colorIndex = 10` en `Length = 8`: `10 % 8 = 2`
- Dit "wrapt" de index rond naar het begin van de array

**Waarom nodig:**
- We hebben 8 kleuren maar mogelijk meer trips
- Zonder modulo → `IndexOutOfRangeException` bij trip 9+
- Met modulo → kleuren herhalen (trip 9 krijgt kleur 1)

---

## Samenvatting

- **Mapsui** voor OpenStreetMap integratie (geen API key)
- **MVVM exception** voor UI-specifieke kaartcode (gedocumenteerd)
- **Event bridge** voor één update na alle data geladen
- **Tuple in message** voor meerdere waarden
- **SphericalMercator** voor GPS → kaart coordinaten
- **ThemeStyle** met delegate voor per-feature styling
- **Modulo** voor kleur herhaling

---

## Referenties

- **Mapsui**: [mapsui.com](https://mapsui.com)
- **OpenStreetMap**: [openstreetmap.org](https://www.openstreetmap.org)
- **Web Mercator**: [Wikipedia](https://en.wikipedia.org/wiki/Web_Mercator_projection)
- **Cursus**: Les 3 - Device Features (Geolocation)
