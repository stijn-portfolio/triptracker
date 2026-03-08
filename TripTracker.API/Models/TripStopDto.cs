namespace TripTracker.API.Models
{
    public class TripStopDto
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        // Trip property verwijderd - voorkomt circular references
        // App gebruikt alleen TripId, niet het hele Trip object
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
