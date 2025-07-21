using Microsoft.AspNetCore.Mvc;
using QuizVerse.WebAPI.Helper;
using QuizVerse.WebAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

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
