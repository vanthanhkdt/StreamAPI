using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task Stream([FromBody] StreamRequest request)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);

            for (int i = 1; i <= 1000; i++)
            {
                await writer.WriteAsync($"data: {request.Message} - Part {i}\n\n"); // Gửi từng phần
                await writer.FlushAsync(); // Đẩy dữ liệu ngay lập tức
                await Task.Delay(100); // Giả lập delay 1 giây
            }
        }


        [HttpGet]
        public async Task<IActionResult> Test()
        {
            return Json(new { m = "test" });
        }

        public class StreamRequest
        {
            public string Message { get; set; }
        }
    }
}
