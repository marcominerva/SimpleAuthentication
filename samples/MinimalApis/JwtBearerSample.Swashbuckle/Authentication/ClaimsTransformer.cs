using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace JwtBearerSample.Authentication;

public class ClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        var newClaim = new Claim(ClaimTypes.Version, "v1");
        identity!.AddClaim(newClaim);

        return Task.FromResult(principal);
    }
}