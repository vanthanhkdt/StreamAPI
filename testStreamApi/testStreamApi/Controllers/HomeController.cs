using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        public async Task GetStream_([FromBody] ChatRequest chatRequest)
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


        [HttpGet]
        public async Task GetStream([FromBody] ChatRequest chatRequest)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var client = _httpClientFactory.CreateClient();

            var conversationBuffer = new StringBuilder();
            string metaDataInfo = "";
            bool metaDataProcessed = false; // Chỉ xử lý MetaData 1 lần

            try
            {

                var apiRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5001/Home/Stream")
                {
                    Content = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var buffer = new StringBuilder();
                char[] chunk = new char[4096]; // Đọc 4KB mỗi lần

                while (!reader.EndOfStream)
                {
                    int bytesRead = await reader.ReadAsync(chunk, 0, chunk.Length);
                    if (bytesRead > 0)
                    {
                        buffer.Append(chunk, 0, bytesRead);

                        while (TryExtractJson(buffer, out string json))
                        {
                            var result = await HandleStreamEvent(json, metaDataInfo, metaDataProcessed, conversationBuffer);
                            metaDataInfo = result.metaDataInfo;
                            metaDataProcessed = result.metaDataProcessed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"{{\"error\": \"{ex.Message}\"}}\n\n");
                await Response.Body.FlushAsync();
            }
        }

        private static bool TryExtractJson(StringBuilder buffer, out string json)
        {
            json = null;
            string data = buffer.ToString();

            int openBraces = 0;
            int closeBraces = 0;
            int lastValidIndex = -1;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '{') openBraces++;
                if (data[i] == '}') closeBraces++;

                if (openBraces > 0 && openBraces == closeBraces)
                {
                    lastValidIndex = i;
                    break;
                }
            }

            if (lastValidIndex != -1)
            {
                json = data.Substring(0, lastValidIndex + 1);
                buffer.Remove(0, lastValidIndex + 1);
                return true;
            }

            return false;
        }

        private async Task<(string metaDataInfo, bool metaDataProcessed)> HandleStreamEvent(
            string json, string metaDataInfo, bool metaDataProcessed, StringBuilder conversationBuffer)
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<StreamEventData>(json);

                if (eventData == null) return (metaDataInfo, metaDataProcessed);

                if (!metaDataProcessed && eventData.Type == "MetaData")
                {
                    metaDataInfo = eventData.Data;
                    metaDataProcessed = true;
                    await Response.WriteAsync($"{JsonSerializer.Serialize(new { type = "MetaData", data = metaDataInfo })}\n\n");
                }
                else if (eventData.Type == "Tokens")
                {
                    conversationBuffer.Append(eventData.Data);
                    await Response.WriteAsync($"{JsonSerializer.Serialize(new { type = "Tokens", data = eventData.Data })}\n\n");
                }

                await Response.Body.FlushAsync();
            }
            catch (JsonException ex)
            {
                await Response.WriteAsync($"{{\"error\": \"JSON Error: {ex.Message}\"}}\n\n");
                await Response.Body.FlushAsync();
            }

            return (metaDataInfo, metaDataProcessed);
        }
    }

    public class StreamEventData
    {
        public string Data { get; set; }
        public string Type { get; set; }
    }

    public class ChatRequest
    {
        public string[] Messages { get; set; }
        public string ConversationId { get; set; }
    }

}
