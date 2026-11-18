# Microsoft Entra ID extensions for Simple Authentication

An optional package for [Simple Authentication for ASP.NET Core](https://github.com/marcominerva/SimpleAuthentication) that adds service-to-service authentication based on Microsoft Entra ID:

- a named JWT Bearer authentication scheme that validates the access tokens issued by a single Microsoft Entra ID tenant;
- authorization policies based on application roles, that accept app-only tokens and reject the ones issued on behalf of a user;
- HTTP clients that call other APIs using app-only tokens, with timeouts, retries and circuit breaker.

The package is built on top of [Microsoft.Identity.Web](https://learn.microsoft.com/entra/msidweb/) and [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/dotnet/core/resilience/http-resilience). It works next to the authentication schemes and the authorization policies that are already registered in the application, without replacing them or changing their defaults.

> [!NOTE]
> User-delegated scenarios, like signing-in users or calling an API on behalf of the current user, are not supported by this package.

## Installation

The library is available on [NuGet](https://www.nuget.org/packages/SimpleAuthenticationTools.EntraId). Search for *SimpleAuthenticationTools.EntraId* in the **Package Manager GUI** or run the following command in the **.NET CLI**:

```shell
dotnet add package SimpleAuthenticationTools.EntraId
```

## Protecting an API

Add to the _appsettings.json_ file a section that contains the settings of the app registration that represents the API. The section uses the same format of Microsoft.Identity.Web:

```json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>", // Required: it must be the GUID of a single tenant
    "ClientId": "<api-client-id>" // Required
}
```

Then, add the Microsoft Entra ID scheme to the authentication builder. If the application uses Simple Authentication, just chain the call to the existing registration:

```csharp
using SimpleAuthentication;

var builder = WebApplication.CreateBuilder(args);

// Registers the schemes defined in the Authentication section, then adds Microsoft Entra ID.
builder.Services.AddSimpleAuthentication(builder.Configuration)
    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"));

// Otherwise, add the scheme to the standard authentication builder.
//builder.Services.AddAuthentication()
//    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"));
```

The scheme is named **EntraId** (you can specify a different name using the second argument of the method) and has the following characteristics:

- it does not change the default scheme and does not add a fallback policy, so no endpoint becomes protected or anonymous because of the registration: every endpoint that must accept Microsoft Entra ID tokens has to opt in explicitly (see below). If the application has a single scheme, which ASP.NET Core automatically uses as the default one, that scheme remains the default one even after adding Microsoft Entra ID;
- tokens are validated by Microsoft.Identity.Web. Signature, issuer, audience and lifetime are always checked, even if the configuration tries to disable these validations;
- only single-tenant applications are supported. _TenantId_ must be a GUID (values like _common_ or _organizations_ are rejected at startup) and tokens issued by other tenants are refused;
- both version 1.0 and 2.0 access tokens are accepted. By default, the audience must be the client ID of the API (or `api://<api-client-id>` for version 1.0 tokens): if the API uses a different Application ID URI, specify it with the _Audience_ setting;
- the configuration is validated when the application starts.

> [!NOTE]
> If Microsoft Entra ID is the only scheme of the application, ASP.NET Core automatically uses it as the default one. Moreover, Microsoft.Identity.Web adds to the default authorization policy the requirements that enforce its `RequiredScope` and `RequiredScopeOrAppPermission` attributes: they have no effect on the endpoints that don't use these attributes.

**Requiring application roles**

Use the `RequireEntraIdApplicationRole` extension method to require that the caller is an application that has been granted at least one of the specified [application roles](https://learn.microsoft.com/entra/identity-platform/howto-add-app-roles-in-apps):

```csharp
// In a Minimal API
app.MapGet("api/orders", () => TypedResults.Ok())
    .RequireEntraIdApplicationRole("Orders.Read");
```

It is also possible to create a policy and then use it in Controllers or Minimal APIs:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OrdersRead", policy => policy.RequireEntraIdApplicationRole("Orders.Read", "Orders.ReadWrite"));

// ...

// In a Controller
[Authorize(Policy = "OrdersRead")]
public IActionResult Get() => Ok();

// In a Minimal API
app.MapGet("api/orders", () => TypedResults.Ok())
    .RequireAuthorization("OrdersRead");
```

The policy is bound to the Microsoft Entra ID scheme, so it works even if another scheme is the default one, and it evaluates only the claims of the token validated by that scheme: a token accepted by another scheme cannot satisfy the requirement, even if it contains the same claims. If you have registered the scheme with a custom name, pass it to the policy:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OrdersRead", policy => policy.RequireEntraIdApplicationRole(["Orders.Read"], schemeName: "Partners"));
```

Only **app-only** tokens are accepted, that is the ones acquired by an application using its own identity (client credentials flow or managed identity). Tokens issued on behalf of a user are rejected, even if the user has been assigned to the required role. To recognize app-only tokens, the API must add the **idtyp** [optional claim](https://learn.microsoft.com/entra/identity-platform/optional-claims-reference) to its access tokens (in the app registration, select **Token configuration**, **Add optional claim**, **Access** and then **idtyp**): tokens that do not contain this claim are rejected.

## Calling another API

An API can call another API protected by Microsoft Entra ID using its own identity. Add the credentials of the application to the Microsoft Entra ID section and create a section for every API to call:

```json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<api-client-id>",
    "ClientCredentials": [
        {
            "SourceType": "SignedAssertionFromManagedIdentity",
            "ManagedIdentityClientId": "<managed-identity-client-id>"
        }
    ]
},
"DownstreamApis": {
    "Inventory": {
        "BaseAddress": "https://inventory.example.com/", // Required: it must use HTTPS and end with a trailing slash
        "Scope": "api://<inventory-api-client-id>/.default" // Required
        //"AuthenticationScheme": "EntraId", // Default: EntraId
        //"AllowHttpOnLoopback": false, // Default: false
        //"Resilience": {
        //    "TotalRequestTimeout": {
        //        "Timeout": "00:00:30" // Default: 30 seconds
        //    },
        //    "AttemptTimeout": {
        //        "Timeout": "00:00:10" // Default: 10 seconds
        //    },
        //    "Retry": {
        //        "MaxRetryAttempts": 3 // Default: 3
        //    }
        //}
    }
}
```

Enable token acquisition, choose a token cache and register the HTTP client:

```csharp
builder.Services.AddSimpleAuthentication(builder.Configuration)
    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches(); // or AddDistributedTokenCaches()

builder.Services.AddEntraIdHttpClient("Inventory", builder.Configuration.GetSection("DownstreamApis:Inventory"))
    .AddTypedClient<InventoryClient>();

// ...

public class InventoryClient(HttpClient httpClient)
{
    public async Task<IEnumerable<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<IEnumerable<Product>>("api/products", cancellationToken) ?? [];
}
```

Tokens are acquired by Microsoft.Identity.Web using the credentials of the authentication scheme and the token cache registered by the application, so they are reused until they are about to expire. The client always uses the identity of the application: the token of the incoming request is never forwarded.

The following settings are available:

| Setting | Description |
| --- | --- |
| _BaseAddress_ | The base address of the API. It must be an absolute HTTPS URI that ends with a trailing slash. |
| _Scope_ | The scope used to request the token, in the `{Application ID URI}/.default` format. |
| _AuthenticationScheme_ | The name of the Microsoft Entra ID scheme whose credentials are used to acquire the token (default: _EntraId_). |
| _AllowHttpOnLoopback_ | Set it to `true` to allow a _BaseAddress_ that uses HTTP, only if it refers to the local machine (for example `http://localhost:5000/`). Use it for local development only (default: `false`). |
| _Resilience_ | The optional settings of the resilience pipeline, bound to [HttpStandardResilienceOptions](https://learn.microsoft.com/dotnet/api/microsoft.extensions.http.resilience.httpstandardresilienceoptions). |

Settings are validated when the application starts.

**Security**

The access token identifies the application, so the HTTP client makes sure that it can be sent only to the API it has been requested for:

- requests can be sent only to addresses that start with _BaseAddress_. A request that targets another host, port, scheme or path fails with an `InvalidOperationException`, before acquiring the token;
- requests cannot contain the _Authorization_ or _Host_ headers;
- redirects are not followed and cookies are not used, when the primary handler is a `SocketsHttpHandler` or an `HttpClientHandler` (as by default);
- the access token is never written in the logs of the HTTP client, and it is removed from the request as soon as the response has been received;
- synchronous requests are not supported.

**Resilience**

The client uses the [standard resilience pipeline](https://learn.microsoft.com/dotnet/core/resilience/http-resilience#standard-resilience-handler-defaults) of .NET, that provides rate limiter, total request timeout, retries, circuit breaker and attempt timeout. Token acquisition is part of every attempt, so it is subject to the same timeouts. You can change the defaults using the _Resilience_ section shown above or in code (code is applied after the configuration):

```csharp
builder.Services.AddEntraIdHttpClient("Inventory", builder.Configuration.GetSection("DownstreamApis:Inventory"), options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
    options.Retry.MaxRetryAttempts = 2;
});
```

The client always uses a single pipeline, otherwise retries would be multiplied: resilience handlers added to all the HTTP clients using `ConfigureHttpClientDefaults` are removed, while adding or removing resilience handlers on the builder returned by `AddEntraIdHttpClient` causes an `InvalidOperationException` when the client is created. Handlers of other libraries that retry requests (for example the policies of _Microsoft.Extensions.Http.Polly_) are not detected, so don't add them to all the HTTP clients.

Only the requests that use a safe HTTP method (_GET_, _HEAD_, _OPTIONS_ and _TRACE_) are retried. The other ones can write data, and the API may have already processed them even if the response has not been received, so they are sent only once and this behavior cannot be changed with the resilience settings. If you know that a request is idempotent (for example because the API supports idempotency keys), you can explicitly allow retries for it:

```csharp
using var request = new HttpRequestMessage(HttpMethod.Put, $"api/products/{id}") { Content = JsonContent.Create(product) };
using var response = await httpClient.SendAsync(request.MarkAsIdempotent(), cancellationToken);
```

## Credentials

Credentials are required only by the applications that call other APIs and are configured with the _ClientCredentials_ setting of Microsoft.Identity.Web, that supports [multiple sources](https://learn.microsoft.com/entra/msidweb/authentication/credentials-overview).

When the application is hosted in Azure, the recommended approach is to use a **managed identity federated with the existing app registration**, so that there are no secrets or certificates to store and rotate. Assign a managed identity to the Azure resource, add it as federated credential of the app registration and use the `SignedAssertionFromManagedIdentity` source type, as shown above. Refer to the [official documentation](https://learn.microsoft.com/entra/msidweb/authentication/certificateless) for the complete procedure.

Managed identities are not available on a development machine. For local development, use an app registration dedicated to development and provide its credentials with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or environment variables, for example:

```shell
dotnet user-secrets set "AzureAd:ClientCredentials:0:SourceType" "ClientSecret"
dotnet user-secrets set "AzureAd:ClientCredentials:0:ClientSecret" "<client-secret>"
```

> [!WARNING]
> Never store secrets in the _appsettings.json_ file or in other files under source control.

## Samples

The [EntraIdMicroservices](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/MinimalApis/EntraIdMicroservices) sample contains two APIs: the first one requires an application role and calls the second one using its own identity. The README of the sample describes how to configure Microsoft Entra ID.

## Contribute

The project is constantly evolving. Contributions are welcome. Feel free to file issues and pull requests in the repository, and we'll address them as we can.
