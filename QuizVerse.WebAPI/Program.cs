using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Domain.Data;
using QuizVerse.WebAPI;
using QuizVerse.WebAPI.Configurations;
using QuizVerse.WebAPI.Helper;
using QuizVerse.WebAPI.Middlewares;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuizVerseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//allow CORS for Angular app
builder.Services.AddCors(options =>
{
    options.AddPolicy(SystemConstants.CORS_POLICY_NAME, policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.RegisterDependency();
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

app.UseCors(SystemConstants.CORS_POLICY_NAME);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
