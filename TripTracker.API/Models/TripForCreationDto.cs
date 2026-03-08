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
