using CircuitBreakerPlayground.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CircuitBreakerPlayground.Controllers
{
    [Route("message")]
    public class MessageController : Controller
    {
        private IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("GetWeatherForecast")]
        public async Task<IActionResult> WeatherForecast()
        {
            var result = await _messageService.WeatherForecast();
            return Ok(result);
        }

        [HttpGet("NewWeatherForecast")]
        public async Task<IActionResult> NewWeatherForecast()
        {
            var result = await _messageService.NewWeatherForecast();
            return Ok(result);
        }

        //[HttpGet("hello")]
        //public async Task<IActionResult> GetHello()
        //{
        //    var result = await _messageService.GetHelloMessage();
        //    return Ok(result);
        //}

        //[HttpGet("goodbye")]
        //public async Task<IActionResult> GetGoodbye()
        //{
        //    var result = await _messageService.GetGoodbyeMessage();
        //    return Ok(result);
        //}
    }
}
