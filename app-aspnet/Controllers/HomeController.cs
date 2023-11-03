using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;

[Authorize]
public class HomeController: Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // GET: /Home/
    public IActionResult Index()
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
    public IActionResult Welcome()
    {
        return Ok("This is the Welcome action method...");
    }
}