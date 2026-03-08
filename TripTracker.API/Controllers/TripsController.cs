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
        private readonly ITripStopRepository _tripStopRepository;
        private readonly IMapper _mapper;

        public TripsController(ITripRepository tripRepository, ITripStopRepository tripStopRepository, IMapper mapper)
        {
            _tripRepository = tripRepository;
            _tripStopRepository = tripStopRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips()
        {
            var tripsFromRepo = await _tripRepository.GetTripsAsync();
            return Ok(_mapper.Map<IEnumerable<TripDto>>(tripsFromRepo));
        }

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

        // Endpoint voor het ophalen van TripStops voor een specifieke Trip
        // Aangeroepen door: GET api/trips/{tripId}/tripstops
        [HttpGet("{tripId}/tripstops")]
        public async Task<ActionResult<IEnumerable<TripStopDto>>> GetTripStops(int tripId)
        {
            // Check of de trip bestaat
            var tripExists = await _tripRepository.GetTripAsync(tripId);
            if (tripExists == null)
            {
                return NotFound($"Trip with id {tripId} not found");
            }

            var tripStopsFromRepo = await _tripStopRepository.GetTripStopsAsync(tripId);
            return Ok(_mapper.Map<IEnumerable<TripStopDto>>(tripStopsFromRepo));
        }

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

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTrip(int id, [FromBody] TripForUpdateDto trip)
        {
            var tripFromRepo = await _tripRepository.GetTripAsync(id);

            if (tripFromRepo == null)
            {
                return NotFound();
            }

            _mapper.Map(trip, tripFromRepo);

            _tripRepository.UpdateTrip(tripFromRepo);

            await _tripRepository.SaveChangesAsync();

            return NoContent();
        }

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
