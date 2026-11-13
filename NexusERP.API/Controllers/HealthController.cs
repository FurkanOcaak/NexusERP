using Microsoft.AspNetCore.Mvc;

namespace NexusERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", application = "NexusERP API" });
    }
}
