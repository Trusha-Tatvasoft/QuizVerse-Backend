using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Mappings;
using QuizVerse.Infrastructure.Repository;

namespace QuizVerse.WebAPI;

public static class ServiceCollectionExtensions
{
    public static void RegisterDependency(this IServiceCollection services)
    {
        //services
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICommonService, CommonService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IQuizDifficultyLevelService, QuizDifficultyLevelService>();
        services.AddScoped<IQuizCategoryService, QuizCategoryService>();
        services.AddScoped<IQuizManagementService, QuizManagementService>();
        services.AddScoped<IMemoryCacheService, MemoryCacheService>();
        services.AddScoped<IDropDownDataService, DropDownDataService>();
        services.AddScoped<IQuestionPoolService, QuestionPoolService>();
        services.AddScoped<IBattleManagementService, BattleManagementService>();
        services.AddScoped<IEmailTemplatesService, EmailTemplatesService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IPlatformConfigurationService, PlatformConfigurationService>();

        //mappers
        services.AddAutoMapper(typeof(MappingProfile));

        //repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped(typeof(ISqlQueryRepository), typeof(SqlQueryRepository));
    }

}
