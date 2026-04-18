---
fase: 12
status: Voltooid
tags:
  - bugfix
  - net8
  - camera
  - maui
  - threading
created: 2025-12-20
---

# Fase 12: UI cleanup & .NET 8 downgrade

## Overzicht

Deze fase behandelt **platform-specifieke bugfixes** en **UI polish**. Het belangrijkste onderwerp is de camera crash op Android en hoe je omgaat met onbetrouwbare platform APIs.

> [!warning] Kritieke Fix
> **Downgrade van .NET 9 naar .NET 8** was nodig vanwege een bekende MAUI bug met `MediaPicker.CapturePhotoAsync()` op Android.

---

## Waarom .NET 8 i.p.v. .NET 9?

| Aspect | .NET 8 | .NET 9 |
|--------|--------|--------|
| Support Type | **LTS** (Long Term Support) | STS (Standard Term Support) |
| Support tot | November 2026 | Mei 2026 |
| Stabiliteit | Productie-klaar | Cutting edge, meer bugs |
| MAUI Camera | ✅ Werkt | ❌ Crash op Android |

> [!info] Cursus Referentie
> Voor examenprojecten is stabiliteit belangrijker dan nieuwe features. Kies altijd LTS versies voor productie-apps.

---

## 1. Camera crash fix

### Probleem

De app crashte wanneer een foto werd genomen met de camera op Android.

**Symptomen:**
- App sluit volledig af na foto bevestigen
- Geen error in debug output
- Alleen bij camera, niet bij gallery

### Diagnose: Android activity lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│                Android Activity Lifecycle                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  TripTracker App                    Camera App               │
│  ┌────────────┐                    ┌────────────┐           │
│  │ AddStopPage│                    │            │           │
│  │            │──CapturePhotoAsync─▶│  Camera   │           │
│  │            │     (Intent)       │   (Native) │           │
│  │            │                    │            │           │
│  │  PAUSED   │◀─────────────────────│            │           │
│  │  (or KILLED)◀ Memory pressure   │            │           │
│  │            │                    │            │           │
│  │            │◀───Photo result────│  ✓ button  │           │
│  │  CRASHED! │    (if killed)     └────────────┘           │
│  └────────────┘                                             │
│                                                              │
│  Probleem: Als Android TripTracker killt tijdens camera,    │
│  is de async call verloren en crasht de app bij terugkeer.  │
└─────────────────────────────────────────────────────────────┘
```

### Oplossing 1: .NET 8 downgrade

```xml
<!-- TripTracker.App.csproj -->

<!-- VOOR (.NET 9) -->
<TargetFrameworks>net9.0-android;net9.0-ios</TargetFrameworks>

<!-- NA (.NET 8) -->
<TargetFrameworks>net8.0-android</TargetFrameworks>
<CheckEolWorkloads>false</CheckEolWorkloads>
```

**Package versies:**
```xml
<!-- VOOR -->
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="9.0.8" />

<!-- NA -->
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.1" />
```

### Oplossing 2: retry logica (in PhotoService)

De retry logica zit nu **in de PhotoService** zelf, niet meer in de ViewModel:

```csharp
// Services/PhotoService.cs - retry pattern ingebouwd
public async Task<byte[]?> CapturePhotoAsync()
{
    var photo = await MediaPicker.Default.CapturePhotoAsync();
    if (photo == null) return null;

    // Kleine delay voor Android recovery
    await Task.Delay(100);

    try
    {
        return await ResizePhotoStreamAsync(photo);
    }
    catch
    {
        // Retry na extra delay
        await Task.Delay(200);
        return await ResizePhotoStreamAsync(photo);
    }
}
```

Hierdoor wordt de ViewModel simpel:

```csharp
// AddStopViewModel.cs - ÉÉN lijn dankzij PhotoService!
private async Task CapturePhoto()
{
    var bytes = await _photoService.CapturePhotoAsync();
    if (bytes != null)
    {
        await ProcessPhoto(bytes);
    }
}
```

**Voordeel:**
- Retry logica op één plek (PhotoService)
- ViewModels blijven simpel
- Consistente error handling voor camera én gallery

---

## 2. Geocoding duplicatie fix

### Probleem

Adressen toonden dezelfde waarde twee keer:
- **Fout:** "Herentals, Herentals"
- **Correct:** "Herentals, België"

### Oorzaak

`Geocoding.GetPlacemarksAsync()` retourneert soms dezelfde waarde voor `SubLocality` en `Locality`.

### Oplossing: duplicaat check

```csharp
// AddStopViewModel.cs - GetLocationAndGeocode()
var addressParts = new List<string>();

// Voeg alleen toe als niet al aanwezig
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
```

**Pattern:** Check met `.Contains()` voordat je toevoegt.

---

## 3. Dark theme fixes

### TextColor probleem

Labels waren onzichtbaar op donkere achtergronden.

```xml
<!-- VOOR: Geen TextColor = standaard donker -->
<Label Text="{Binding LatitudeDisplay}" FontSize="14"/>

<!-- NA: Expliciete TextColor -->
<Label Text="{Binding LatitudeDisplay}"
       FontSize="14"
       TextColor="{StaticResource Gray600}"/>
```

### Editor achtergrond probleem

Witte Editor achtergrond paste niet bij dark theme.

```xml
<!-- VOOR: Frame met witte achtergrond -->
<Frame BackgroundColor="White" Padding="5">
    <Editor Text="{Binding Description}"/>
</Frame>

<!-- NA: Border met transparante achtergrond -->
<Border Stroke="LightGray" StrokeThickness="1" Padding="5">
    <Editor Text="{Binding Description}"
            BackgroundColor="Transparent"/>
</Border>
```

> [!tip] Frame vs Border
> - **Frame** heeft altijd een achtergrond (moeilijk transparant)
> - **Border** is lichter en ondersteunt transparantie beter
> - Gebruik Border voor moderne MAUI apps

---

## Retry pattern voor platform APIs

```
┌─────────────────────────────────────────────────────────────┐
│                    Retry Pattern                             │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   1. TRY                                                     │
│      └── await PlatformAPI.MethodAsync()                    │
│                                                              │
│   2. CATCH (op fout)                                        │
│      ├── Log de error                                       │
│      ├── await Task.Delay(100-500ms)                        │
│      └── RETRY (max 1-3 keer)                               │
│                                                              │
│   3. FALLBACK (als alles faalt)                             │
│      └── Toon foutmelding aan gebruiker                     │
│                                                              │
│   Toepasbaar op:                                            │
│   • Camera/MediaPicker                                      │
│   • GPS/Geolocation                                         │
│   • Network calls                                           │
│   • File I/O                                                │
└─────────────────────────────────────────────────────────────┘
```

---

## Bestanden gewijzigd

| Bestand | Wijziging |
|---------|-----------|
| `TripTracker.App.csproj` | .NET 9 → .NET 8, package versies |
| `ViewModels/AddStopViewModel.cs` | Retry logica, MainThread call |
| `Views/AddStopPage.xaml` | TextColor fixes |
| `Views/EditStopPage.xaml` | Border i.p.v. Frame |

---

## Cursus compliance

| Vereiste | Status |
|----------|--------|
| Platform-specifieke fixes | ✅ Android camera handling |
| Error handling | ✅ Try-catch met retry |
| Threading | ✅ MainThread.InvokeOnMainThreadAsync |
| UI consistency | ✅ TextColor, achtergronden |

---

## Examenvragen

### Vraag 1: .NET 8 vs .NET 9

**Vraag:** Waarom kies je .NET 8 in plaats van .NET 9 voor een MAUI productie-app?

**Antwoord:**
| Criterium | .NET 8 | .NET 9 |
|-----------|--------|--------|
| Support | **LTS** (3 jaar) | STS (18 maanden) |
| Stabiliteit | Productie-klaar | Nieuwe features, meer bugs |
| MAUI Camera | Werkt | Bekende crash bugs |

**.NET 8 is de LTS (Long Term Support) versie.** Voor examenprojecten en productie-apps is stabiliteit belangrijker dan de nieuwste features.

---

### Vraag 2: MainThread.InvokeOnMainThreadAsync

**Vraag:** Waarom heb je `MainThread.InvokeOnMainThreadAsync()` nodig na een camera capture?

**Antwoord:**
```csharp
await MainThread.InvokeOnMainThreadAsync(async () =>
{
    PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
});
```

**Redenen:**
1. Na camera capture ben je mogelijk op een **background thread**
2. UI properties (zoals `ImageSource`) moeten op de **main thread** worden gezet
3. Zonder dit krijg je een **cross-thread exception**

**Regel:** Alle UI updates moeten op de main thread gebeuren.

---

### Vraag 3: retry pattern

**Vraag:** Hoe implementeer je retry logica voor onbetrouwbare platform APIs?

**Antwoord:**
```csharp
byte[]? result = null;

try
{
    result = await PlatformAPI.MethodAsync();
}
catch (Exception)
{
    // Wacht kort
    await Task.Delay(200);

    // Probeer opnieuw
    result = await PlatformAPI.MethodAsync();
}
```

**Stappen:**
1. **Try**: Probeer de operatie
2. **Catch**: Vang exception op
3. **Delay**: Geef platform tijd om te herstellen (100-500ms)
4. **Retry**: Probeer opnieuw (max 1-3 keer)

Dit pattern is toepasbaar op camera, GPS, network calls, en file I/O.

---

### Vraag 4: duplicaten voorkomen in strings

**Vraag:** Hoe voorkom je duplicaten bij het samenstellen van een adres string?

**Antwoord:**
```csharp
var parts = new List<string>();

// Check of waarde al aanwezig is
if (!string.IsNullOrEmpty(value) && !parts.Contains(value))
{
    parts.Add(value);
}

var result = string.Join(", ", parts);
```

**Patroon:**
1. Gebruik een `List<string>` om delen te verzamelen
2. Check met `.Contains()` voordat je toevoegt
3. Join met separator (bv. ", ") aan het einde

---

### Vraag 5: frame vs border

**Vraag:** Wat is het verschil tussen Frame en Border in MAUI?

**Antwoord:**
| Aspect | Frame | Border |
|--------|-------|--------|
| Achtergrond | Altijd aanwezig | Optioneel/transparant |
| Performance | Zwaarder | Lichter |
| Moderniteit | Legacy | Modern MAUI |
| Shadow | Ingebouwd | Aparte Shadow element |

**Aanbeveling:** Gebruik `Border` voor moderne apps, vooral als je transparante achtergronden nodig hebt.

```xml
<!-- Moderne aanpak -->
<Border Stroke="LightGray" StrokeThickness="1">
    <Editor BackgroundColor="Transparent"/>
</Border>
```

---

### Vraag 6: TextColor en theming

**Vraag:** Waarom moet je altijd een expliciete TextColor specificeren voor labels op een gekleurde achtergrond?

**Antwoord:**
- Zonder `TextColor` gebruikt MAUI de **standaard theme kleur**
- Op een donkere achtergrond kan dit dezelfde kleur zijn → **onzichtbaar**
- Door expliciet `TextColor` te zetten, garandeer je contrast

```xml
<!-- Altijd leesbaar -->
<Label Text="{Binding Value}"
       TextColor="{StaticResource Gray600}"/>
```

**Best Practice:** Definieer kleuren in `Resources/Styles/Colors.xaml` en gebruik `{StaticResource}`.

---

## Samenvatting

- **.NET 8 LTS** voor stabiliteit (niet .NET 9)
- **Retry logica** voor onbetrouwbare platform APIs
- **MainThread** voor alle UI updates
- **Contains()** check om duplicaten te voorkomen
- **Border** i.p.v. Frame voor transparante achtergronden
- **Expliciete TextColor** voor leesbaarheid

---

## Referenties

- **GitHub Issue Camera Bug**: [dotnet/maui#26541](https://github.com/dotnet/maui/issues/26541)
- **MainThread**: [docs.microsoft.com](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread)
- **.NET Support Policy**: [dotnet.microsoft.com](https://dotnet.microsoft.com/en-us/platform/support/policy)
- **Cursus**: Les 3 - Device Features (MediaPicker)
