using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Repository;
using QuizVerse.WebAPI.Configurations;
using QuizVerse.WebAPI.Helper;
using QuizVerse.WebAPI.Middlewares;
using QuizVerse.Infrastructure.Common;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuizVerseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ------------------ CORS ------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy(SystemConstants.CORS_POLICY_NAME, policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});


// ------------------ Repositories & Services ------------------
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomService, CustomService>();
builder.Services.AddScoped<ITokenService, TokenService>();


// Add services to the container.
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();

// for returning validation error in ApiResponse Formate
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    var isDev = builder.Environment.IsDevelopment();

    options.InvalidModelStateResponseFactory = context =>
        ValidationResponseHelper.CreateValidationErrorResponse(context, isDev);
});

// Register middlewares
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuizVerse API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = @"Bearer token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new List<string>()
        }
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(options => { });

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
