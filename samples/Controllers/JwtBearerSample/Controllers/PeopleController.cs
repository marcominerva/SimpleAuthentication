using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthentication.Permissions;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtBearerSample.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PeopleController : ControllerBase
{
    [Permissions(Permissions.PeopleRead, Permissions.PeopleAdmin)]
    [HttpGet]
    [SwaggerOperation(description: $"This endpoint requires the '{Permissions.PeopleRead}' or '{Permissions.PeopleAdmin}' permissions")]
    public IActionResult GetList() => NoContent();

    [Permissions(Permissions.PeopleRead, Permissions.PeopleAdmin)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(description: $"This endpoint requires the '{Permissions.PeopleRead}' or '{Permissions.PeopleAdmin}' permissions")]
    public IActionResult GetPerson(int id) => NoContent();

    [Permissions(Permissions.PeopleWrite)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(description: $"This endpoint requires the '{Permissions.PeopleWrite}' permission")]
    public IActionResult Insert() => NoContent();

    [Permissions(Permissions.PeopleWrite)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(description: $"This endpoint requires the '{Permissions.PeopleWrite}' permission")]
    public IActionResult Update() => NoContent();

    [Permissions(Permissions.PeopleAdmin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(description: $"This endpoint requires the '{Permissions.PeopleAdmin}' permission")]
    public IActionResult Delete(int id) => NoContent();
}

public static class Permissions
{
    public const string PeopleRead = "people:read";
    public const string PeopleWrite = "people:write";
    public const string PeopleAdmin = "people:admin";
}