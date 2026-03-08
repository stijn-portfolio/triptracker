using AutoMapper;
using TripTracker.API.Entities;
using TripTracker.API.Models;

namespace TripTracker.API.MappingProfiles
{
    public class TripStopProfile : Profile
    {
        public TripStopProfile()
        {
            CreateMap<TripStop, TripStopDto>(); // Entity → DTO
            CreateMap<TripStopForCreationDto, TripStop>(); // DTO → Entity (optioneel)
            CreateMap<TripStopForUpdateDto, TripStop>(); // DTO → Entity (optioneel)
        }
    }
}
