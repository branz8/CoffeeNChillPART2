using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public() => Ok("Anyone can see this.");

        [HttpGet("staff-only")]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult StaffOnly() => Ok("Welcome, Staff or Admin.");

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly() => Ok("Welcome, Admin.");
    }
}