using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HomeController: Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // GET: /Home/
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Running index method, here is some int: {randomValue}", Random.Shared.Next());
        
        return Ok("This is my default action... " + System.Text.Json.JsonSerializer.Serialize(new
        {
            Claims = User?.Claims?.Select(x => new {x.Type, x.Value})
        }, 
        new JsonSerializerOptions()
        {
            WriteIndented = true
        }));
    }

    // GET: /Home/Welcome/ 
    [HttpGet("welcome")]
    public IActionResult Welcome()
    {
        return Ok("This is the Welcome action method...");
    }
}