using System.Globalization;
using AutoMapper;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs;
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
        CreateMap<QuizListDto, QuizListDto>()
            .ForMember(dest => dest.QuizTitle, opt => opt.MapFrom(src => ToTitleCase(src.QuizTitle)))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => ToTitleCase(src.CategoryName)))
            .ForMember(dest => dest.QuizDifficultyLevel, opt => opt.MapFrom(src => CapitalizeFirst(src.QuizDifficultyLevel)));


        #endregion

        #region Question Type
        CreateMap<QuestionType, CommonListDropDownDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => CapitalizeFirst(src.TypeName)));
        #endregion

        #region Question Difficulty
        CreateMap<QuestionDifficulty, CommonListDropDownDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => CapitalizeFirst(src.Name)));
        #endregion

        #region Question Mapping
        CreateMap<QuizCategory, QuizCategoryDTO>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status));

        CreateMap<QuestionRequestDTO, BaseQuestion>()
            .ForMember(dest => dest.QueText,
                opt => opt.MapFrom(src => src.QuestionText.Trim()))
            .ForMember(dest => dest.CategoryId,
                opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.QueDifficultyId,
                opt => opt.MapFrom(src => src.DifficultyId))
            .ForMember(dest => dest.QueTypeId,
                opt => opt.MapFrom(src => src.QuestionTypeId))
            .ForMember(dest => dest.CreatedBy,
                opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate,
                opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy,
                opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,
                opt => opt.Ignore());

        CreateMap<BaseQuestion, QuestionDetailDTO>()
            .ForMember(dest => dest.QuestionText,
                opt => opt.MapFrom(src => src.QueText))
            .ForMember(dest => dest.QuestionType,
                opt => opt.MapFrom(src => src.QueType.TypeName))
            .ForMember(dest => dest.Difficulty,
                opt => opt.MapFrom(src => src.QueDifficulty.Name))
            .ForMember(dest => dest.Category,
                opt => opt.MapFrom(src => src.Category.CategoryName))
            .ForMember(dest => dest.Options,
                opt => opt.Ignore())
            .ForMember(dest => dest.CorrectAnswer,
                opt => opt.Ignore());

        CreateMap<QuestionsListRequestDto, BaseQuestion>()
            .ForMember(dest => dest.Id,
                opt => opt.Ignore())
            .ForMember(dest => dest.QueText,
                opt => opt.MapFrom(src => src.QueText))
            .ForMember(dest => dest.CategoryId,
                opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.QueDifficultyId,
                opt => opt.MapFrom(src => src.QueDifficultyId))
            .ForMember(dest => dest.QueTypeId,
                opt => opt.MapFrom(src => src.QueTypeId))
            .ForMember(dest => dest.CreatedBy,
                opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate,
                opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy,
                opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,
                opt => opt.Ignore());

        CreateMap<QueOptionsAndAnswersDto, QuestionOptionsAnswer>()
            .ForMember(dest => dest.Id,
                opt => opt.Ignore())
            .ForMember(dest => dest.QuestionId,
                opt => opt.Ignore())
            .ForMember(dest => dest.Key,
                opt => opt.MapFrom(src => src.Key))
            .ForMember(dest => dest.Value,
                opt => opt.MapFrom(src => src.Value))
            .ForMember(dest => dest.CreatedBy,
                opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate,
                opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy,
                opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,
                opt => opt.Ignore());
        #endregion

        #region BattleManagement
        CreateMap<BattleManagementData, BattleManagementData>()
            .ForMember(dest => dest.BattleDifficulty, opt => opt.MapFrom(src => CapitalizeFirst(src.BattleDifficulty)))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => CapitalizeFirst(src.CategoryName)))
            .ForMember(dest => dest.BattleName, opt => opt.MapFrom(src => CapitalizeFirst(src.BattleName)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => ToTitleCase(src.Description)));
        #endregion

        #region Email Templates
        CreateMap<EmailTemplete, EmailTemplatesResponseDto>();
        #endregion

        #region User Dashboard
        CreateMap<RawUserDashboardMetricsDTO, UserDashboardResponse>()
            .ForMember(dest => dest.QuizzesCompleted,
                opt => opt.MapFrom(src => src.QuizzesCompleted))
            .ForMember(dest => dest.TotalXp,
                opt => opt.MapFrom(src => src.TotalXp))
            .ForMember(dest => dest.WinRate,
                opt => opt.MapFrom(src => Math.Round((double)src.WinRate, 2)))
            .ForMember(dest => dest.CurrentRank,
                opt => opt.MapFrom(src => src.CurrentRank));
                
        CreateMap<RawRankProgressDTO, RankProgressDTO>()
            .ForMember(dest => dest.CurrentRank,
                opt => opt.MapFrom(src => src.CurrentRank))
            .ForMember(dest => dest.NextRank,
                opt => opt.MapFrom(src => src.NextRank))
            .ForMember(dest => dest.XpNeeded,
                opt => opt.MapFrom(src => src.XpNeeded))
            .ForMember(dest => dest.ProgressPercent,
                opt => opt.MapFrom(src => src.ProgressPercent));
        #endregion
    }

    private static string ToTitleCase(string input) =>
      CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input?.ToLower() ?? string.Empty);

    private static string CapitalizeFirst(string input) =>
        string.IsNullOrWhiteSpace(input) ? string.Empty
            : char.ToUpper(input[0]) + input[1..];
}
