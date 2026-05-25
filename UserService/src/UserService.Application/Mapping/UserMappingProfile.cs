using AutoMapper;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Mapping;

/// <summary>
/// AutoMapper profile for User entity and DTOs.
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
        CreateMap<User, AuthTokenDto>().ReverseMap();
    }
}
