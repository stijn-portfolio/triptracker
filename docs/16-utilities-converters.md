---
fase: 16
status: Voltooid
tags:
  - utilities
  - converters
  - image-processing
  - ivalueconverter
created: 2025-12-25
---

# Fase 16: Utilities & Converters

## Overzicht

In deze fase documenteren we de **helper classes** en **XAML Value Converters** die door de app worden gebruikt. Deze zijn essentieel voor image processing en UI state transformaties.

> [!info] Cursus Referentie
> Value Converters zijn onderdeel van XAML data binding. Ze transformeren data tussen ViewModel en View - een belangrijk MVVM concept.

---

## Simpele Uitleg Eerst

### PhotoService - Wat doet het?

**Eén service die ALLES doet voor foto's:**

```
┌─────────────────────────────────────────────────┐
│              PhotoService                        │
├─────────────────────────────────────────────────┤
│  1. Foto maken (camera)                         │
│  2. Foto kiezen (gallery)                       │
│  3. Foto verkleinen als te groot                │
│  4. Retry als Android klaagt                    │
└─────────────────────────────────────────────────┘
                    │
                    ▼
              byte[] foto data
              (klaar voor API!)
```

### Waarom resizing?

```
Telefoon foto:     10MB
                    │
                    ▼ Na resizing
API ontvangt:       Max 4MB

Waarom?
- API's hebben limiet (vaak 4MB)
- Base64 maakt het 33% groter (10MB → 13MB!)
- Snellere uploads
- Server niet overbelasten
```

### Android Retry Probleem

```
Android probleem:
Camera slaat foto op → Bestand nog "locked" → App crashed!

Oplossing in PhotoService:
1. Wacht 100ms na camera
2. Als het misgaat → wacht 200ms → probeer opnieuw
```

### Gebruik in ViewModel (simpel!)

```csharp
// Vroeger: veel code in elke ViewModel
var photo = await MediaPicker.Default.CapturePhotoAsync();
await Task.Delay(100);
try { /* resize */ } catch { /* retry */ }

// Nu: één regel
var bytes = await _photoService.CapturePhotoAsync();
// Klaar! Alle complexiteit zit in de service.
```

---

### IValueConverter - Wat is het?

**Een "vertaler" tussen ViewModel en XAML**

```
ViewModel                    Converter                    XAML
─────────                    ─────────                    ────
HasPhoto = true     →     InvertedBool     →     IsVisible = false
                          (keert om)
```

**In gewone taal:** "De data is `true`, maar ik wil het element NIET zichtbaar"

---

### InvertedBoolConverter - Simpel

**Probleem:** Je hebt `HasPhoto`, maar wilt placeholder tonen als GEEN foto.

```csharp
public object? Convert(object? value, ...)
{
    return !(bool)value;  // true → false, false → true
}
```

**Gebruik:**
```xml
<!-- Placeholder zichtbaar als HasPhoto = FALSE -->
<Label Text="Geen foto"
       IsVisible="{Binding HasPhoto,
                   Converter={StaticResource InvertedBoolConverter}}"/>
```

**Visueel:**
```
HasPhoto = false  →  InvertedBool  →  IsVisible = true  →  Placeholder ZICHTBAAR
HasPhoto = true   →  InvertedBool  →  IsVisible = false →  Placeholder VERBORGEN
```

---

### YearSelectedConverter - Simpel

**Probleem:** Button moet paars zijn als geselecteerd, grijs als niet.

```csharp
public object? Convert(object? value, ..., object? parameter, ...)
{
    int? selectedYear = value as int?;      // Van ViewModel
    int? buttonYear = parameter as int?;    // Van deze button

    bool isSelected = selectedYear == buttonYear;

    return isSelected
        ? Color.FromArgb("#512BD4")   // Paars
        : Color.FromArgb("#E0E0E0");  // Grijs
}
```

**Visueel:**
```
SelectedYear = 2025

Button "2025" (Year = 2025):  2025 == 2025?  JA   → Paars
Button "2024" (Year = 2024):  2024 == 2025?  NEE  → Grijs

Resultaat: [All] [2025] [2024]
                  ^^^^
                  paars
```

---

### Converter vs DataTrigger - Wanneer Welke?

| Situatie | Gebruik |
|----------|---------|
| Check op **eigen** property (`IsSelected`) | DataTrigger |
| Vergelijk met **externe** waarde (`SelectedYear`) | Converter |

```
DataTrigger:  "Ben IK geselecteerd?"
Converter:    "Is MIJN jaar gelijk aan het geselecteerde jaar?"
```

---

### Registratie in App.xaml

```xml
<Application.Resources>
    <converters:InvertedBoolConverter x:Key="InvertedBoolConverter"/>
</Application.Resources>
```

**Gebruik met:**
```xml
Converter={StaticResource InvertedBoolConverter}
```

---

## PhotoService - Foto Capture & Resize

### Wat doet het?

DI-gebaseerde service voor camera/gallery EN resizing van foto's.

### Waarom resizing nodig?

| Zonder resizing | Met resizing |
|-----------------|--------------|
| 10MB foto naar API | Max 4MB naar API |
| Trage uploads | Snelle uploads |
| Server overbelast | Efficiënte verwerking |
| Base64 = 13MB | Base64 = max 5.3MB |

### Implementatie (gecombineerd)

```csharp
// Services/PhotoService.cs - ÉÉN service voor alles
public class PhotoService : IPhotoService
{
    private const int ImageMaxSizeBytes = 4194304;  // 4MB
    private const int ImageMaxResolution = 1024;     // 1024px

    public async Task<byte[]?> CapturePhotoAsync()
    {
        var photo = await MediaPicker.Default.CapturePhotoAsync();
        if (photo == null) return null;

        await Task.Delay(100);  // Android hersteltijd

        // Retry pattern voor Android file lock
        try {
            return await ResizePhotoStreamAsync(photo);
        } catch {
            await Task.Delay(200);
            return await ResizePhotoStreamAsync(photo);
        }
    }

    public async Task<byte[]?> PickPhotoAsync()
    {
        var photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo == null) return null;
        return await ResizePhotoStreamAsync(photo);
    }

    private async Task<byte[]> ResizePhotoStreamAsync(FileResult photo)
    {
        using var stream = await photo.OpenReadAsync();

        if (stream.Length > ImageMaxSizeBytes)
        {
            var image = PlatformImage.FromStream(stream);
            return image!.Downsize(ImageMaxResolution, true).AsBytes();
        }

        using var reader = new BinaryReader(stream);
        return reader.ReadBytes((int)stream.Length);
    }
}
```

### Gebruik in ViewModel (simpel!)

```csharp
// AddStopViewModel.cs - via DI
private readonly IPhotoService _photoService;

private async Task CapturePhoto()
{
    var bytes = await _photoService.CapturePhotoAsync();  // Alles in één!
    if (bytes != null)
        await ProcessPhoto(bytes);
}
```

### Waarom DI i.p.v. Static?

| Static (oud) | DI (nieuw) |
|--------------|------------|
| `PhotoImageService.ResizePhotoStreamAsync()` | `_photoService.CapturePhotoAsync()` |
| Retry logica in ViewModel | Retry logica in Service |
| Moeilijk te testen | Makkelijk te mocken |
| Verspreid over ViewModels | Gecentraliseerd |

**Voordelen DI:**
- Retry pattern op één plek
- Testbaar (mock IPhotoService)
- ViewModels simpeler
- Consistente interface

---

## IValueConverter Interface

### Wat is een Converter?

Een converter transformeert data tussen ViewModel en View in XAML bindings.

```
ViewModel Property → [IValueConverter.Convert()] → XAML Element Property
XAML Element Property → [IValueConverter.ConvertBack()] → ViewModel Property
```

### Interface

```csharp
public interface IValueConverter
{
    // ViewModel → View transformatie
    object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    // View → ViewModel transformatie (optioneel)
    object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}
```

### Registratie in App.xaml

```xml
<!-- App.xaml -->
<Application xmlns:converters="clr-namespace:TripTracker.App.Converters">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Converters registreren als StaticResource -->
            <converters:InvertedBoolConverter x:Key="InvertedBoolConverter"/>
            <converters:YearSelectedConverter x:Key="YearSelectedConverter"/>
            <converters:YearSelectedTextConverter x:Key="YearSelectedTextConverter"/>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### Gebruik in XAML

```xml
<!-- Met StaticResource key -->
<Label IsVisible="{Binding HasPhoto, Converter={StaticResource InvertedBoolConverter}}"/>

<!-- Met ConverterParameter -->
<Button BackgroundColor="{Binding SelectedYear,
    Converter={StaticResource YearSelectedConverter},
    ConverterParameter={Binding Year}}"/>
```

---

## InvertedBoolConverter

### Doel

Keert een boolean waarde om. Typisch gebruik: placeholder tonen als GEEN foto aanwezig is.

### Implementatie

```csharp
// Converters/InvertedBoolConverter.cs
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;  // Keer om
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;  // Keer ook terug om (symmetrisch)
        }
        return false;
    }
}
```

### Gebruik

```xml
<!-- AddTripPage.xaml -->
<Border>
    <Grid>
        <!-- Placeholder: zichtbaar als HasPhoto = FALSE -->
        <VerticalStackLayout IsVisible="{Binding HasPhoto,
            Converter={StaticResource InvertedBoolConverter}}">
            <Label Text="Take or pick a photo"/>
        </VerticalStackLayout>

        <!-- Foto preview: zichtbaar als HasPhoto = TRUE -->
        <Image Source="{Binding PhotoPreview}"
               IsVisible="{Binding HasPhoto}"/>
    </Grid>
</Border>
```

### Waarom niet gewoon !HasPhoto property?

| Aanpak | Nadeel |
|--------|--------|
| Extra `NoPhoto` property | Duplicatie, moet sync houden |
| Computed property | Moet OnPropertyChanged handmatig triggeren |
| **Converter** | Eenmalig, herbruikbaar, geen extra code |

---

## YearSelectedConverter & YearSelectedTextConverter

### Doel

Bepaalt de **BackgroundColor** en **TextColor** van jaar filter buttons op basis van selectie.

### Implementatie

```csharp
// Converters/YearSelectedConverter.cs

// BackgroundColor converter
public class YearSelectedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value = SelectedYear (int? van ViewModel)
        // parameter = jaar van de button (int of null voor "All")

        int? selectedYear = value as int?;
        int? buttonYear = parameter as int?;

        // Check of deze button actief is
        bool isSelected = selectedYear == buttonYear;

        // Return kleur: paars voor actief, lichtgrijs voor inactief
        return isSelected
            ? Color.FromArgb("#512BD4")   // Paars (primary)
            : Color.FromArgb("#E0E0E0");  // Lichtgrijs
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();  // Niet nodig voor OneWay binding
    }
}

// TextColor converter
public class YearSelectedTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int? selectedYear = value as int?;
        int? buttonYear = parameter as int?;

        bool isSelected = selectedYear == buttonYear;

        // Wit voor actief, zwart voor inactief
        return isSelected ? Colors.White : Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### Gebruik met ConverterParameter

```xml
<!-- TripsPage.xaml - Jaar filter buttons -->
<Button Text="{Binding DisplayText}"
        BackgroundColor="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}},
            Path=SelectedYear,
            Converter={StaticResource YearSelectedConverter},
            ConverterParameter={Binding Year}}"
        TextColor="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:TripsViewModel}},
            Path=SelectedYear,
            Converter={StaticResource YearSelectedTextConverter},
            ConverterParameter={Binding Year}}"/>
```

### Alternatief: DataTrigger

In Fase 15 (YearFilterItem met IsSelected property) gebruiken we DataTrigger i.p.v. converters:

```xml
<!-- Alternatieve aanpak met DataTrigger -->
<Button Text="{Binding DisplayText}"
        BackgroundColor="#E0E0E0"
        TextColor="Black">
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

| Aanpak | Wanneer gebruiken |
|--------|-------------------|
| **Converter** | Vergelijk met externe waarde (SelectedYear) |
| **DataTrigger** | Check op property van eigen item (IsSelected) |

---

## Converter vs Computed Property vs DataTrigger

| Methode | Wanneer |
|---------|---------|
| **Converter** | Transformatie die niet in ViewModel hoort (UI-specifiek) |
| **Computed Property** | Logica die bij het Model/ViewModel hoort |
| **DataTrigger** | Visuele state based op boolean property |

**Voorbeelden:**

```csharp
// Computed Property in ViewModel
public string DisplayText => Year?.ToString() ?? "All";

// Converter in XAML
IsVisible="{Binding HasPhoto, Converter={StaticResource InvertedBoolConverter}}"

// DataTrigger in XAML
<DataTrigger Binding="{Binding IsSelected}" Value="True">
    <Setter Property="BackgroundColor" Value="#512BD4"/>
</DataTrigger>
```

---

## Bestanden

| Bestand | Doel |
|---------|------|
| `Services/PhotoService.cs` | DI service: camera/gallery + resize |
| `Converters/InvertedBoolConverter.cs` | Bool inverter voor IsVisible |
| `Converters/YearSelectedConverter.cs` | Button BackgroundColor |
| `Converters/YearSelectedTextConverter.cs` | Button TextColor |
| `App.xaml` | Converter registraties |

---

## Cursus Compliance

| Vereiste | Status |
|----------|--------|
| DI Photo Service | PhotoService (IPhotoService) |
| IValueConverter | 3 converters geïmplementeerd |
| XAML Resources | Converters in App.xaml |
| Separation of Concerns | UI logic in converters, niet ViewModel |

---

## Examenvragen

### Vraag 1: Convert vs ConvertBack

**Vraag:** Wat is het verschil tussen `Convert` en `ConvertBack` in IValueConverter?

**Antwoord:**
| Method | Richting | Gebruik |
|--------|----------|---------|
| `Convert` | ViewModel → View | Altijd nodig |
| `ConvertBack` | View → ViewModel | Alleen voor TwoWay bindings |

```csharp
// Convert: ViewModel property → XAML element
public object? Convert(object? value, ...)
{
    // value = HasPhoto (bool)
    return !(bool)value;  // Omgekeerde bool voor IsVisible
}

// ConvertBack: XAML element → ViewModel property (optioneel)
public object? ConvertBack(object? value, ...)
{
    // Vaak: throw new NotImplementedException();
}
```

**ConvertBack is alleen nodig bij:**
- TwoWay bindings (bv. Entry.Text)
- Wanneer gebruiker input moet terugvloeien naar ViewModel

---

### Vraag 2: ConverterParameter

**Vraag:** Hoe gebruik je een ConverterParameter in XAML?

**Antwoord:**
```xml
<!-- ConverterParameter is een extra waarde die je meegeeft -->
<Button BackgroundColor="{Binding SelectedYear,
    Converter={StaticResource YearSelectedConverter},
    ConverterParameter={Binding Year}}"/>
```

**In de Converter:**
```csharp
public object? Convert(object? value, Type targetType, object? parameter, ...)
{
    int? selectedYear = value as int?;      // van Binding
    int? buttonYear = parameter as int?;    // van ConverterParameter

    return selectedYear == buttonYear
        ? Color.FromArgb("#512BD4")
        : Color.FromArgb("#E0E0E0");
}
```

**Typische use cases:**
- Vergelijk met tweede waarde
- Format string meegeven
- Threshold/limiet waarde

---

### Vraag 3: Converter vs Computed Property

**Vraag:** Wanneer gebruik je een converter in plaats van een computed property?

**Antwoord:**
| Criterium | Converter | Computed Property |
|-----------|-----------|-------------------|
| UI-specifiek | Ja | Nee |
| Herbruikbaar | Ja, app-wide | Nee, per ViewModel |
| Logica locatie | View layer | ViewModel layer |
| Dependencies | Geen andere properties | Kan andere properties gebruiken |

**Gebruik Converter voor:**
- Bool inverteren (IsVisible)
- Kleuren bepalen
- Format transformaties

**Gebruik Computed Property voor:**
- Business logic
- Combinatie van meerdere properties
- `DisplayText => Year?.ToString() ?? "All"`

---

### Vraag 4: Waarom Photo Resizing?

**Vraag:** Waarom resize je foto's voordat je ze naar de API stuurt?

**Antwoord:**
1. **API limieten**: Veel APIs hebben max request size (bv. 4MB)
2. **Base64 overhead**: Base64 encoding vergroot data met 33%
3. **Performance**: Grote uploads zijn traag
4. **Storage**: Server disk space besparen
5. **UX**: Snellere upload = betere gebruikerservaring

```csharp
private const int ImageMaxSizeBytes = 4194304;  // 4MB

if (stream.Length > ImageMaxSizeBytes)
{
    // Resize naar max 1024px
    var newImage = image.Downsize(ImageMaxResolution, true);
}
```

---

### Vraag 5: PlatformImage.Downsize

**Vraag:** Wat doet `PlatformImage.Downsize()`?

**Antwoord:**
```csharp
var newImage = image.Downsize(1024, true);
```

| Parameter | Betekenis |
|-----------|-----------|
| `1024` | Max breedte OF hoogte in pixels |
| `true` | Behoud aspect ratio |

**Wat het doet:**
1. Check of afbeelding groter is dan 1024px
2. Schaal proportioneel naar beneden
3. Behoud aspect ratio (geen vervorming)
4. Return nieuwe `IImage`

---

### Vraag 6: Static vs DI Service

**Vraag:** Wanneer kies je voor een static class in plaats van een DI service?

**Antwoord:**
| Criterium | Static Class | DI Service |
|-----------|--------------|------------|
| State | Geen | Kan state hebben |
| Dependencies | Geen andere services | Kan injecteren |
| Lifetime | App lifetime | Scoped/Transient/Singleton |
| Testing | Moeilijker mocken | Makkelijk mocken |
| Gebruik | Pure functions | Business logic |

**Kies Static voor:**
- Utility methods (string helpers, math)
- Pure functions (geen side effects)
- Geen dependencies nodig

**Kies DI voor:**
- Services met state
- Services die andere services nodig hebben
- Services die gemocked moeten worden in tests

```csharp
// Static (geen DI nodig)
byte[] bytes = await _photoService.CapturePhotoAsync();

// DI Service (injectie via constructor)
public AddStopViewModel(INavigationService nav, IPhotoService photo)
{
    _photoService = photo;
}
```

---

## Samenvatting

- **PhotoService** - DI service voor camera/gallery + resize (max 4MB/1024px)
- **IValueConverter** - Interface voor XAML value transformatie
- **InvertedBoolConverter** - Keert bool om voor inverse IsVisible
- **YearSelectedConverter** - Bepaalt button kleur obv SelectedYear
- **ConverterParameter** - Extra waarde meegeven aan converter
- **Static vs DI** - Static voor pure utilities, DI voor services met dependencies
- **Registratie** - Converters in App.xaml als StaticResource

---

## Referenties

- **IValueConverter**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/converters)
- **PlatformImage**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/images)
- **Cursus**: Les 2 - MAUI & MVVM (Data Binding)
