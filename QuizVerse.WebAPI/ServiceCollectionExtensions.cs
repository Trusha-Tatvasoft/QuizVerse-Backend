using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Repository;

namespace QuizVerse.WebAPI;

public static class ServiceCollectionExtensions
{
    public static void RegisterDependency(this IServiceCollection services)
    {
        //services
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomService, CustomService>();
        services.AddScoped<IEmailClient, SmtpEmailClient>();

        //repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
    }
}
