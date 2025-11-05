using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Mappings;
using QuizVerse.Infrastructure.Repository;
using QuizVerse.WebAPI.Notifications;

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
        services.AddScoped<IUserDashboardService, UserDashboardService>();
        services.AddScoped<IQuestionDifficultyService, QuestionDifficultyService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IPlatformConfigurationService, PlatformConfigurationService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IUserBattlesService, UserBattlesService>();
        services.AddScoped<IBrowseQuizzesService, BrowseQuizzesService>();
        services.AddScoped<IAiService, AiService>();
        services.AddScoped<IBattleMatchmakingService, BattleMatchmakingService>();
        services.AddScoped<IBattleService, BattleService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddScoped<IUserActivityCheckerService, UserActivityCheckerService>();
        services.AddScoped<IContentModerationService,ContentModerationService>();
        services.AddScoped<IGroqModelRotationService, GroqModelRotationService>();
        services.AddScoped<IGroqService, GroqService>();
        services.AddScoped<IGroqContentValidatorService, GroqContentValidatorService>();
        services.AddScoped<IAiQuestionGenerationService, AiQuestionGenerationService>();
        services.AddScoped<IQuizCommentSectionService, QuizCommentSectionService>();
        services.AddScoped<IAiLogService, AiLogService>();
        services.AddScoped<IAiConfigurationService, AiConfigurationService>();
        services.AddScoped<IGeminiModelService, GeminiModelService>();
        services.AddScoped<IFetchContentFromUrlService, FetchContentFromUrlService>();

        services.AddHttpClient<IPerspectiveApiService, PerspectiveApiService>();
        services.AddSingleton<IGcpApiQueueService, GcpApiQueueService>();
        services.AddHostedService<PerspectiveQueueHostedService>();
        
        //mappers
        services.AddAutoMapper(typeof(MappingProfile));

        //repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped(typeof(ISqlQueryRepository), typeof(SqlQueryRepository));

        services.AddSingleton<IMatchmakingQueueRepository, MatchmakingQueueRepository>();

        //Ai Clients
        services.AddScoped<IGeminiWebsiteSafetyClient, GeminiWebsiteSafetyClient>();
    }

}
