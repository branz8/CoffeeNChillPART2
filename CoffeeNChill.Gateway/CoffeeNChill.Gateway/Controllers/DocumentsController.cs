using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _functionsUrl = "http://localhost:7071/api/documents";

        public DocumentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Admin,Staff")] // Only Admins and Staff can upload
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided.");

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);

            content.Add(streamContent, "file", file.FileName);

            var response = await _httpClient.PostAsync($"{_functionsUrl}/upload", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, responseContent);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")] // Only Admins and Staff can list
        public async Task<IActionResult> List()
        {
            var response = await _httpClient.GetAsync(_functionsUrl);
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        [HttpGet("download/{fileName}")]
        [Authorize(Roles = "Admin,Staff")] // Only Admins and Staff can download
        public async Task<IActionResult> Download(string fileName)
        {
            var response = await _httpClient.GetAsync($"{_functionsUrl}/download/{fileName}");
            if (!response.IsSuccessStatusCode) return NotFound("File not found.");

            var content = await response.Content.ReadAsStreamAsync();
            return File(content, "application/octet-stream", fileName);
        }
    }
}