# Simple Authentication for ASP.NET Core

[![Lint Code Base](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/linter.yml/badge.svg)](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/linter.yml)
[![CodeQL](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/github-code-scanning/codeql)
[![Nuget](https://img.shields.io/nuget/v/SimpleAuthenticationTools)](https://www.nuget.org/packages/SimpleAuthenticationTools)
[![Nuget](https://img.shields.io/nuget/dt/SimpleAuthenticationTools)](https://www.nuget.org/packages/SimpleAuthenticationTools)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/marcominerva/SimpleAuthentication/blob/master/LICENSE)

A library to easily integrate Authentication in ASP.NET Core projects. Currently it supports JWT Bearer, API Key and Basic Authentication in both Controller-based and Minimal API projects.

> [!IMPORTANT]
> **Update from Version 2.x to 3.x**
> Swashbuckle (Swagger) support has been moved out from SimpleAuthentication. If you're using the `AddSimpleAuthentication` extension method with `AddSwaggerGen`, now you need to install the [SimpleAuthentication.Swashbuckle](https://github.com/marcominerva/SimpleAuthentication/tree/master/src/SimpleAuthentication.Swashbuckle) package.

## Installation

The library is available on [NuGet](https://www.nuget.org/packages/SimpleAuthenticationTools). Search for *SimpleAuthenticationTools* in the **Package Manager GUI** or run the following command in the **.NET CLI**:

```shell
dotnet add package SimpleAuthenticationTools
```
## Usage video

Take a look at a quick demo showing how to integrate the library:

[![Simple Authentication for ASP.NET Core](https://raw.githubusercontent.com/marcominerva/SimpleAuthentication/master/Screenshot.jpg)](https://www.youtube.com/watch?v=SVZuaPE2yNc)

## Configuration

Authentication can be fully configured adding an _Authentication_ section in the _appsettings.json_ file:

```
"Authentication": {
    "DefaultScheme": "Bearer", // Optional
    "JwtBearer": {
        "SchemeName": "Bearer" // Default: Bearer
        //"NameClaimType": "user_name", // Default: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name
        //"RoleClaimType": "user_role", // Default: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
        "SecurityKey": "supersecretsecuritykey42!", // Required
        "Algorithm": "HS256", // Default: HS256
        "Issuers": [ "issuer" ], // Optional
        "Audiences": [ "audience" ], // Optional
        "ExpirationTime": "01:00:00", // Default: No expiration
        "ClockSkew": "00:02:00" // Default: 5 minutes
    },
    "ApiKey": {
        "SchemeName": "ApiKey", // Default: ApiKey
        // You can specify either HeaderName, QueryStringKey or both
        "HeaderName": "x-api-key",
        "QueryStringKey": "code",
        //"NameClaimType": "user_name", // Default: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name
        //"RoleClaimType": "user_role", // Default: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
        // Uncomment this line if you want to validate the API Key against a fixed value.
        // Otherwise, you need to register an IApiKeyValidator implementation that will be used
        // to validate the API Key.
        //"ApiKeyValue": "f1I7S5GXa4wQDgLQWgz0",
        "UserName": "ApiUser" // Required if ApiKeyValue is used
    },
    "Basic": {
        "SchemeName": "Basic", // Default: Basic
        //"NameClaimType": "user_name", // Default: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name
        //"RoleClaimType": "user_role", // Default: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
        // Uncomment the following lines if you want to validate user name and password
        // against fixed values.
        // Otherwise, you need to register an IBasicAuthenticationValidator implementation
        // that will be used to validate the credentials.
        //"UserName": "marco",
        //"Password": "P@$$w0rd"
    }
}
```

You can configure only the kind of authentication you want to use, or you can include all of them.

The _DefaultScheme_ attribute is used to specify what kind of authentication must be configured as default. Allowed values are the values of the _SchemeName_ attributes.

**Registering authentication at Startup**

```csharp
using SimpleAuthentication;

var builder = WebApplication.CreateBuilder(args);

// ...
// Registers authentication schemes and services using IConfiguration information (see above).
builder.Services.AddSimpleAuthentication(builder.Configuration);

// ...

var app = builder.Build();

//...
// The following middlewares aren't strictly necessary in .NET 7.0 or higher, because they are automatically
// added when detecting that the corresponding services have been registered. However, you may
// need to call them explicitly if the default middlewares configuration is not correct for your
// app, for example when you need to use CORS.
// Check https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/middleware
// for more information.
//app.UseAuthentication();
//app.UseAuthorization();

//...

app.Run();
```

**Integrating with Swashbuckle**

If you're using Swashbuckle (Swagger) to document your API, you can integrate the authentication configuration with the Swagger documentation. Search for *SimpleAuthenticationTools.Swashbuckle* in the **Package Manager GUI** or run the following command in the **.NET CLI**:

```shell
dotnet add package SimpleAuthenticationTools.Swashbuckle
```

Then, you can use the `AddSimpleAuthentication` extension method:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // ...
    // Add this line to integrate authentication with Swagger.
    options.AddSimpleAuthentication(builder.Configuration);
});
```

**Integrating with Microsoft.AspNetCore.OpenApi (.NET 9 or later)**

Starting from version 9, .NET offers built-in support for OpenAPI. If you're using the `AddOpenApi` extension method to provide OpenAPI support, you just need to add the corresponding extension method in its declaration (no extra package required):

```csharp
builder.Services.AddOpenApi(options =>
{
    // ...
    // Add this line to integrate authentication with OpenAPI.
    options.AddSimpleAuthentication(builder.Configuration);
});
```

**Creating a JWT Bearer**

When using JWT Bearer authentication, an implementation of the [IJwtBearerService](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication.Abstractions/JwtBearer/IJwtBearerService.cs) interface is automatically registered to create a valid JWT Bearer, according to the settings you have specified in the _appsettings.json_ file:

```csharp
app.MapPost("api/auth/login", async (LoginRequest loginRequest, IJwtBearerService jwtBearerService) =>
{
    // Check for login rights...

    // Add custom claims (optional).
    var claims = new List<Claim>
    {
        new(ClaimTypes.GivenName, "Marco"),
        new(ClaimTypes.Surname, "Minerva")
    };

    var token = await jwtBearerService.CreateTokenAsync(loginRequest.UserName, claims);
    return TypedResults.Ok(new LoginResponse(token));
});

public record class LoginRequest(string UserName, string Password);

public record class LoginResponse(string Token);
```

The [IJwtBearerService.CreateToken](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication.Abstractions/JwtBearer/IJwtBearerService.cs#L23) method allows to specify the issuer and the audience of the token. If you don't specify any value, the first ones defined in _appsettings.json_ will be used.

**Supporting multiple API Keys/Basic Authentication credentials**

When using API Key or Basic Authentication, you can specify multiple fixed values for authentication:

```json
"Authentication": {
    "ApiKey": {
        "ApiKeys": [
            {
                "Value": "key-1",
                "UserName": "UserName1"
            },
            {
                "Value": "key-2",
                "UserName": "UserName2"
            }
        ]
    },
    "Basic": {
        "Credentials": [
            {
                "UserName": "UserName1",
                "Password": "Password1"
            },
            {
                "UserName": "UserName2",
                "Password": "Password2"
            }
        ]
    }
}
```

With this configuration, authentication will succeed if any of these credentials are provided.

**Custom Authentication logic for API Keys and Basic Authentication**

If you need to implement custom authentication logic, for example validating credentials with dynamic values and adding claims to identity, you can omit all the credentials in the _appsettings.json_ file and then provide an implementation of [IApiKeyValidator.cs](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication.Abstractions/ApiKey/IApiKeyValidator.cs) or [IBasicAuthenticationValidator.cs](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication.Abstractions/BasicAuthentication/IBasicAuthenticationValidator.cs):

```csharp
builder.Services.AddTransient<IApiKeyValidator, CustomApiKeyValidator>();
builder.Services.AddTransient<IBasicAuthenticationValidator, CustomBasicAuthenticationValidator>();
//...

public class CustomApiKeyValidator : IApiKeyValidator
{
    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey)
    {
        var result = apiKey switch
        {
            "ArAilHVOoL3upX78Cohq" => ApiKeyValidationResult.Success("User 1"),
            "DiUU5EqImTYkxPDAxBVS" => ApiKeyValidationResult.Success("User 2"),
            _ => ApiKeyValidationResult.Fail("Invalid User")
        };

        return Task.FromResult(result);
    }
}

public class CustomBasicAuthenticationValidator : IBasicAuthenticationValidator
{
    public Task<BasicAuthenticationValidationResult> ValidateAsync(string userName, string password)
    {
        if (userName == password)
        {
            var claims = new List<Claim>() { new(ClaimTypes.Role, "User") };
            return Task.FromResult(BasicAuthenticationValidationResult.Success(userName, claims));
        }

        return Task.FromResult(BasicAuthenticationValidationResult.Fail("Invalid user"));
    }
}
```

## Permission-based authorization

The library provides services for adding permission-based authorization to an ASP.NET Core project. Just use the following registration at startup:

```csharp
// Enable permission-based authorization.
builder.Services.AddPermissions<T>();
```

The **AddPermissions** extension method requires an implementation of the [IPermissionHandler interface](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication.Abstractions/Permissions/IPermissionHandler.cs), which is responsible to check if the user owns the required permissions:

```csharp
public interface IPermissionHandler
{
    Task<bool> IsGrantedAsync(ClaimsPrincipal user, IEnumerable<string> permissions);
}
```

The library provides the built-in [ScopeClaimPermissionHandler class](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication/Permissions/ScopeClaimPermissionHandler.cs), that checks for permissions reading the default **scope** claims of the current user (_scp_ or _http://schemas.microsoft.com/identity/claims/scope_). To use this default handler, we can just write this:

```csharp
builder.Services.AddScopePermissions(); 
// The line above is equivalent to builder.Services.AddPermissions<ScopeClaimPermissionHandler>();
```

Based on the scenario, we can provide our own implementation, for example reading different claims or using external services (database, HTTP calls, etc.) to get user permissions.

Then, just use the [PermissionAttribute](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication.Abstractions/Permissions/PermissionAttribute.cs) or the [RequirePermission](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication/PermissionAuthorizationExtensions.cs#L98) extension method:

```csharp
// In a Controller
[Permission("profile")]
public ActionResult<User> Get() => new User(User.Identity!.Name);

// In a Minimal API
app.MapGet("api/me", (ClaimsPrincipal user) =>
{
    return TypedResults.Ok(new User(user.Identity!.Name));
})
.RequirePermission("profile")
```

With the [ScopeClaimPermissionHandler](https://github.com/marcominerva/SimpleAuthentication/blob/master/src/SimpleAuthentication/Permissions/ScopeClaimPermissionHandler.cs) mentioned above, the invocation succeeds if the user has a _scp_ or _http://schemas.microsoft.com/identity/claims/scope_ claim that contains the _profile_ value, for example:

```json
"scp": "profile email calendar:read"
```

It is also possible to explicitly create a policy that requires the one or more permissions:

```csharp
builder.Services.AddAuthorization(options =>
{
    // Define permissions using a policy.
    options.AddPolicy("UserProfile", builder => builder.RequirePermission("profile"));
});

// ...

// In a Controller
[Authorize(Policy = "UserProfile")] 
public ActionResult<User> Get() => new User(User.Identity!.Name);

// In a Minimal API
app.MapGet("api/me", (ClaimsPrincipal user) =>
{
    return TypedResults.Ok(new User(user.Identity!.Name));
})
.RequireAuthorization(policyNames: "UserProfile")
```

## Samples

- JWT Bearer ([Controller](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/Controllers/JwtBearerSample) | [Minimal API](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/MinimalApis/JwtBearerSample))
- API Key ([Controller](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/Controllers/ApiKeySample) | [Minimal API](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/MinimalApis/ApiKeySample))
- Basic Authentication ([Controller](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/Controllers/BasicAuthenticationSample) | [Minimal API](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/MinimalApis/BasicAuthenticationSample))

## Contribute

The project is constantly evolving. Contributions are welcome. Feel free to file issues and pull requests on the repository, and we'll address them as we can. 
