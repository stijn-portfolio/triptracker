---
fase: 09 - UI Polish & Bug Fixes
status: Voltooid
tags:
  - triptracker
  - ui
  - debugging
  - binding
  - stringformat
  - examen
created: 2025-12-20
---

# Fase 9: UI polish & bug fixes

## Overzicht

Deze fase behandelt **UI debugging** en **polish** - het vinden en oplossen van veelvoorkomende MAUI problemen. Dit zijn vaardigheden die je nodig hebt bij elk project.

> [!info] Cursus Referentie
> UI debugging en polish zijn essentiële skills die niet expliciet in een les worden behandeld, maar die je bij elk project tegenkomt. Deze fase documenteert common patterns en solutions.

---

## 1. Button command niet werkend

### Probleem

De + button op TripsPage deed niets wanneer erop getikt werd.

### Diagnose

```csharp
// TripsViewModel.cs - VOOR
private async Task AddNewTrip()
{
    // TODO: Implement add trip logic
    await Task.CompletedTask;
}
```

De method was leeg (TODO comment). Het command was wel gebonden, maar deed niets.

### Oplossing

Implementeer de method met een prompt dialoog:

```csharp
// TripsViewModel.cs - NA
private async Task AddNewTrip()
{
    string? tripName = await Application.Current!.MainPage!.DisplayPromptAsync(
        "New Trip",
        "Enter trip name:",
        "Create",
        "Cancel",
        placeholder: "My Amazing Trip");

    if (!string.IsNullOrWhiteSpace(tripName))
    {
        var newTrip = new Trip
        {
            Name = tripName,
            Description = "A new adventure awaits!",
            StartDate = DateTime.Now
        };

        var tripService = new TripDataService();
        await tripService.PostAsync(newTrip);
        await LoadTripsAsync();  // Refresh lijst
    }
}
```

> [!tip] Debug Tip: Command Werkt Niet
> **Checklist:**
> 1. Is de method geimplementeerd (geen TODO)?
> 2. Is het command gebonden in BindCommands()?
> 3. Is CanExecute true (als gebruikt)?
> 4. Is BindingContext correct gezet?

---

## 2. Property binding toont niets

### Probleem

Latitude en Longitude waarden waren leeg tijdens het ophalen van GPS locatie.

### Diagnose

De properties waren 0.0 tijdens het ophalen, wat als lege string werd getoond.

### Oplossing: computed display property

```csharp
// AddStopViewModel.cs
// Backing fields
private double latitude;
public double Latitude
{
    get => latitude;
    set
    {
        if (SetProperty(ref latitude, value))
        {
            OnPropertyChanged(nameof(LatitudeDisplay));  // Update display
        }
    }
}

// Computed property voor UI
public string LatitudeDisplay => Latitude != 0
    ? Latitude.ToString("F6")
    : "Fetching...";
```

**Waarom dit werkt:**
1. `LatitudeDisplay` is een **computed property** (alleen getter)
2. Wanneer `Latitude` wijzigt, roepen we `OnPropertyChanged(nameof(LatitudeDisplay))` aan
3. UI bindt aan `LatitudeDisplay`, niet aan `Latitude`
4. User ziet "Fetching..." tot echte waarde beschikbaar is

> [!warning] Common Mistake: Computed Properties
> Computed properties hebben geen setter, dus `SetProperty()` kan niet. Je MOET handmatig `OnPropertyChanged()` aanroepen vanuit de backing property!

---

## 3. Editor tekst afgeknipt

### Probleem

AI-gegenereerde beschrijvingen pasten niet in de Editor - tekst was afgeknipt.

### Diagnose

```xml
<!-- VOOR -->
<Editor Text="{Binding Description}"
        HeightRequest="80"/>  <!-- Te klein! -->
```

### Oplossing: AutoSize

```xml
<!-- NA -->
<Editor Text="{Binding Description}"
        HeightRequest="150"
        AutoSize="TextChanges"/>
```

**AutoSize opties:**
| Waarde | Gedrag |
|--------|--------|
| `Disabled` | Vaste hoogte (default) |
| `TextChanges` | Groeit mee met tekst |

> [!tip] Best Practice: Editor vs Entry
> - **Entry**: Enkele regel input (naam, email)
> - **Editor**: Meerdere regels (beschrijving, notities)
>
> Gebruik altijd `AutoSize="TextChanges"` voor Editor als de inhoud kan varieren.

---

## 4. Datum format problemen

### Probleem

Datum toonde "21 December 2024 ap 14:30" in plaats van "21 December 2024 - 14:30".

### Diagnose

```xml
<!-- VOOR -->
<Label Text="{Binding DateTime, StringFormat='{0:dd MMMM yyyy at HH:mm}'}"/>
```

De letter `t` in "at" werd geinterpreteerd als AM/PM format code!

### Oplossing

```xml
<!-- NA: Gebruik dash separator -->
<Label Text="{Binding DateTime, StringFormat='{0:dd MMMM yyyy} - {0:HH:mm}'}"/>
```

**StringFormat escape regels:**
| Karakter | Betekenis | Escape nodig? |
|----------|-----------|---------------|
| `d` | dag | Ja, in datum context |
| `M` | maand | Ja |
| `y` | jaar | Ja |
| `H` | uur (24h) | Ja |
| `m` | minuut | Ja |
| `t` | AM/PM | **JA - pas op!** |
| `-` | literal dash | Nee |

> [!warning] StringFormat Valkuil
> Letters in StringFormat kunnen format codes zijn! Vermijd woorden als "at", "to", "am" in datum formats. Gebruik symbolen (-) of splits de format string.

---

## 5. Dubbele titels

### Probleem

Pagina's toonden de titel twee keer - in de navigation bar EN in de content.

### Diagnose

```xml
<!-- TripsPage.xaml -->
<ContentPage Title="My Trips">
    <StackLayout>
        <Label Text="My Trips" FontSize="24"/>  <!-- Dubbel! -->
        ...
    </StackLayout>
</ContentPage>
```

### Oplossing

Verwijder de body titel - de `Title` property van ContentPage is voldoende.

```xml
<!-- NA -->
<ContentPage Title="My Trips">
    <StackLayout>
        <!-- Geen extra titel label -->
        <CollectionView .../>
    </StackLayout>
</ContentPage>
```

**Consistentie regel:**
| Pagina Type | Titel Bron |
|-------------|------------|
| Statische titel | `Title="My Trips"` |
| Dynamische titel | `Title="{Binding Trip.Name}"` |

---

## 6. Inconsistente kleuren

### Probleem

De "Analyze Photo with AI" button was groen, terwijl alle andere buttons paars waren.

### Oplossing

```xml
<!-- VOOR -->
<Button Text="Analyze Photo with AI"
        BackgroundColor="#2E7D32"/>  <!-- Groen -->

<!-- NA -->
<Button Text="Analyze Photo with AI"
        BackgroundColor="#512BD4"/>  <!-- Paars - consistent -->
```

> [!tip] Best Practice: Kleur Consistentie
> Definieer kleuren in `Resources/Styles/Colors.xaml`:
> ```xml
> <Color x:Key="Primary">#512BD4</Color>
> ```
> Gebruik dan:
> ```xml
> <Button BackgroundColor="{StaticResource Primary}"/>
> ```

---

## Architectuur: debugging workflow

```
┌─────────────────────────────────────────────────────────────┐
│                    UI Bug Debugging Flow                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. IDENTIFICEER het probleem                               │
│     └─> Screenshot/beschrijving                             │
│                                                              │
│  2. LOCALISEER de code                                      │
│     └─> XAML binding? ViewModel property? Service?          │
│                                                              │
│  3. DIAGNOSE                                                │
│     ├─> Debug.WriteLine() voor waarden                      │
│     ├─> Breakpoints in ViewModel                            │
│     └─> XAML Hot Reload voor UI changes                     │
│                                                              │
│  4. FIX                                                     │
│     └─> Pas code aan, test, herhaal                         │
│                                                              │
│  5. DOCUMENTEER                                             │
│     └─> Wat was het probleem? Hoe opgelost?                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Bestanden gewijzigd

| Bestand | Wijziging |
|---------|-----------|
| `ViewModels/TripsViewModel.cs` | AddNewTrip() geimplementeerd |
| `ViewModels/AddStopViewModel.cs` | LatitudeDisplay/LongitudeDisplay |
| `Views/TripsPage.xaml` | Dubbele titel verwijderd |
| `Views/TripDetailPage.xaml` | Dubbele titel verwijderd |
| `Views/StopDetailPage.xaml` | Datum format gefixd |
| `Views/AddStopPage.xaml` | Editor height, button kleur |

---

## Examenvragen

### Vraag 1: computed properties

**Vraag:** Hoe maak je een property die "Fetching..." toont als de waarde nog 0 is, en anders de waarde zelf?

**Antwoord:**
```csharp
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

public string LatitudeDisplay => Latitude != 0
    ? Latitude.ToString("F6")
    : "Fetching...";
```

**Belangrijk:** De computed property (`LatitudeDisplay`) heeft geen setter. Je MOET `OnPropertyChanged()` aanroepen vanuit de backing property (`Latitude`).

---

### Vraag 2: StringFormat escaping

**Vraag:** Waarom toont `StringFormat='{0:dd MMMM yyyy at HH:mm}'` "ap" in plaats van "at"?

**Antwoord:** De letter `t` is een format code voor AM/PM marker. In de context van een datum format wordt `t` geinterpreteerd als "abbreviated AM/PM" (a of p).

**Oplossing:** Vermijd letters die format codes kunnen zijn. Gebruik:
- `StringFormat='{0:dd MMMM yyyy} - {0:HH:mm}'` (dash separator)
- Of splits in twee aparte labels

---

### Vraag 3: editor AutoSize

**Vraag:** Wat is het verschil tussen `AutoSize="Disabled"` en `AutoSize="TextChanges"`?

**Antwoord:**
| AutoSize | Gedrag |
|----------|--------|
| `Disabled` | Editor behoudt vaste `HeightRequest` |
| `TextChanges` | Editor groeit/krimpt met de tekst |

Gebruik `TextChanges` voor beschrijvingen of notities waar de lengte kan varieren.

---

### Vraag 4: command debugging

**Vraag:** Een button command werkt niet. Welke 4 dingen controleer je?

**Antwoord:**
1. **Method geimplementeerd?** Geen TODO of lege body
2. **Command gebonden?** In `BindCommands()`: `SaveCommand = new AsyncRelayCommand(Save)`
3. **CanExecute?** Als je `CanExecute` delegate gebruikt, is die true?
4. **BindingContext?** Is de ViewModel correct gekoppeld aan de Page?

---

### Vraag 5: DisplayPromptAsync

**Vraag:** Hoe vraag je de gebruiker om tekst input met een popup?

**Antwoord:**
```csharp
string? result = await Application.Current!.MainPage!.DisplayPromptAsync(
    "Titel",           // Popup titel
    "Bericht",         // Instructie
    "OK",              // Accept button text
    "Cancel",          // Cancel button text
    placeholder: "Hint text"
);

if (!string.IsNullOrWhiteSpace(result))
{
    // User heeft iets ingevuld
}
```

**Let op:** `result` is `null` als gebruiker Cancel drukt.

---

## Samenvatting

| Bug Type | Diagnose | Oplossing |
|----------|----------|-----------|
| Command doet niets | Lege method | Implementeer logica |
| Property toont niets | Geen waarde | Computed property met fallback |
| Tekst afgeknipt | Vaste hoogte | `AutoSize="TextChanges"` |
| Datum fout | Format code | Vermijd letters, gebruik symbolen |
| Dubbele titel | Body + header | Verwijder body titel |
| Inconsistente kleur | Hardcoded | Gebruik `StaticResource` |

---

## Referenties

- **StringFormat codes**: [docs.microsoft.com - Custom date format](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)
- **Editor control**: [docs.microsoft.com - MAUI Editor](https://docs.microsoft.com/en-us/dotnet/maui/user-interface/controls/editor)
- **DisplayPromptAsync**: [docs.microsoft.com - Display prompts](https://docs.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups)
