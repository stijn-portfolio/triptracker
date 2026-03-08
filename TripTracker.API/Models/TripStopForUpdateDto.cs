using System.ComponentModel.DataAnnotations;

namespace TripTracker.API.Models
{
    public class TripStopForUpdateDto
    {
        [Required]
        public int TripId { get; set; }

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
