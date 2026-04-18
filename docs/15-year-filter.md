---
fase: 15
status: Voltooid
tags:
  - filtering
  - bindablelayout
  - datatrigger
  - performance
  - linq
created: 2025-12-20
---

# Fase 15: year filter tabs

## Overzicht

In deze fase hebben we **horizontale jaar-filter tabs** geïmplementeerd op TripsPage. Dit demonstreert dynamische UI generatie, LINQ filtering, en performance optimalisatie.

> [!info] Cursus Referentie
> Deze fase combineert BindableLayout (dynamische UI), DataTriggers (visuele feedback), en LINQ queries - allemaal essentiële MAUI/MVVM concepts.

---

## Feature preview

```
My Trips                      [Map] [+]
┌──────────────────────────────────────┐
│  All  │ 2025 │ 2024 │ 2023 │   →     │
├──────────────────────────────────────┤
│  [gefilterde trips van dat jaar]     │
└──────────────────────────────────────┘
```

| Element | Gedrag |
|---------|--------|
| **All** | Toon alle trips |
| **Jaar buttons** | Dynamisch gegenereerd uit trip data |
| **Default** | Huidig jaar geselecteerd |
| **Visuele feedback** | Geselecteerd = paars, anders = grijs |

---

## Simpele uitleg eerst

### Wat doet year filter?

Filter-knoppen bovenaan de trips pagina:

```
┌──────────────────────────────────────┐
│  [All]  [2025]  [2024]  [2023]       │  ← Klik op een jaar
├──────────────────────────────────────┤
│  Trip 1 - Parijs                     │  ← Alleen trips van dat jaar
│  Trip 2 - Rome                       │
└──────────────────────────────────────┘
```

### De 3 onderdelen

```
1. YearFilterItem      →  Eén knop (jaar + is geselecteerd?)
2. YearFilters         →  Lijst van alle knoppen
3. FilteredTrips       →  De gefilterde trips die je ziet
```

### Waar komt YearFilters vandaan?

```
XAML                          ViewModel
────                          ─────────
{Binding YearFilters}    →    TripsViewModel.YearFilters
        ↑
        │
        └── Via BindingContext
```

De keten:
```
XAML: "Ik zoek 'YearFilters'"
  │
  ▼
BindingContext = TripsViewModel
  │
  ▼
TripsViewModel.YearFilters ← GEVONDEN!
  │
  ▼
ObservableCollection met [All, 2025, 2024]
```

### Hoe wordt YearFilters gevuld? (App start)

```
App start
    │
    ▼
Constructor
    │
    ▼
LoadTripsAsync()          ← Haal trips van API
    │
    ▼
UpdateAvailableYears()    ← HIER worden YearFilters gevuld!
    │
    ├── 1. LINQ haalt unieke jaren uit Trips
    ├── 2. YearFilters.Add() voor elk jaar
    └── 3. SelectedYear = huidig jaar
```

### Wat gebeurt bij knop klik?

```
Gebruiker klikt [2024]
        │
        ▼
SelectYearCommand
        │
        ▼
SelectedYear = 2024
        │
        ├──► ApplyYearFilter()
        │       └── FilteredTrips = alleen 2024 trips
        │
        └──► UpdateYearFilterSelection()
                └── [2024].IsSelected = true (paars)
                    [2025].IsSelected = false (grijs)
```

### BindableLayout - simpel uitgelegd

```xml
<StackLayout BindableLayout.ItemsSource="{Binding YearFilters}">
```

**Betekent:** "Maak automatisch een UI element voor elk item in YearFilters"

```
YearFilters collectie:              StackLayout resultaat:
┌─────────────────────┐             ┌─────────────────────────────┐
│ { Year: null }      │ ──────────► │ [All]                       │
│ { Year: 2025 }      │ ──────────► │ [2025]                      │
│ { Year: 2024 }      │ ──────────► │ [2024]                      │
└─────────────────────┘             └─────────────────────────────┘

3 items in collectie      =         3 buttons in StackLayout
```

---

## LINQ basics

### Wat is LINQ?

**LINQ** = **L**anguage **In**tegrated **Q**uery

**In gewone taal:** "Een manier om data te filteren, sorteren en bewerken met simpele commando's"

### Zonder LINQ vs met LINQ

```csharp
// ZONDER LINQ (veel code)
var jaren = new List<int>();
foreach (var trip in Trips)
{
    if (!jaren.Contains(trip.StartDate.Year))
        jaren.Add(trip.StartDate.Year);
}
jaren.Sort();
jaren.Reverse();

// MET LINQ (kort en leesbaar)
var jaren = Trips
    .Select(t => t.StartDate.Year)
    .Distinct()
    .OrderByDescending(y => y)
    .ToList();
```

### De belangrijkste LINQ methods

| Method | Wat doet het | Voorbeeld |
|--------|--------------|-----------|
| `Select` | Transformeer elk item | `.Select(t => t.Name)` → alleen namen |
| `Where` | Filter items | `.Where(t => t.Year == 2024)` → alleen 2024 |
| `Distinct` | Verwijder dubbelen | `[2025, 2025, 2024]` → `[2025, 2024]` |
| `OrderBy` | Sorteer oplopend | `[3, 1, 2]` → `[1, 2, 3]` |
| `OrderByDescending` | Sorteer aflopend | `[1, 2, 3]` → `[3, 2, 1]` |
| `ToList` | Maak er een lijst van | Resultaat → `List<T>` |

### Visueel voorbeeld

```
Trips:
┌─────────────────────────────┐
│ { Name: "Parijs",  2025 }   │
│ { Name: "Rome",    2025 }   │
│ { Name: "Berlijn", 2024 }   │
└─────────────────────────────┘
        │
        ▼ .Select(t => t.StartDate.Year)
┌─────────────────────────────┐
│ 2025, 2025, 2024            │
└─────────────────────────────┘
        │
        ▼ .Distinct()
┌─────────────────────────────┐
│ 2025, 2024                  │
└─────────────────────────────┘
        │
        ▼ .OrderByDescending(y => y)
┌─────────────────────────────┐
│ 2025, 2024                  │
└─────────────────────────────┘
```

### De Pijl `=>` (Lambda)

```csharp
.Select(t => t.StartDate.Year)
        │         │
        │         └── Wat je wilt hebben
        └── "t" is één trip uit de lijst
```

**Lees als:** "Voor elke trip `t`, geef mij `t.StartDate.Year`"

---

## MVVM architectuur

```
┌─────────────────────────────────────────────────────────────┐
│                 Year Filter Architectuur                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  TripsPage.xaml                                              │
│  ┌─────────────────────────────────────────────────┐        │
│  │ <StackLayout BindableLayout.ItemsSource=        │        │
│  │              "{Binding YearFilters}">           │        │
│  │   ┌────────────────────────────────────────┐   │        │
│  │   │ Button per YearFilterItem              │   │        │
│  │   │   ├── Text="{Binding DisplayText}"     │   │        │
│  │   │   ├── Command → SelectYearCommand      │   │        │
│  │   │   └── DataTrigger voor IsSelected      │   │        │
│  │   └────────────────────────────────────────┘   │        │
│  └─────────────────────────────────────────────────┘        │
│                          │                                   │
│                          │ Binding                           │
│                          ▼                                   │
│  TripsViewModel.cs                                          │
│  ┌─────────────────────────────────────────────────┐        │
│  │ ObservableCollection<YearFilterItem> YearFilters│        │
│  │ int? SelectedYear                               │        │
│  │ ObservableCollection<Trip> FilteredTrips       │        │
│  │                                                 │        │
│  │ SelectYearCommand                               │        │
│  │   └── ApplyYearFilter()                        │        │
│  │          └── LINQ Where() op Trips             │        │
│  └─────────────────────────────────────────────────┘        │
│                          │                                   │
│                          │                                   │
│  YearFilterItem.cs (Model)                                  │
│  ┌─────────────────────────────────────────────────┐        │
│  │ int? Year                                       │        │
│  │ string DisplayText => Year?.ToString() ?? "All" │        │
│  │ bool IsSelected (observable)                    │        │
│  └─────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

---

## YearFilterItem model

```csharp
// Models/YearFilterItem.cs
public class YearFilterItem : ObservableObject
{
    public int? Year { get; set; }

    // Computed property: null → "All", anders het jaar
    public string DisplayText => Year?.ToString() ?? "All";

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
```

**Waarom een aparte class?**
- Simpele `int` kan geen `IsSelected` property hebben
- `ObservableObject` nodig voor property change notification
- `DisplayText` computed property voor null handling

---

## ViewModel filter logica

### Jaar lijst genereren

```csharp
// TripsViewModel.cs
private void UpdateAvailableYears()
{
    // 1. Haal unieke jaren uit trips
    var years = Trips
        .Select(t => t.StartDate.Year)
        .Distinct()
        .OrderByDescending(y => y)
        .ToList();

    // 2. Maak filter items: "All" + jaren
    var items = new List<YearFilterItem>
    {
        new YearFilterItem { Year = null, IsSelected = false }  // "All" button
    };
    items.AddRange(years.Select(y => new YearFilterItem { Year = y }));

    // 3. Vul collectie via Clear + Add (performance)
    YearFilters.Clear();
    foreach (var item in items)
    {
        YearFilters.Add(item);
    }

    // 4. Default: huidig jaar (als trips van dit jaar bestaan)
    var currentYear = DateTime.Now.Year;
    SelectedYear = years.Contains(currentYear) ? currentYear : null;
}
```

### Filter toepassen

```csharp
private void ApplyYearFilter()
{
    // 1. Bepaal welke trips te tonen
    var tripsToShow = SelectedYear == null
        ? Trips
        : Trips.Where(t => t.StartDate.Year == SelectedYear);

    // 2. Update FilteredTrips via Clear + Add
    FilteredTrips.Clear();
    foreach (var trip in tripsToShow)
    {
        FilteredTrips.Add(trip);
    }

    // 3. Update IsSelected op alle filter items
    foreach (var item in YearFilters)
    {
        var shouldBeSelected = item.Year == SelectedYear;
        if (item.IsSelected != shouldBeSelected)
        {
            item.IsSelected = shouldBeSelected;
        }
    }
}
```

---

## BindableLayout XAML

```xml
<!-- Views/TripsPage.xaml -->
<ScrollView Orientation="Horizontal"
            HorizontalScrollBarVisibility="Never">
    <StackLayout BindableLayout.ItemsSource="{Binding YearFilters}"
                 Orientation="Horizontal"
                 Spacing="8"
                 Padding="15,10">
        <BindableLayout.ItemTemplate>
            <DataTemplate x:DataType="models:YearFilterItem">
                <Button Text="{Binding DisplayText}"
                        Command="{Binding Source={RelativeSource
                            AncestorType={x:Type viewmodels:TripsViewModel}},
                            Path=SelectYearCommand}"
                        CommandParameter="{Binding}"
                        Padding="15,8"
                        CornerRadius="20"
                        BackgroundColor="#E0E0E0"
                        TextColor="Black"
                        FontSize="14">
                    <Button.Triggers>
                        <DataTrigger TargetType="Button"
                                     Binding="{Binding IsSelected}"
                                     Value="True">
                            <Setter Property="BackgroundColor" Value="#512BD4"/>
                            <Setter Property="TextColor" Value="White"/>
                        </DataTrigger>
                    </Button.Triggers>
                </Button>
            </DataTemplate>
        </BindableLayout.ItemTemplate>
    </StackLayout>
</ScrollView>
```

**BindableLayout uitleg:**
- `ItemsSource` bindt aan collectie (zoals CollectionView)
- Maar genereert items in een **layout** (StackLayout, Grid, etc.)
- Ideaal voor kleine, vaste collecties (filter tabs, chips)

---

## Performance optimalisatie

### Probleem: frame drops

Bij wisselen van filter verscheen in Android logs:
```
[Choreographer] Skipped 149 frames! The application may be doing too much work on its main thread.
```

### Oorzaak: nieuwe collectie = full UI rebuild

```csharp
// SLECHT: Nieuwe collectie aanmaken
FilteredTrips = new ObservableCollection<Trip>(filtered);
```

Dit dwingt MAUI om de **hele CollectionView** opnieuw te renderen.

### Oplossing: clear + add

```csharp
// GOED: Bestaande collectie hergebruiken
FilteredTrips.Clear();
foreach (var trip in tripsToShow)
{
    FilteredTrips.Add(trip);
}
```

**Waarom beter?**
- MAUI kan individuele `CollectionChanged` events batchen
- Geen nieuwe UI elementen aanmaken
- Virtualisatie blijft behouden

### Extra: onnodige PropertyChanged voorkomen

```csharp
// Check of waarde daadwerkelijk verandert
var shouldBeSelected = item.Year == SelectedYear;
if (item.IsSelected != shouldBeSelected)
{
    item.IsSelected = shouldBeSelected;
}
```

Zonder check → elke filter switch triggert **alle** button updates.

---

## Bestanden

### Nieuw

| Bestand | Doel |
|---------|------|
| `Models/YearFilterItem.cs` | Wrapper met Year, DisplayText, IsSelected |

### Gewijzigd

| Bestand | Wijziging |
|---------|-----------|
| `ViewModels/TripsViewModel.cs` | YearFilters, SelectedYear, filter logica |
| `ViewModels/ITripsViewModel.cs` | Interface uitbreiding |
| `Views/TripsPage.xaml` | BindableLayout met filter tabs |

---

## Cursus compliance

| Vereiste | Status |
|----------|--------|
| MVVM Architecture | ✅ Filter logica in ViewModel |
| Data Binding | ✅ BindableLayout, DataTriggers |
| ObservableCollection | ✅ YearFilters, FilteredTrips |
| LINQ | ✅ Distinct, Where, OrderBy |

---

## Examenvragen

### Vraag 1: BindableLayout vs CollectionView

**Vraag:** Wanneer gebruik je BindableLayout in plaats van CollectionView?

**Antwoord:**
| Control | Gebruik |
|---------|---------|
| **CollectionView** | Grote/dynamische lijsten, scrolling, virtualisatie |
| **BindableLayout** | Kleine vaste collecties, filter tabs, chips, tags |

```xml
<!-- BindableLayout: Klein, geen virtualisatie nodig -->
<StackLayout BindableLayout.ItemsSource="{Binding YearFilters}">
    <BindableLayout.ItemTemplate>...</BindableLayout.ItemTemplate>
</StackLayout>

<!-- CollectionView: Groot, virtualisatie nodig -->
<CollectionView ItemsSource="{Binding Trips}">
    <CollectionView.ItemTemplate>...</CollectionView.ItemTemplate>
</CollectionView>
```

**BindableLayout voordelen:**
- Werkt in elk Layout type (StackLayout, Grid, FlexLayout)
- Minder overhead dan CollectionView
- Beter voor horizontale tabs

---

### Vraag 2: LINQ distinct en OrderBy

**Vraag:** Hoe haal je een gesorteerde lijst van unieke jaren uit een collectie trips?

**Antwoord:**
```csharp
var years = Trips
    .Select(t => t.StartDate.Year)    // Projecteer naar jaren
    .Distinct()                        // Verwijder duplicaten
    .OrderByDescending(y => y)         // Nieuwste eerst
    .ToList();                         // Materialiseer
```

**LINQ volgorde is belangrijk:**
1. `Select` → transformeer data
2. `Distinct` → dedup
3. `OrderBy` → sorteer
4. `ToList` → materialiseer (anders lazy evaluation)

---

### Vraag 3: performance - clear vs new collection

**Vraag:** Waarom is `collection.Clear() + Add()` sneller dan `collection = new ObservableCollection<T>()`?

**Antwoord:**
| Aanpak | Wat er gebeurt | Performance |
|--------|----------------|-------------|
| **New collection** | UI maakt alle elementen opnieuw | Langzaam |
| **Clear + Add** | UI update individuele items | Snel |

```csharp
// SLECHT: Hele CollectionView herbouwen
FilteredTrips = new ObservableCollection<Trip>(data);

// GOED: Incrementele updates
FilteredTrips.Clear();
foreach (var item in data) FilteredTrips.Add(item);
```

Met `Clear + Add` kan MAUI:
- CollectionChanged events batchen
- Bestaande UI elementen recyclen
- Virtualisatie behouden

---

### Vraag 4: nullable int in filter

**Vraag:** Waarom is `SelectedYear` een `int?` (nullable) in plaats van `int`?

**Antwoord:**
```csharp
public int? SelectedYear { get; set; }

private void ApplyYearFilter()
{
    if (SelectedYear == null)
        // Toon ALLE trips ("All" button)
    else
        // Filter op specifiek jaar
}
```

**Null representeert "All":**
- `null` = geen filter = alle trips
- `2024` = filter op 2024

Dit is eleganter dan een magic number (bv. `SelectedYear = -1`).

---

### Vraag 5: DataTrigger voor button state

**Vraag:** Hoe maak je een button paars wanneer `IsSelected == true`?

**Antwoord:**
```xml
<Button BackgroundColor="#E0E0E0" TextColor="Black">
    <Button.Triggers>
        <DataTrigger TargetType="Button"
                     Binding="{Binding IsSelected}"
                     Value="True">
            <Setter Property="BackgroundColor" Value="#512BD4"/>
            <Setter Property="TextColor" Value="White"/>
        </DataTrigger>
    </Button.Triggers>
</Button>
```

**Hoe het werkt:**
1. Default: grijs (`#E0E0E0`) met zwarte tekst
2. Als `IsSelected == true` → DataTrigger activeert
3. Setters overschrijven BackgroundColor en TextColor
4. Als `IsSelected == false` → terug naar defaults

---

### Vraag 6: computed property DisplayText

**Vraag:** Wat is het voordeel van `DisplayText => Year?.ToString() ?? "All"`?

**Antwoord:**
```csharp
public int? Year { get; set; }

// Computed property met null-coalescing
public string DisplayText => Year?.ToString() ?? "All";
```

**Voordelen:**
1. **Null handling**: `Year = null` → "All", anders het jaar
2. **DRY**: Logica op één plek, niet in XAML
3. **Type safety**: XAML bindt aan string, geen null check nodig

**Operators uitleg:**
- `?.` (null-conditional): roep `ToString()` alleen aan als `Year` niet null is
- `??` (null-coalescing): als resultaat null is, gebruik "All"

---

## Samenvatting

- **BindableLayout** voor kleine, dynamische UI collecties (tabs, chips)
- **LINQ pipeline** voor data transformatie (Select → Distinct → OrderBy)
- **Clear + Add** voor performance (geen nieuwe collectie)
- **Nullable int** om "All" te representeren
- **DataTrigger** voor visuele feedback op state
- **Computed properties** voor null handling

---

## Referenties

- **BindableLayout**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/layouts/bindablelayout)
- **DataTrigger**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/triggers)
- **LINQ**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/)
- **Cursus**: Les 2 - MAUI & MVVM (Data Binding)
