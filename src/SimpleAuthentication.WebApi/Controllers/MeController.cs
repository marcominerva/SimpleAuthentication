using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleAuthentication.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
        => Ok(new { username = User.Identity!.Name });
}
