using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthentication.Permissions;

namespace JwtBearerSample.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PeopleController : ControllerBase
{
    [HttpGet]
    public IActionResult GetList() => NoContent();

    [Permission(Permissions.Read)]
    [HttpGet("{id:int}")]
    public IActionResult GetPerson(int id) => NoContent();

    [Permission(Permissions.Write)]
    [HttpPost]
    public IActionResult Insert() => NoContent();

    [HttpPut]
    public IActionResult Update() => NoContent();

    [Permission(Permissions.Write, Permissions.Admin)]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) => NoContent();
}

public static class Permissions
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Admin = "admin";
}