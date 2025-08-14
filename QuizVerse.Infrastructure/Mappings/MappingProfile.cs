using System.Globalization;
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
        #region User Management
        // user create/update
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

        // user edit info fetch
        CreateMap<User, UserRequestDto>();

        // user list
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.AttemptedQuizzes,
                        opt => opt.MapFrom(src => src.QuizAttempteds.Count));

        CreateMap<User, UserExportDto>()
            .ForMember(dest => dest.TotalQuizAttemptedCount,
                opt => opt.MapFrom(src => src.QuizAttempteds.Count))
            .ForMember(dest => dest.RoleName,
                opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.StatusName,
                opt => opt.MapFrom(src => ((UserStatus)src.Status).ToString()))
            .ForMember(dest => dest.JoinDate,
                opt => opt.MapFrom(src => src.CreatedDate.ToString("dd-MM-yyyy")))
            .ForMember(dest => dest.LastActive,
                opt => opt.MapFrom(src =>
                    src.LastLogin.HasValue
                        ? src.LastLogin.Value.ToString("dd-MM-yyyy")
                        : "-"));
        #endregion

        #region Login/Registration User
        // user registration
        CreateMap<UserRegisterDto, User>()
           .ForMember(dest => dest.FirstTimeLogin, opt => opt.MapFrom(src => false))
           .ForMember(dest => dest.Status, opt => opt.MapFrom(src => UserStatus.Active))
           .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => UserRole.Player))
           .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
           .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));
        #endregion

        #region Quiz Difficulty Levels
        // quiz difficulty levels list
        CreateMap<QuizDifficulty, QuizDifficultyDTO>()
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(src => ToTitleCase(src.Name)))
            .ForMember(dest => dest.Description,
                opt => opt.MapFrom(src => CapitalizeFirst(src.Description)));

        CreateMap<QuizDifficultyRequestDto, QuizDifficulty>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Trim()));

        CreateMap<QuizDifficulty, CommonListDropDownDto>()
                    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => CapitalizeFirst(src.Name)));
        #endregion

        #region Quiz Category
        // quiz category to quiz category DTO
        CreateMap<QuizCategory, QuizCategoryDTO>()
            .ForMember(dest => dest.QuizCount, opt => opt.MapFrom(src => src.Quizzes != null ? src.Quizzes.Count : 0))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status));

        CreateMap<QuizCategory, CommonListDropDownDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => CapitalizeFirst(src.CategoryName)));
        #endregion

        #region Quiz Tag
        CreateMap<QuizTag, CommonListDropDownDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => CapitalizeFirst(src.TagName)));
        #endregion

        #region QuizManagement

        // CreateMap<Quiz, QuizListDto>()
        //    .ForMember(dest => dest.QuizTitle, opt => opt.MapFrom(src => src.Name))
        //    .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
        //    .ForMember(dest => dest.QuizDifficultyLevel, opt => opt.MapFrom(src => src.DifficultyLevel.Name))
        //    .ForMember(dest => dest.TotalQuestion, opt => opt.MapFrom(src => src.TotalQuestion))
        //    .ForMember(dest => dest.NoOfPersonAttempted, opt => opt.MapFrom(src => src.NoOfPersonAttempted))
        //    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
        //    .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate));

        #endregion

        #region Question Type
        CreateMap<QuestionType, CommonListDropDownDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => CapitalizeFirst(src.TypeName)));
        #endregion

        #region Question Difficulty
        CreateMap<QuestionDifficulty, CommonListDropDownDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => CapitalizeFirst(src.Name)));
        #endregion

        CreateMap<QuizCategory, QuizCategoryDTO>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status));
    }

    private static string ToTitleCase(string input) =>
      CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input?.ToLower() ?? string.Empty);

    private static string CapitalizeFirst(string input) =>
        string.IsNullOrWhiteSpace(input) ? string.Empty
            : char.ToUpper(input[0]) + input[1..];
}
