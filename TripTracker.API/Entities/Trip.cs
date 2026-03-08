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

        // Navigation property - One-to-Many relatie met TripStops
        public ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();
    }
}
