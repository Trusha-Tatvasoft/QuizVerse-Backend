using AutoMapper;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Infrastructure.Mappings;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserRequestDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Password, opt =>
            {
                opt.PreCondition(src =>
                    !string.IsNullOrWhiteSpace(src.Password) && (src.Id ?? 0) == 0);
                opt.MapFrom(src => src.Password);
            })
            .ForAllMembers(opts =>
            {
                opts.Condition((src, dest, srcMember, destMember, ctx) =>
                srcMember switch
                {
                    string str => !string.IsNullOrWhiteSpace(str),
                    _ => srcMember != null
                }
                );
            });

        CreateMap<User, UserRequestDto>();
        CreateMap<User, UserDto>();

         CreateMap<UserRegisterDto, User>()
            .ForMember(dest => dest.FirstTimeLogin, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => 2))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}