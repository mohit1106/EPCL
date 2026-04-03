using AutoMapper;
using StationService.Application.DTOs;
using StationService.Domain.Entities;

namespace StationService.Application.Mappings;

public class StationMappingProfile : Profile
{
    public StationMappingProfile()
    {
        CreateMap<Station, StationDto>()
            .ForMember(d => d.OperatingHoursStart, opt => opt.MapFrom(s => s.OperatingHoursStart.ToString("HH:mm")))
            .ForMember(d => d.OperatingHoursEnd, opt => opt.MapFrom(s => s.OperatingHoursEnd.ToString("HH:mm")))
            .ForMember(d => d.DistanceKm, opt => opt.Ignore());

        CreateMap<FuelType, FuelTypeDto>();
    }
}
