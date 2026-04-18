---
fase: TripTracker API
status: Voltooid
tags:
  - api
  - dotnet
  - entity-framework
  - automapper
  - repository-pattern
  - examen
created: 2025-12-20
---

# TripTracker API - volledige implementatie

## Overzicht

Deze documentatie beschrijft de **TripTracker.API** die is gebouwd volgens de .NET API Development principes uit de cursus. De API beheert trips en trip stops (tussenstops), met een one-to-many relatie tussen beide tabellen.

> [!info] Doel
> De TripTracker API dient als backend voor een MAUI app waarmee gebruikers hun reizen kunnen bijhouden met GPS-locaties, foto's en beschrijvingen per tussenstop.

**Wat is er gebouwd:**
- ASP.NET Core Web API met Entity Framework Core
- SQL Server database met 2 tabellen en one-to-many relatie
- Repository Pattern voor data access
- AutoMapper voor DTO conversies
- CRUD endpoints voor Trips en TripStops
- Seed data voor development

---

## 1. Database structuur

### Entities

De database bestaat uit **2 tabellen** met een **one-to-many** relatie:

```
Trip (1) ──────< TripStop (Many)
```

#### Trip entity

**Locatie:** `Entities/Trip.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripTracker.API.Entities
{
    public class Trip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // Navigation property - One-to-Many relatie
        public ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();
    }
}
```

> [!tip] Examenvraag: Wat doen de Data Annotations?
> - `[Key]`: Markeert de primary key
> - `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`: Database genereert automatisch een oplopende waarde
> - `[Required]`: Veld mag niet NULL zijn
> - `[MaxLength(100)]`: Maximale lengte van string in database
> - `?` na type: Nullable property (mag NULL zijn)

#### TripStop entity

**Locatie:** `Entities/TripStop.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripTracker.API.Entities
{
    public class TripStop
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TripId { get; set; }  // Foreign Key

        [ForeignKey("TripId")]
        public Trip? Trip { get; set; }  // Navigation property

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public DateTime DateTime { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }
    }
}
```

> [!tip] Examenvraag: Hoe werk je met foreign keys?
> - `TripId` is de foreign key property (int)
> - `[ForeignKey("TripId")]` koppelt de navigation property aan de FK
> - `Trip? Trip` is de navigation property (nullable omdat EF lazy loading)
> - In de parent (`Trip`) heb je een `ICollection<TripStop>` voor de relatie

### Vergelijking met Safari.API

| TripTracker | Safari | Verschil |
|-------------|--------|----------|
| `Trip` ↔ `TripStop` | `Animal` ↔ `Sighting` | Beide one-to-many |
| `Trip` heeft `ICollection<TripStop>` | `Animal` heeft GEEN collection | TripTracker toont parent-child relatie explicieter |
| GPS coords in child (`TripStop`) | GPS coords in child (`Sighting`) | Zelfde patroon |
| `ImageUrl` in parent | Geen image in `Animal` | Extra feature |

---

## 2. DTOs (Data transfer objects)

> [!warning] KRITIEK: Waarom DTOs?
> **NOOIT entities direct returnen via API!**
>
> Redenen:
> - Navigation properties veroorzaken circular references
> - Je wilt niet alle properties blootstellen
> - Validation attributes op entities zijn voor database, niet voor API input
> - DTOs geven controle over wat je expose

### Trip DTOs

#### TripDto (voor GET requests)

**Locatie:** `Models/TripDto.cs`

```csharp
namespace TripTracker.API.Models
{
    public class TripDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ImageUrl { get; set; }
        // GEEN TripStops collection - voorkomt circular refs
    }
}
```

#### TripForCreationDto (voor POST requests)

**Locatie:** `Models/TripForCreationDto.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace TripTracker.API.Models
{
    public class TripForCreationDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }
}
```

> [!tip] Examenvraag: Waarom aparte DTOs voor creation?
> - Geen `Id` property (database genereert die)
> - Validation attributes voor API input
> - Client stuurt ALLEEN de velden die nodig zijn voor creatie
> - Voorkomt dat client ongewenste velden kan instellen

#### TripForUpdateDto

**Locatie:** `Models/TripForUpdateDto.cs`

Zelfde structuur als `TripForCreationDto` (in dit geval).

### TripStop DTOs

#### TripStopDto

**Locatie:** `Models/TripStopDto.cs`

```csharp
namespace TripTracker.API.Models
{
    public class TripStopDto
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        // Geen Trip navigation property - voorkomt circular references
        public string Title { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime DateTime { get; set; }
        public string? Country { get; set; }
    }
}
```

> [!tip] Best Practice
> `TripStopDto` heeft alleen `TripId` (int), GEEN `Trip` object. Dit voorkomt circular references en volgt het Safari.API patroon.

#### TripStopForCreationDto

**Locatie:** `Models/TripStopForCreationDto.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace TripTracker.API.Models
{
    public class TripStopForCreationDto
    {
        [Required]
        public int TripId { get; set; }  // FK is REQUIRED bij creation

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        public DateTime DateTime { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }
    }
}
```

---

## 3. AutoMapper profiles

AutoMapper **vermijdt handmatige mapping** tussen entities en DTOs.

> [!info] Wat doet AutoMapper?
> ```csharp
> // ZONDER AutoMapper (handmatig):
> var dto = new TripDto
> {
>     Id = trip.Id,
>     Name = trip.Name,
>     Description = trip.Description,
>     // ... nog 10 properties
> };
>
> // MET AutoMapper:
> var dto = _mapper.Map<TripDto>(trip);
> ```

### TripProfile

**Locatie:** `MappingProfiles/TripProfile.cs`

```csharp
using AutoMapper;
using TripTracker.API.Entities;
using TripTracker.API.Models;

namespace TripTracker.API.MappingProfiles
{
    public class TripProfile : Profile
    {
        public TripProfile()
        {
            CreateMap<Trip, TripDto>();
            CreateMap<TripForCreationDto, Trip>();
            CreateMap<TripForUpdateDto, Trip>();
        }
    }
}
```

> [!tip] Examenvraag: Hoe werkt CreateMap?
> `CreateMap<Source, Destination>()` configureert een mapping van Source naar Destination.
>
> AutoMapper matcht automatisch properties met dezelfde naam. Geen extra configuratie nodig als property names overeenkomen.

### TripStopProfile

**Locatie:** `MappingProfiles/TripStopProfile.cs`

```csharp
using AutoMapper;
using TripTracker.API.Entities;
using TripTracker.API.Models;

namespace TripTracker.API.MappingProfiles
{
    public class TripStopProfile : Profile
    {
        public TripStopProfile()
        {
            CreateMap<TripStop, TripStopDto>();
            CreateMap<TripStopForCreationDto, TripStop>();
            CreateMap<TripStopForUpdateDto, TripStop>();
        }
    }
}
```

### Vergelijking met Safari.API

Zowel Safari.API als TripTracker.API gebruiken **AutoMapper met Profiles**. Dit is een best practice voor DTO conversies. Let op: AutoMapper wordt niet expliciet in de cursus behandeld, maar wordt wel gebruikt in de voorbeeldprojecten.

---

## 4. Repository pattern

Het Repository Pattern **scheidt data access logica** van de rest van de applicatie.

> [!info] Voordelen Repository Pattern
> - **Testability**: Controllers kunnen gemockte repositories gebruiken
> - **Separation of Concerns**: Controllers hoeven niet te weten over DbContext
> - **Consistency**: Alle data access via dezelfde interface
> - **Reusability**: Repository kan hergebruikt worden in meerdere controllers

### Interface vs implementation

**Belangrijk patroon:**
- Interface definieert **SYNC** methods voor Add/Update/Delete
- Interface definieert **ASYNC** method voor SaveChanges
- Implementation doet het echte werk

### ITripRepository

**Locatie:** `Services/ITripRepository.cs`

```csharp
using TripTracker.API.Entities;

namespace TripTracker.API.Services
{
    public interface ITripRepository
    {
        Task<IEnumerable<Trip>> GetTripsAsync();
        Task<Trip> GetTripAsync(int id);
        void AddTrip(Trip trip);           // SYNC - alleen track!
        void DeleteTrip(Trip trip);        // SYNC - alleen track!
        void UpdateTrip(Trip trip);        // SYNC - alleen track!
        Task<bool> SaveChangesAsync();     // ASYNC - persists to DB
    }
}
```

> [!tip] Examenvraag: Waarom zijn Add/Update/Delete SYNC?
> Deze methods voegen alleen entities toe aan EF Core's **change tracker**. Er is geen database operatie, dus geen async nodig.
>
> `SaveChangesAsync()` doet de echte database operatie, dus die is async.

### TripRepository

**Locatie:** `Services/TripRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using TripTracker.API.DbContexts;
using TripTracker.API.Entities;

namespace TripTracker.API.Services
{
    public class TripRepository : ITripRepository
    {
        private readonly TripTrackerContext _context;

        public TripRepository(TripTrackerContext context)
        {
            _context = context;
        }

        public void AddTrip(Trip trip)
        {
            _context.Trips.Add(trip);
        }

        public void DeleteTrip(Trip trip)
        {
            _context.Trips.Remove(trip);
        }

        public void UpdateTrip(Trip trip)
        {
            // EF Core tracks changes automatically
            // Method exists for consistency
        }

        public async Task<IEnumerable<Trip>> GetTripsAsync()
        {
            return await _context.Trips
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<Trip> GetTripAsync(int id)
        {
            return await _context.Trips
                .Include(t => t.TripStops)  // Eager loading
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() >= 0);
        }
    }
}
```

> [!tip] Examenvraag: Wat doet Include()?
> `Include(t => t.TripStops)` is **eager loading**: laadt de gerelateerde TripStops in één query.
>
> Zonder Include zou je alleen de Trip ophalen, zonder child entities.

### ITripStopRepository & TripStopRepository

**Locatie:** `Services/ITripStopRepository.cs` & `Services/TripStopRepository.cs`

Zelfde patroon als TripRepository, maar voor TripStops:

```csharp
public interface ITripStopRepository
{
    Task<IEnumerable<TripStop>> GetAllTripStopsAsync();
    Task<TripStop> GetTripStopAsync(int id);
    void AddTripStop(TripStop tripStop);
    void DeleteTripStop(TripStop tripStop);
    void UpdateTripStop(TripStop tripStop);
    Task<bool> SaveChangesAsync();
}
```

### Vergelijking met Safari.API

| TripTracker | Safari | Identiek? |
|-------------|--------|-----------|
| `ITripRepository` | `IAnimalRepository` | Ja, zelfde patroon |
| `TripRepository` | `AnimalRepository` | Ja, zelfde patroon |
| Async Get, Sync Add/Delete | Async Get, Sync Add/Delete | Ja |
| `SaveChangesAsync()` apart | `SaveChangesAsync()` apart | Ja |

**Conclusie:** Repository pattern is **identiek** aan Safari.API. Dit is de standaard cursus pattern.

---

## 5. Controllers

Controllers **exposen HTTP endpoints** en gebruiken Repositories + AutoMapper.

> [!info] Verantwoordelijkheden Controller
> - HTTP routing (`[Route]`, `[HttpGet]`, etc.)
> - Input validatie (gebeurt automatisch via `[ApiController]`)
> - Repository aanroepen voor data
> - AutoMapper gebruiken voor DTO conversie
> - HTTP status codes returnen

### TripsController

**Locatie:** `Controllers/TripsController.cs`

```csharp
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TripTracker.API.Entities;
using TripTracker.API.Models;
using TripTracker.API.Services;

namespace TripTracker.API.Controllers
{
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        private readonly ITripRepository _tripRepository;
        private readonly IMapper _mapper;

        public TripsController(ITripRepository tripRepository, IMapper mapper)
        {
            _tripRepository = tripRepository;
            _mapper = mapper;
        }

        // GET /api/trips
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips()
        {
            var tripsFromRepo = await _tripRepository.GetTripsAsync();
            return Ok(_mapper.Map<IEnumerable<TripDto>>(tripsFromRepo));
        }

        // GET /api/trips/{id}
        [HttpGet("{id}", Name = "GetTrip")]
        public async Task<ActionResult<TripDto>> GetTrip(int id)
        {
            var tripFromRepo = await _tripRepository.GetTripAsync(id);

            if (tripFromRepo == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<TripDto>(tripFromRepo));
        }

        // POST /api/trips
        [HttpPost]
        public async Task<ActionResult<TripDto>> CreateTrip([FromBody] TripForCreationDto trip)
        {
            if (trip == null)
            {
                return BadRequest();
            }

            var tripEntity = _mapper.Map<Trip>(trip);

            _tripRepository.AddTrip(tripEntity);
            await _tripRepository.SaveChangesAsync();

            var tripToReturn = _mapper.Map<TripDto>(tripEntity);

            return CreatedAtRoute(
                "GetTrip",
                new { id = tripToReturn.Id },
                tripToReturn);
        }

        // PUT /api/trips/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTrip(int id, [FromBody] TripForUpdateDto trip)
        {
            var tripFromRepo = await _tripRepository.GetTripAsync(id);

            if (tripFromRepo == null)
            {
                return NotFound();
            }

            _mapper.Map(trip, tripFromRepo);  // Update properties

            _tripRepository.UpdateTrip(tripFromRepo);
            await _tripRepository.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/trips/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTrip(int id)
        {
            var tripFromRepo = await _tripRepository.GetTripAsync(id);

            if (tripFromRepo == null)
            {
                return NotFound();
            }

            _tripRepository.DeleteTrip(tripFromRepo);
            await _tripRepository.SaveChangesAsync();

            return NoContent();
        }
    }
}
```

> [!tip] Examenvraag: Wat is CreatedAtRoute?
> `CreatedAtRoute()` returnt HTTP 201 Created met:
> - **Location header**: URL naar de nieuwe resource (`/api/trips/5`)
> - **Body**: De nieuwe resource (TripDto)
>
> Het gebruikt de route naam ("GetTrip") en parameters (`new { id = 5 }`) om de URL te genereren.

> [!tip] Examenvraag: Waarom NoContent() bij PUT/DELETE?
> - PUT en DELETE hebben geen response body nodig
> - `NoContent()` returnt HTTP 204 No Content
> - Dit is REST best practice

### TripStopsController

**Locatie:** `Controllers/TripStopsController.cs`

Zelfde structuur als TripsController, maar voor TripStops:

```csharp
[ApiController]
[Route("api/tripstops")]
public class TripStopsController : ControllerBase
{
    // Identiek patroon: GET, GET{id}, POST, PUT, DELETE
    // Gebruikt ITripStopRepository + IMapper
}
```

### Vergelijking met Safari.API

| Endpoint Pattern | TripTracker | Safari |
|------------------|-------------|--------|
| GET all | `/api/trips` | `/api/animals` |
| GET by id | `/api/trips/{id}` | `/api/animals/{id}` |
| POST | `/api/trips` | `/api/animals` |
| PUT | `/api/trips/{id}` | `/api/animals/{id}` |
| DELETE | `/api/trips/{id}` | `/api/animals/{id}` |

**Conclusie:** Controller structuur is **identiek** aan Safari.API.

---

## 6. DbContext & database

### TripTrackerContext

**Locatie:** `DbContexts/TripTrackerContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using TripTracker.API.Entities;

namespace TripTracker.API.DbContexts
{
    public class TripTrackerContext : DbContext
    {
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripStop> TripStops { get; set; }

        public TripTrackerContext(DbContextOptions<TripTrackerContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Trips
            modelBuilder.Entity<Trip>().HasData(
                new Trip
                {
                    Id = 1,
                    Name = "Roadtrip Europa 2024",
                    Description = "Avontuurlijke roadtrip door West-Europa",
                    StartDate = new DateTime(2024, 7, 1),
                    EndDate = new DateTime(2024, 7, 14),
                    ImageUrl = null
                },
                new Trip
                {
                    Id = 2,
                    Name = "Weekend Parijs",
                    Description = "Romantisch weekendje Parijs",
                    StartDate = new DateTime(2024, 8, 15),
                    EndDate = new DateTime(2024, 8, 17),
                    ImageUrl = null
                }
            );

            // Seed TripStops
            modelBuilder.Entity<TripStop>().HasData(
                new TripStop
                {
                    Id = 1,
                    TripId = 1,
                    Title = "Eiffeltoren",
                    Description = "De iconische Eiffeltoren in Parijs",
                    Latitude = 48.8584,
                    Longitude = 2.2945,
                    Address = "Champ de Mars, Paris, France",
                    PhotoUrl = null,
                    DateTime = new DateTime(2024, 7, 2, 14, 30, 0),
                    Country = "France"
                },
                new TripStop
                {
                    Id = 2,
                    TripId = 1,
                    Title = "Brandenburger Tor",
                    Description = "Historisch monument in Berlijn",
                    Latitude = 52.5163,
                    Longitude = 13.3777,
                    Address = "Pariser Platz, Berlin, Germany",
                    PhotoUrl = null,
                    DateTime = new DateTime(2024, 7, 5, 10, 0, 0),
                    Country = "Germany"
                },
                new TripStop
                {
                    Id = 3,
                    TripId = 2,
                    Title = "Louvre Museum",
                    Description = "Wereldberoemd kunstmuseum met de Mona Lisa",
                    Latitude = 48.8606,
                    Longitude = 2.3376,
                    Address = "Rue de Rivoli, Paris, France",
                    PhotoUrl = null,
                    DateTime = new DateTime(2024, 8, 16, 9, 0, 0),
                    Country = "France"
                }
            );
        }
    }
}
```

> [!tip] Examenvraag: Wat doet HasData?
> `HasData()` voegt **seed data** toe aan de database tijdens migration.
>
> Gebruik dit voor:
> - Development/test data
> - Initial lookup data
>
> **Let op:** Seed data wordt ALLEEN toegevoegd bij de eerste migration!

### Migrations

**Stappen:**

1. **Add Migration:**
   ```bash
   Add-Migration InitialMigration
   ```
   Dit maakt een migration bestand in `Migrations/` folder.

2. **Update Database:**
   ```bash
   Update-Database
   ```
   Dit past de database aan volgens de migration.

> [!warning] Let op bij Team Work
> Als je in een team werkt:
> - Trek altijd de laatste migrations VOOR je zelf een migration maakt
> - Merge conflicts in migrations zijn lastig te resolven
> - Eventueel: Delete database en run alle migrations opnieuw

---

## 7. Program.cs - application setup

**Locatie:** `Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using TripTracker.API.DbContexts;
using TripTracker.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Register repositories
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripStopRepository, TripStopRepository>();

// Register AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Register DbContext
builder.Services.AddDbContext<TripTrackerContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TripTrackerDBConnectionString")));

// Register Controllers with JSON cycle handling
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler =
    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Swagger for API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for MAUI app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

> [!info] ReferenceHandler.IgnoreCycles
> Deze regel is een **vangnet** voor circular references:
>
> ```csharp
> .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler =
>     System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
> ```
>
> **Best practice:** DTOs zonder terugverwijzende navigation properties. TripTracker volgt dit correct:
> - `TripStopDto` heeft alleen `TripId` (int), geen `Trip` object
> - Geen circular reference mogelijk
> - `IgnoreCycles` blijft als extra vangnet (belt-and-suspenders)

> [!tip] Examenvraag: Wat doet AddScoped?
> Dependency Injection lifetimes:
>
> - **Transient** (`AddTransient`): Nieuwe instance per request
> - **Scoped** (`AddScoped`): Eén instance per HTTP request
> - **Singleton** (`AddSingleton`): Eén instance gedurende app lifetime
>
> **Repositories gebruiken Scoped** omdat ze een DbContext gebruiken (ook scoped).

> [!tip] Examenvraag: Wat doet AddAutoMapper?
> `AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies())` scant alle assemblies naar classes die van `Profile` erven (onze mapping profiles).
>
> Dit registreert automatisch alle `CreateMap` configuraties.

---

## 8. API endpoints overzicht

### Trips endpoints

| Methode | Endpoint | Beschrijving | Request Body | Response |
|---------|----------|--------------|--------------|----------|
| GET | `/api/trips` | Alle trips ophalen | - | `TripDto[]` |
| GET | `/api/trips/{id}` | Specifieke trip ophalen | - | `TripDto` |
| POST | `/api/trips` | Nieuwe trip aanmaken | `TripForCreationDto` | `TripDto` (201 Created) |
| PUT | `/api/trips/{id}` | Trip updaten | `TripForUpdateDto` | 204 No Content |
| DELETE | `/api/trips/{id}` | Trip verwijderen | - | 204 No Content |

### TripStops endpoints

| Methode | Endpoint | Beschrijving | Request Body | Response |
|---------|----------|--------------|--------------|----------|
| GET | `/api/tripstops` | Alle stops ophalen | - | `TripStopDto[]` |
| GET | `/api/tripstops/{id}` | Specifieke stop ophalen | - | `TripStopDto` |
| POST | `/api/tripstops` | Nieuwe stop aanmaken | `TripStopForCreationDto` | `TripStopDto` (201 Created) |
| PUT | `/api/tripstops/{id}` | Stop updaten | `TripStopForUpdateDto` | 204 No Content |
| DELETE | `/api/tripstops/{id}` | Stop verwijderen | - | 204 No Content |

---

## 9. Hoe uit te leggen aan de docent

### Architectuur overview

> [!example] Start met de big picture
> "Ik heb een ASP.NET Core Web API gebouwd volgens het Repository Pattern. De API heeft 2 tabellen met een one-to-many relatie: Trip en TripStop. Ik gebruik Entity Framework Core voor data access, AutoMapper voor DTO conversies, en Dependency Injection voor alle services."

### Database schema tonen

> [!example] Leg de relatie uit (Entities, niet DTOs!)
> "De database bestaat uit Trip en TripStop. Eén trip kan meerdere stops hebben (one-to-many). In de **Trip entity** heb ik een `ICollection<TripStop>` navigation property. In de **TripStop entity** heb ik een `TripId` foreign key en een `Trip` navigation property met `[ForeignKey("TripId")]` attribute. Let op: in de DTOs gebruik ik alleen `TripId`, geen `Trip` object - dat voorkomt circular references."

### DTOs pattern uitleggen

> [!example] Waarom DTOs?
> "Ik gebruik DTOs om entities niet direct te exposen. Dit voorkomt circular references en geeft me controle over wat ik naar de client stuur. Voor GET requests gebruik ik `TripDto`, voor POST gebruik ik `TripForCreationDto` (zonder Id), en voor PUT gebruik ik `TripForUpdateDto`."

### Repository pattern tonen

> [!example] Toon een repository method
> ```csharp
> public async Task<Trip> GetTripAsync(int id)
> {
>     return await _context.Trips
>         .Include(t => t.TripStops)  // Eager loading van child entities
>         .FirstOrDefaultAsync(t => t.Id == id);
> }
> ```
>
> "Hier gebruik ik `Include()` voor eager loading, zodat ik de TripStops in één query ophaal. Dit voorkomt N+1 query problemen."

### AutoMapper demonstreren

> [!example] Laat een mapping zien
> ```csharp
> // In controller:
> var tripDto = _mapper.Map<TripDto>(tripEntity);
> ```
>
> "AutoMapper converteert automatisch tussen entities en DTOs. Ik heb Profile classes aangemaakt met `CreateMap<Trip, TripDto>()` configuraties."

### Dependency injection uitleggen

> [!example] Toon constructor injection
> ```csharp
> public TripsController(ITripRepository tripRepository, IMapper mapper)
> {
>     _tripRepository = tripRepository;
>     _mapper = mapper;
> }
> ```
>
> "Ik gebruik constructor injection voor repositories en AutoMapper. Dit maakt de controller testbaar en los gekoppeld van de implementatie."

---

## 10. Examenvragen & antwoorden

> [!tip] Examenvraag 1: Wat is het verschil tussen Entity en DTO?
> **Antwoord:**
> - **Entity** is de database representatie, met data annotations voor EF Core (`[Key]`, `[ForeignKey]`)
> - **DTO** is de API contract, zonder navigation properties
> - Entities worden NOOIT direct geretourneerd via API (circular reference problemen)
> - DTOs geven controle over welke data je expose

> [!tip] Examenvraag 1b: Waarom heeft de Entity WEL een navigation property maar de DTO niet?
> **Antwoord:**
>
> **Entity (wel):** EF Core heeft navigation properties nodig voor `.Include()` - dit maakt SQL JOINs mogelijk om gerelateerde data in 1 query op te halen.
> ```csharp
> // Zonder navigation property: 2 queries
> var stop = await _context.TripStops.FindAsync(1);
> var trip = await _context.Trips.FindAsync(stop.TripId);
>
> // Met navigation property: 1 query (JOIN)
> var stop = await _context.TripStops
>     .Include(s => s.Trip)
>     .FirstAsync(s => s.Id == 1);
> ```
>
> **DTO (niet):** JSON serialisatie loopt vast in oneindige loop:
> ```
> Trip → Stops → Stop.Trip → Stops → Stop.Trip → ... CRASH!
> ```
>
> Daarom: Entity heeft `Trip` (object), DTO heeft alleen `TripId` (int).

> [!tip] Examenvraag 2: Waarom zijn Add/Update/Delete SYNC in de repository?
> **Antwoord:**
> Deze methods voegen alleen entities toe aan EF Core's change tracker. Er is geen database I/O, dus geen async nodig. Alleen `SaveChangesAsync()` doet de echte database operatie, dus die is async.

> [!tip] Examenvraag 3: Wat doet Include() en waarom gebruik je het?
> **Antwoord:**
> `Include(t => t.TripStops)` is **eager loading**: het laadt gerelateerde entities in één query.
>
> Zonder Include:
> ```csharp
> var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == id);
> // trip.TripStops is NULL of leeg
> ```
>
> Met Include:
> ```csharp
> var trip = await _context.Trips.Include(t => t.TripStops).FirstOrDefaultAsync(t => t.Id == id);
> // trip.TripStops bevat alle gerelateerde stops
> ```

> [!tip] Examenvraag 4: Wat is CreatedAtRoute en waarom gebruik je het?
> **Antwoord:**
> `CreatedAtRoute()` returnt HTTP 201 Created met een Location header die naar de nieuwe resource wijst.
>
> Voorbeeld:
> ```csharp
> return CreatedAtRoute("GetTrip", new { id = 5 }, tripDto);
> // Response:
> // Status: 201 Created
> // Location: https://api.com/api/trips/5
> // Body: { "id": 5, "name": "...", ... }
> ```
>
> Dit is REST best practice: de client weet meteen waar de nieuwe resource te vinden is.

> [!tip] Examenvraag 5: Wat doet AddAutoMapper()?
> **Antwoord:**
> ```csharp
> builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
> ```
>
> Dit scant alle assemblies naar classes die van `Profile` erven (zoals `TripProfile`). AutoMapper registreert alle `CreateMap` configuraties automatisch in de DI container.

> [!tip] Examenvraag 6: Waarom AddScoped voor repositories?
> **Antwoord:**
> Repositories gebruiken een `DbContext`, die ook scoped is (één instance per HTTP request). Als we repositories singleton zouden maken, zouden ze een oude DbContext bijhouden, wat thread-safety problemen veroorzaakt.
>
> **Scoped** = nieuwe instance per HTTP request = matcht de DbContext lifetime.

> [!tip] Examenvraag 7: Wat is seed data en wanneer gebruik je het?
> **Antwoord:**
> Seed data voeg je toe via `HasData()` in `OnModelCreating()`. Dit is test/development data die automatisch in de database komt tijdens migrations.
>
> **Let op:** Seed data wordt ALLEEN toegevoegd bij de eerste migration. Updates doe je via nieuwe migrations.

> [!tip] Examenvraag 8: Hoe werk je met foreign keys in EF Core?
> **Antwoord:**
> ```csharp
> [Required]
> public int TripId { get; set; }  // Foreign key property
>
> [ForeignKey("TripId")]
> public Trip? Trip { get; set; }  // Navigation property
> ```
>
> - `TripId` is de FK property (int)
> - `[ForeignKey("TripId")]` koppelt de navigation property
> - EF Core maakt automatisch een FK constraint in de database

> [!tip] Examenvraag 9: Waarom NoContent() bij PUT/DELETE?
> **Antwoord:**
> PUT en DELETE hebben geen response body nodig volgens REST conventions.
>
> - `NoContent()` = HTTP 204 No Content
> - De client weet al welke resource is geupdate/verwijderd (uit de URL)
> - Geen data returnen = sneller en efficiënter

> [!tip] Examenvraag 10: Wat is ReferenceHandler.IgnoreCycles?
> **Antwoord:**
> ```csharp
> .AddJsonOptions(options =>
>     options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
> ```
>
> Dit voorkomt circular reference errors bij JSON serialization.
>
> **Best practice:** DTOs zonder terugverwijzende navigation properties. Voorbeeld:
> - `TripStopDto` heeft `TripId` (int) - GOED
> - `TripStopDto` heeft `Trip` (TripDto) - FOUT (circular reference risico)
>
> **TripTracker:** Volgt de best practice. `IgnoreCycles` blijft als vangnet.

---

## 11. Verschillen met Safari.API

| Aspect | TripTracker.API | Safari.API | Conclusie |
|--------|-----------------|------------|-----------|
| **AutoMapper** | Ja, met Profiles | Ja, met Profiles | Zelfde patroon |
| **DTOs** | Ja, volledig | Ja, volledig | Zelfde patroon |
| **Repository Pattern** | Ja, identiek | Ja, identiek | Zelfde patroon |
| **Seed Data** | Ja, in DbContext | Ja, in DbContext | Zelfde patroon |
| **CORS** | Ja | Ja | Zelfde patroon |
| **Swagger** | Ja | Ja | Zelfde patroon |
| **ReferenceHandler.IgnoreCycles** | Ja | Ja | Beide als vangnet |

**Conclusie:** TripTracker.API en Safari.API zijn **identieke implementaties**. Beide gebruiken AutoMapper met Profiles, DTOs zonder terugverwijzende navigation properties, en Repository Pattern.

---

## 12. Checklist voor examen

- [ ] Kan ik uitleggen wat een DTO is en waarom we het gebruiken?
- [ ] Kan ik het verschil tussen Entity en DTO uitleggen?
- [ ] Kan ik de one-to-many relatie tussen Trip en TripStop uitleggen?
- [ ] Kan ik uitleggen hoe foreign keys werken met `[ForeignKey]` attribute?
- [ ] Kan ik uitleggen wat AutoMapper doet en hoe je een Profile maakt?
- [ ] Kan ik het Repository Pattern uitleggen (interface vs implementation)?
- [ ] Kan ik uitleggen waarom Add/Update/Delete sync zijn en SaveChanges async?
- [ ] Kan ik uitleggen wat `Include()` doet (eager loading)?
- [ ] Kan ik een controller method uitleggen (GET, POST, PUT, DELETE)?
- [ ] Kan ik uitleggen wat `CreatedAtRoute` doet?
- [ ] Kan ik uitleggen wat Dependency Injection lifetimes zijn (Scoped, Transient, Singleton)?
- [ ] Kan ik de Program.cs configuratie uitleggen (DbContext, AutoMapper, Repositories, CORS)?
- [ ] Kan ik seed data uitleggen en wanneer je het gebruikt?
- [ ] Kan ik een migration uitleggen (Add-Migration, Update-Database)?
- [ ] Kan ik de API testen met Swagger?
- [ ] Kan ik de database bekijken in SQL Server Object Explorer?

---

## 13. Volgende stappen

**Voor MAUI integratie:**
- [ ] Ngrok opzetten om API te exposen
- [ ] ApiService bouwen in MAUI app (zoals SafariSnap)
- [ ] Models in MAUI app maken (met `ObservableObject`)
- [ ] ViewModels bouwen met MVVM pattern
- [ ] API calls doen vanuit ViewModels

**Zie:** Les 3 documentatie voor MAUI integratie patronen.

---

**Documentatie gegenereerd:** 2025-12-20
