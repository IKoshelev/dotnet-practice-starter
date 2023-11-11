using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HomeController(
    ILogger<HomeController> logger): Controller
{
    // GET: /api/Home/
    [HttpGet]
    public IActionResult Get()
    {
        logger.LogInformation("Running index method, here is some int: {randomValue}", Random.Shared.Next());
        
        return Json(new
        {
            Claims = User?.Claims?.Select(x => new {x.Type, x.Value}).ToArray()
        }, 
        new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }

    // GET: /api/Home/Welcome/ 
    [HttpGet("welcome")]
    public IActionResult Welcome()
    {
        return Ok("This is the Welcome action method...");
    }
}