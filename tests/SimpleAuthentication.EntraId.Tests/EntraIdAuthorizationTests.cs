using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace SimpleAuthentication.EntraId.Tests;

public class EntraIdAuthorizationTests
{
    private const string OtherSchemeName = "Bearer";
    private const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";

    [Theory]
    [InlineData("roles")]
    [InlineData(ClaimTypes.Role)]
    public async Task AppOnlyIdentityWithAllowedRole_IsAuthorizedAsync(string roleClaimType)
    {
        var identity = CreateIdentity(EntraIdDefaults.AuthenticationScheme, ("idtyp", "app"), (roleClaimType, "Orders.Write"), (roleClaimType, "Orders.Read"));

        Assert.True(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Read", "Orders.Admin"), identity));
    }

    [Fact]
    public async Task Roles_AreCaseSensitiveAsync()
    {
        var identity = CreateIdentity(EntraIdDefaults.AuthenticationScheme, ("idtyp", "app"), ("roles", "orders.read"));

        Assert.False(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Read"), identity));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("user", null)]
    [InlineData("APP", null)]
    [InlineData("app", "scp")]
    [InlineData("app", ScopeClaimType)]
    public async Task IdentityThatIsNotAppOnly_IsNotAuthorizedAsync(string? identityType, string? scopeClaimType)
    {
        var identity = CreateIdentity(EntraIdDefaults.AuthenticationScheme, ("roles", "Orders.Read"));

        if (identityType is not null)
        {
            identity.AddClaim(new("idtyp", identityType));
        }

        if (scopeClaimType is not null)
        {
            identity.AddClaim(new(scopeClaimType, "Orders.Read"));
        }

        Assert.False(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Read"), identity));
    }

    [Fact]
    public async Task IdentityOfAnotherScheme_IsNotAuthorizedAsync()
    {
        var identity = CreateIdentity(OtherSchemeName, ("idtyp", "app"), ("roles", "Orders.Read"));

        Assert.False(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Read"), identity));
    }

    [Fact]
    public async Task UnauthenticatedIdentity_IsNotAuthorizedAsync()
    {
        var identity = new ClaimsIdentity([new("idtyp", "app"), new("roles", "Orders.Read")]);

        Assert.False(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Read"), identity));
    }

    [Fact]
    public async Task ClaimsOfDifferentIdentities_AreNotCombinedAsync()
    {
        var entraIdIdentity = CreateIdentity(EntraIdDefaults.AuthenticationScheme, ("idtyp", "app"), ("roles", "Orders.Write"));
        var otherIdentity = CreateIdentity(OtherSchemeName, ("idtyp", "app"), ("roles", "Orders.Read"));

        Assert.False(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Read"), entraIdIdentity, otherIdentity));
        Assert.True(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Write"), otherIdentity, entraIdIdentity));
    }

    [Fact]
    public async Task CustomScheme_IsRequiredByPolicyAsync()
    {
        var identity = CreateIdentity("Partners", ("idtyp", "app"), ("roles", "Orders.Read"));

        Assert.True(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole(["Orders.Read"], "Partners"), identity));
        Assert.False(await AuthorizeAsync(policy => policy.RequireEntraIdApplicationRole("Orders.Read"), identity));
    }

    [Fact]
    public void Policy_AuthenticatesRequestsUsingEntraIdScheme()
    {
        var policy = new AuthorizationPolicyBuilder(OtherSchemeName, EntraIdDefaults.AuthenticationScheme)
            .RequireEntraIdApplicationRole("Orders.Read")
            .RequireEntraIdApplicationRole(["Orders.Write"], "Partners")
            .Build();

        string[] expectedSchemes = [OtherSchemeName, EntraIdDefaults.AuthenticationScheme, "Partners"];
        Assert.Equal(expectedSchemes, policy.AuthenticationSchemes);

        var requirements = policy.Requirements.OfType<EntraIdApplicationRoleRequirement>().ToList();
        Assert.Equal(2, requirements.Count);
        Assert.Equal(EntraIdDefaults.AuthenticationScheme, requirements[0].SchemeName);
        Assert.Equal("Orders.Read", Assert.Single(requirements[0].AllowedRoles));
        Assert.Equal("Partners", requirements[1].SchemeName);
        Assert.Equal("Orders.Write", Assert.Single(requirements[1].AllowedRoles));
    }

    [Fact]
    public void InvalidArguments_AreRejected()
    {
        var builder = new AuthorizationPolicyBuilder();

        Assert.Throws<ArgumentNullException>(() => EntraIdAuthorizationExtensions.RequireEntraIdApplicationRole(null!, "Orders.Read"));
        Assert.Throws<ArgumentNullException>(() => builder.RequireEntraIdApplicationRole(null!));
        Assert.Throws<ArgumentException>(() => builder.RequireEntraIdApplicationRole());
        Assert.Throws<ArgumentException>(() => builder.RequireEntraIdApplicationRole("Orders.Read", " "));
        Assert.Throws<ArgumentException>(() => builder.RequireEntraIdApplicationRole(["Orders.Read"], " "));

        Assert.Empty(builder.AuthenticationSchemes);
        Assert.Empty(builder.Requirements);

        var endpoint = Substitute.For<IEndpointConventionBuilder>();

        Assert.Throws<ArgumentNullException>(() => EntraIdAuthorizationExtensions.RequireEntraIdApplicationRole<IEndpointConventionBuilder>(null!, "Orders.Read"));
        Assert.Throws<ArgumentException>(() => endpoint.RequireEntraIdApplicationRole());
        Assert.Throws<ArgumentException>(() => endpoint.RequireEntraIdApplicationRole("Orders.Read", " "));

        Assert.Empty(endpoint.ReceivedCalls());
    }

    private static ClaimsIdentity CreateIdentity(string authenticationType, params (string Type, string Value)[] claims)
        => new(claims.Select(claim => new Claim(claim.Type, claim.Value)), authenticationType);

    private static async Task<bool> AuthorizeAsync(Action<AuthorizationPolicyBuilder> configurePolicy, params ClaimsIdentity[] identities)
    {
        var policyBuilder = new AuthorizationPolicyBuilder();
        configurePolicy(policyBuilder);

        await using var serviceProvider = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(identities), resource: null, policyBuilder.Build());
        return result.Succeeded;
    }
}
