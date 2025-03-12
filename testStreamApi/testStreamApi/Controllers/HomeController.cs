using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using testStreamApi.Models;

namespace testStreamApi.Controllers
{
    //    public class HomeController : Controller
    //    {
    //        private readonly ILogger<HomeController> _logger;

    //        public HomeController(ILogger<HomeController> logger)
    //        {
    //            _logger = logger;
    //        }

    //        public IActionResult Index()
    //        {
    //            return View();
    //        }

    //        public IActionResult Privacy()
    //        {
    //            return View();
    //        }

    //        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    //        public IActionResult Error()
    //        {
    //            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    //        }
    //    }

    using Microsoft.AspNetCore.Mvc;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    [Controller]
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task GetStream([FromBody] ChatRequest chatRequest)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var client = _httpClientFactory.CreateClient();
            var apiRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5001/Home/Stream")
            {
                Content = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var writer = Response.BodyWriter;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var data = Encoding.UTF8.GetBytes($"data: {line}\n\n");
                    await writer.WriteAsync(data);
                    await writer.FlushAsync();
                }
            }
        }
    }

    public class ChatRequest
    {
        public string[] Messages { get; set; }
        public string ConversationId { get; set; }
    }

}
