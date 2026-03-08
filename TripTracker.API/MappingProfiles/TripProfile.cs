using AutoMapper;
using TripTracker.API.Entities;
using TripTracker.API.Models;

namespace TripTracker.API.MappingProfiles
{
    public class TripProfile : Profile
    {
        public TripProfile()
        {
            CreateMap<Trip, TripDto>(); // Entity → DTO
            CreateMap<TripForCreationDto, Trip>(); // DTO → Entity (optioneel)
            CreateMap<TripForUpdateDto, Trip>(); // DTO → Entity (optioneel)
        }
    }
}
