using CircuitBreakerPlayground.Helpers;
using CircuitBreakerPlayground.Model;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CircuitBreakerPlayground.Repositories
{
    public interface IWeatherForecastRepositories
    {
        Task<IEnumerable<WeatherForecast>> GetPrimaryWeatherForecast();
        Task<IEnumerable<WeatherForecast>> GetSecondaryWeatherForecast();

    }

    public class WeatherForecastRepositories : IWeatherForecastRepositories
    {
        public async Task<IEnumerable<WeatherForecast>> GetPrimaryWeatherForecast()
        {
            try
            {
                var response = await HttpHelper.GetAsync("https://localhost:7034/WeatherForecast/GetPrimaryWeatherForecast");
                return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(response);
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<WeatherForecast>> GetSecondaryWeatherForecast()
        {
            try
            {
                var response = await HttpHelper.GetAsync("https://localhost:7213/WeatherForecast/GetSecondaryWeatherForecast");
                return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(response);
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
