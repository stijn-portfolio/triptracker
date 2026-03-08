using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TripTracker.API.Entities;
using TripTracker.API.Models;
using TripTracker.API.Services;

namespace TripTracker.API.Controllers
{
    [ApiController]
    [Route("api/tripstops")]
    public class TripStopsController : ControllerBase
    {
        private readonly ITripStopRepository _tripStopRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IMapper _mapper;

        public TripStopsController(
            ITripStopRepository tripStopRepository,
            ITripRepository tripRepository,
            IMapper mapper)
        {
            _tripStopRepository = tripStopRepository;
            _tripRepository = tripRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripStopDto>>> GetAllTripStops()
        {
            var tripStopsFromRepo = await _tripStopRepository.GetAllTripStopsAsync();
            return Ok(_mapper.Map<IEnumerable<TripStopDto>>(tripStopsFromRepo));
        }

        [HttpGet("{id}", Name = "GetTripStop")]
        public async Task<ActionResult<TripStopDto>> GetTripStop(int id)
        {
            var tripStopFromRepo = await _tripStopRepository.GetTripStopAsync(id);

            if (tripStopFromRepo == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<TripStopDto>(tripStopFromRepo));
        }

        [HttpPost]
        public async Task<ActionResult<TripStopDto>> CreateTripStop([FromBody] TripStopForCreationDto tripStop)
        {
            if (tripStop == null)
            {
                return BadRequest();
            }

            var tripStopEntity = _mapper.Map<TripStop>(tripStop);

            _tripStopRepository.AddTripStop(tripStopEntity);
            await _tripStopRepository.SaveChangesAsync();

            // Recalculate trip dates based on all stops (AFTER saving new stop!)
            await RecalculateTripDatesAsync(tripStopEntity.TripId);
            await _tripStopRepository.SaveChangesAsync();

            var tripStopToReturn = _mapper.Map<TripStopDto>(tripStopEntity);

            return CreatedAtRoute(
                "GetTripStop",
                new { id = tripStopToReturn.Id },
                tripStopToReturn);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTripStop(int id, [FromBody] TripStopForUpdateDto tripStop)
        {
            var tripStopFromRepo = await _tripStopRepository.GetTripStopAsync(id);

            if (tripStopFromRepo == null)
            {
                return NotFound();
            }

            _mapper.Map(tripStop, tripStopFromRepo);

            _tripStopRepository.UpdateTripStop(tripStopFromRepo);
            await _tripStopRepository.SaveChangesAsync();

            // Recalculate trip dates based on all stops (AFTER saving updated stop!)
            await RecalculateTripDatesAsync(tripStopFromRepo.TripId);
            await _tripStopRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTripStop(int id)
        {
            var tripStopFromRepo = await _tripStopRepository.GetTripStopAsync(id);

            if (tripStopFromRepo == null)
            {
                return NotFound();
            }

            // Save tripId before deleting
            var tripId = tripStopFromRepo.TripId;

            _tripStopRepository.DeleteTripStop(tripStopFromRepo);
            await _tripStopRepository.SaveChangesAsync();

            // Recalculate trip dates based on remaining stops
            await RecalculateTripDatesAsync(tripId);
            await _tripStopRepository.SaveChangesAsync();

            return NoContent();
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Recalculates trip StartDate/EndDate based on MIN/MAX of all stops.
        /// </summary>
        private async Task RecalculateTripDatesAsync(int tripId)
        {
            var trip = await _tripRepository.GetTripAsync(tripId);
            if (trip == null) return;

            var stops = await _tripStopRepository.GetTripStopsAsync(tripId);

            if (!stops.Any())
            {
                // Geen stops → behoud originele trip dates
                return;
            }

            var minDate = stops.Min(s => s.DateTime.Date);
            var maxDate = stops.Max(s => s.DateTime.Date);

            bool updated = false;

            // Update StartDate als anders dan MIN
            if (trip.StartDate.Date != minDate)
            {
                trip.StartDate = minDate;
                updated = true;
            }

            // Update EndDate als anders dan MAX
            if (!trip.EndDate.HasValue || trip.EndDate.Value.Date != maxDate)
            {
                trip.EndDate = maxDate;
                updated = true;
            }

            if (updated)
            {
                _tripRepository.UpdateTrip(trip);
            }
        }
    }
}
