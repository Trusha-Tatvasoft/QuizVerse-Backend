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
    options.UseNpgsql(builder.Configuration.GetConnectionString(SystemConstants.DB_CONNECTION_STRING_NAME)));

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
builder.Services.AddHttpContextAccessor();

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
    c.SwaggerDoc(SystemConstants.SYSTEM_VERSION, new OpenApiInfo { Title = SystemConstants.SWAGGER_PAGE_TITLE, Version = SystemConstants.SYSTEM_VERSION });

    c.AddSecurityDefinition(SystemConstants.SCEURITY_SCHEME, new OpenApiSecurityScheme
    {
        Name = SystemConstants.JWT_ACCESS_TOKEN_HEADER_NAME,
        Type = SecuritySchemeType.ApiKey,
        Scheme = SystemConstants.SCEURITY_SCHEME,
        BearerFormat = SystemConstants.BEARER_FORMAT,
        In = ParameterLocation.Header,
        Description = SystemConstants.HEADER_TOKEN_DESCRIPTION
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SystemConstants.SCEURITY_SCHEME }
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
