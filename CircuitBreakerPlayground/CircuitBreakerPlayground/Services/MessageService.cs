using CircuitBreakerPlayground.Model;
using CircuitBreakerPlayground.Repositories;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CircuitBreakerPlayground.Services
{
    public interface IMessageService
    {
        //Task<string> GetHelloMessage();
        //Task<string> GetGoodbyeMessage();

        Task<IEnumerable<WeatherForecast>> WeatherForecast();
        Task<IEnumerable<WeatherForecast>> NewWeatherForecast();
    }

    public class MessageService : IMessageService
    {
        //private IMessageRepository _messageRepository;        
        //private AsyncRetryPolicy _retryPolicy;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Random Jitterer = new Random();
        private IWeatherForecastRepositories _weatherForecastRepositories;
        private AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private static readonly AsyncRetryPolicy<HttpResponseMessage> TransientErrorRetryPolicy =
            Policy.HandleResult<HttpResponseMessage>(
                message => ((int)message.StatusCode == 429 || (int)message.StatusCode >= 500))
                .WaitAndRetryAsync(2, retryAttempt =>
                {
                    Console.WriteLine($"Retrying because of transient error. Attempt {retryAttempt}");
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Jitterer.Next(0, 1000));
                });

        private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreakerPolicy =
            Policy.HandleResult<HttpResponseMessage>(message => (int) message.StatusCode >= 500)
            .AdvancedCircuitBreakerAsync(0.5,
                TimeSpan.FromMinutes(1),
                100,
                TimeSpan.FromMinutes(1));
        //.CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

        private readonly AsyncPolicyWrap<HttpResponseMessage> _resilientPolicy =
            CircuitBreakerPolicy.WrapAsync(TransientErrorRetryPolicy);

        public MessageService(IMessageRepository messageRepository, IWeatherForecastRepositories weatherForecastRepositories, IHttpClientFactory httpClientFactory)
        {
            //_messageRepository = messageRepository;

            //_retryPolicy = Policy
            //    .Handle<Exception>()
            //    .WaitAndRetryAsync(2, retryAttempt =>
            //    {
            //        var timeToWait = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
            //        Console.WriteLine($"Waiting {timeToWait.TotalSeconds} seconds");
            //        return timeToWait;
            //    }
            //    );

            _weatherForecastRepositories = weatherForecastRepositories;

            _circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(15),
                (ex, t) =>
                {
                    Console.WriteLine("Circuit broken!");
                },
                () =>
                {
                    Console.WriteLine("Circuit Reset!");
                });

            _httpClientFactory = httpClientFactory;
        }

        //public async Task<string> GetHelloMessage()
        //{
        //    return await _retryPolicy.ExecuteAsync<string>(async () => await _messageRepository.GetHelloMessage());
        //}

        //public async Task<string> GetGoodbyeMessage()
        //{
        //    try
        //    {
        //        Console.WriteLine($"Circuit State: {_circuitBreakerPolicy.CircuitState}");

        //        if (_circuitBreakerPolicy.CircuitState == CircuitState.Closed || _circuitBreakerPolicy.CircuitState == CircuitState.HalfOpen)
        //        {
        //            return await _circuitBreakerPolicy.ExecuteAsync<string>(async () =>
        //            {
        //                return await _messageRepository.GetGoodbyeMessage();
        //            });
        //        }
        //        else
        //        {
        //            return await _messageRepository.GetHelloMessage();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }
        //}

        public async Task<IEnumerable<WeatherForecast>> WeatherForecast()
        {
            try
            {
                
                Console.WriteLine($"Circuit State: {_circuitBreakerPolicy.CircuitState}");
                if (_circuitBreakerPolicy.CircuitState == CircuitState.Closed || _circuitBreakerPolicy.CircuitState == CircuitState.HalfOpen)
                {
                    return await _circuitBreakerPolicy.ExecuteAsync<IEnumerable<WeatherForecast>>(async () =>
                    {
                        return await _weatherForecastRepositories.GetPrimaryWeatherForecast();
                    });
                    
                }
                else
                {
                    return await _weatherForecastRepositories.GetSecondaryWeatherForecast();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Circuit State: {_circuitBreakerPolicy.CircuitState}");
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<WeatherForecast>> NewWeatherForecast()
        {
            try
            {
                if(CircuitBreakerPolicy.CircuitState == CircuitState.Open)
                {
                    throw new Exception("Service is currently unavailable");
                }
                var httpClient = _httpClientFactory.CreateClient();
                var response = await _resilientPolicy.ExecuteAsync(() => httpClient.GetAsync("https://localhost:7034/WeatherForecast/GetPrimaryWeatherForecast")));

                if(!response.IsSuccessStatusCode)
                {
                    throw new Exception("Service is currently unavailable");
                }

                var responseText = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(responseText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Circuit State: {_circuitBreakerPolicy.CircuitState}");
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
