using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Repository;

namespace QuizVerse.WebAPI.Helper;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomService, CustomService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        return services;
    }
}
