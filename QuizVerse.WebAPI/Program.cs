using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Repository;
using QuizVerse.WebAPI.Helper;
using QuizVerse.WebAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuizVerseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ------------------ CORS ------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
    });
});


// ------------------ Repositories & Services ------------------
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();


// Add services to the container.

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
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(options => { });

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
