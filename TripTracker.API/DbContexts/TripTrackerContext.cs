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
