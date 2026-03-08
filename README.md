# TripTracker

Een cross-platform mobile app waarmee je reizen kunt plannen en bijhouden, met een eigen REST API als backend.

> Schoolproject voor AI.NET — Thomas More Hogeschool (score: 19/20)

## Screenshots

*Wordt nog aangevuld*

## Tech stack

- .NET MAUI (cross-platform mobile)
- ASP.NET Core Web API
- Entity Framework Core + SQLite
- AutoMapper
- CommunityToolkit.Mvvm

## Features

- Reizen aanmaken, bewerken en verwijderen
- Stops per reis toevoegen met GPS-locatie en foto's
- Interactieve kaartweergave van stops (MAUI Maps)
- Automatische geocoding (coördinaten → adres)
- Jaarfilter voor reisoverzicht
- AI-powered beeldherkenning voor stop-beschrijvingen

## Installatie

```bash
# Clone repo
git clone https://github.com/stijn-portfolio/triptracker.git

# API starten
cd TripTracker.API
dotnet run

# MAUI app starten (Visual Studio 2022+ vereist)
cd TripTracker.App
dotnet build
```

## Architectuur

**API (TripTracker.API):**
- Repository pattern met interface-first design
- DTOs gescheiden van entities (nooit entities via API exposen)
- AutoMapper voor entity ↔ DTO conversie
- Dependency injection via `Program.cs`

**App (TripTracker.App):**
- MVVM pattern met CommunityToolkit.Mvvm
- Services layer (API, Geocoding, Geolocation, Photo, Navigation)
- WeakReferenceMessenger voor cross-ViewModel communicatie
- Converters en custom controls

```
TripTracker/
├── TripTracker.API/          # REST API backend
│   ├── Controllers/          # API endpoints
│   ├── Entities/             # Database models
│   ├── Models/               # DTOs
│   ├── Services/             # Business logic
│   └── MappingProfiles/      # AutoMapper configuratie
├── TripTracker.App/          # .NET MAUI frontend
│   ├── ViewModels/           # MVVM ViewModels
│   ├── Views/                # XAML pages
│   ├── Services/             # API + device services
│   ├── Models/               # App-side models
│   └── Converters/           # Value converters
└── docs/                     # Ontwikkeldocumentatie
```
