using Microsoft.AspNetCore.Mvc;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Route("weather-forecast")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecastDTO> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecastDTO
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Constants.Summaries[Random.Shared.Next(Constants.Summaries.Length)]
            })
            .ToArray();
        }
    }
}
