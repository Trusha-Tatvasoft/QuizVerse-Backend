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

        CreateMap<User, UserDto>()
            .ForMember(dest => dest.AttemptedQuizzes,
                        opt => opt.MapFrom(src => src.QuizAttempteds.Count));

    }
}