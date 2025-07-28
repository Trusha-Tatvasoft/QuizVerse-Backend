using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Repository;

namespace QuizVerse.WebAPI;

public static class ServiceCollectionExtensions
{
    public static void RegisterDependency(this IServiceCollection services)
    {
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICommonService, CommonService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    }
}
