using AutoMapper;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using UserRole = QuizVerse.Infrastructure.Enums.UserRoles;

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

        CreateMap<User, UserDto>()
            .ForMember(dest => dest.AttemptedQuizzes,
                        opt => opt.MapFrom(src => src.QuizAttempteds.Count));


        CreateMap<UserRegisterDto, User>()
           .ForMember(dest => dest.FirstTimeLogin, opt => opt.MapFrom(src => false))
           .ForMember(dest => dest.Status, opt => opt.MapFrom(src => UserStatus.Active))
           .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => UserRole.Player))
           .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
           .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<User, UserExportDto>()
            .ForMember(dest => dest.TotalQuizAttemptedCount,
                    opt => opt.MapFrom(src => src.QuizAttempteds.Count))
            .ForMember(dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.StatusName,
                    opt => opt.MapFrom(src => ((UserStatus)src.Status).ToString()))
            .ForMember(dest => dest.JoinDate,
                    opt => opt.MapFrom(src => src.CreatedDate)) 
            .ForMember(dest => dest.LastActive,
                    opt => opt.MapFrom(src => src.LastLogin));

    }
}