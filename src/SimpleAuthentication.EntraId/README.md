# Microsoft Entra ID extensions for Simple Authentication

An optional package for [Simple Authentication for ASP.NET Core](https://github.com/marcominerva/SimpleAuthentication) that adds service-to-service authentication based on Microsoft Entra ID:

- a named JWT Bearer authentication scheme that validates the access tokens issued by a single Microsoft Entra ID tenant;
- authorization policies based on application roles, that accept app-only tokens and reject the ones issued on behalf of a user;
- HTTP clients that call other APIs using app-only tokens, with timeouts, retries and circuit breaker.

```
                         1. token request (client credentials)
   ┌──────────────┐ ───────────────────────────────────────────▶ ┌──────────────────────┐
   │  Client API  │ ◀─────────────────────────────────────────── │  Microsoft Entra ID  │
   │ (the caller) │              2. app-only access token         └──────────────────────┘
   └──────────────┘
          │ 3. HTTP request with "Authorization: Bearer <token>"
          ▼
   ┌──────────────┐
   │  Target API  │  4. validates the token and checks the application role
   └──────────────┘
```

The package is built on top of [Microsoft.Identity.Web](https://learn.microsoft.com/entra/msidweb/) and [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/dotnet/core/resilience/http-resilience). It works next to the authentication schemes and the authorization policies that are already registered in the application, without replacing them or changing their defaults.

> [!NOTE]
> User-delegated scenarios, like signing-in users or calling an API on behalf of the current user (On-Behalf-Of), are not supported by this package.

**Contents**

- [Installation](#installation)
- [Quick start](#quick-start)
- [How it works](#how-it-works)
- [Microsoft Entra ID setup](#microsoft-entra-id-setup)
- [Protecting an API](#protecting-an-api)
- [Calling another API](#calling-another-api)
- [Credentials](#credentials)
- [Migrating from API keys](#migrating-from-api-keys)
- [Verifying the token](#verifying-the-token)
- [Troubleshooting](#troubleshooting)
- [Requirements and limitations](#requirements-and-limitations)

## Installation

The library is available on [NuGet](https://www.nuget.org/packages/SimpleAuthenticationTools.EntraId). Search for *SimpleAuthenticationTools.EntraId* in the **Package Manager GUI** or run the following command in the **.NET CLI**:

```shell
dotnet add package SimpleAuthenticationTools.EntraId
```

## Quick start

If you are already familiar with Microsoft Entra ID, this is everything you need. The rest of this document explains each step.

**Target API** (receives the calls), with an app registration that exposes the `Orders.Read` application role and emits the `idtyp` optional claim:

```json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<target-api-client-id>"
}
```

```csharp
builder.Services.AddSimpleAuthentication(builder.Configuration)
    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"));

// ...

app.MapGet("api/orders", () => TypedResults.Ok())
    .RequireEntraIdApplicationRole("Orders.Read");
```

**Client API** (makes the calls), with an app registration that has been granted the `Orders.Read` application permission (with admin consent) and has a credential:

```json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-api-client-id>",
    "ClientCredentials": [
        { "SourceType": "SignedAssertionFromManagedIdentity", "ManagedIdentityClientId": "<managed-identity-client-id>" }
    ]
},
"DownstreamApis": {
    "Orders": {
        "BaseAddress": "https://orders.example.com/",
        "Scope": "api://<target-api-client-id>/.default"
    }
}
```

```csharp
builder.Services.AddSimpleAuthentication(builder.Configuration)
    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddEntraIdHttpClient("Orders", builder.Configuration.GetSection("DownstreamApis:Orders"))
    .AddTypedClient<OrdersClient>();
```

## How it works

Two applications are always involved, and each one has its own app registration in Microsoft Entra ID:

- the **target API** is the application that *receives* the token: its app registration defines the application roles and the content of the tokens issued for it;
- the **client** is the application that *requests* the token to call the target API: its app registration is granted the roles and owns the credential used to prove its identity.

The client uses the [client credentials flow](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-client-creds-grant-flow) to get an **app-only** token, that is a token issued to the application itself, without any user. When the target API receives the token, the following checks are performed, in this order:

| Check | Claims involved | Performed by | Result if it fails |
| --- | --- | --- | --- |
| The token has been issued by the configured tenant | `iss`, `tid` | Microsoft.Identity.Web and this package | 401 |
| The token has been issued for this API | `aud` | Microsoft.Identity.Web | 401 |
| The token is signed by Microsoft Entra ID and is not expired | signature, `exp`, `nbf` | Microsoft.Identity.Web | 401 |
| The token contains at least a role or a scope | `roles`, `scp` | Microsoft.Identity.Web | 401 (`IDW10201`) |
| The token has been issued to an application, not to a user | `idtyp`, `scp` | this package | 403 |
| The token contains one of the required application roles | `roles` | this package | 403 |

The first four checks are part of authentication: a token that fails them is not accepted at all. The last two are part of authorization, and are performed only on the endpoints that require an application role.

> [!IMPORTANT]
> Microsoft Entra ID issues a token for an API to **any** application of the tenant that has a credential, even if it has not been granted any role: such a token simply has no `roles` claim. For this reason, the application role is what actually decides which applications can call the API, and it is always required.

## Microsoft Entra ID setup

Each setting belongs to exactly one of the two app registrations. Most configuration errors come from applying a setting to the wrong one.

**Target API app registration** (the application that receives the token)

| Setting | Where (Microsoft Entra admin center) | Why |
| --- | --- | --- |
| Application ID URI, for example `api://<target-api-client-id>` | **Expose an API** | Identifies the API when the client requests a token (`<Application ID URI>/.default`) |
| An application role, for example `Orders.Read`, with **Allowed member types** set to **Applications** | **App roles** | It is the permission granted to the clients. Don't reuse roles meant for users: Microsoft recommends keeping user and application roles separate |
| The **`idtyp`** optional claim, for the **Access** token type | **Token configuration** → **Add optional claim** | Lets the API tell app-only tokens from user tokens. Without it, every token is rejected with 403 |
| `requestedAccessTokenVersion` set to `2` (optional) | **Manifest** (`api` section) | Tokens are version 1.0 by default. Both versions are accepted, but the audience differs (see [Protecting an API](#protecting-an-api)) |

**Client app registration** (the application that requests the token)

| Setting | Where (Microsoft Entra admin center) | Why |
| --- | --- | --- |
| The application permission that corresponds to the role, for example `Orders.Read` | **API permissions** → **Add a permission** → **APIs my organization uses** → **Application permissions** | Declares that the client needs the role |
| **Grant admin consent** | **API permissions** | Actually assigns the role to the client. Without it, the token has no `roles` claim and is rejected with 401 |
| A credential | **Certificates & secrets** (certificate or secret) or **Federated credentials** (managed identity) | Proves the identity of the client when it requests the token. See [Credentials](#credentials) |

> [!NOTE]
> The **`idtyp`** claim (identity type) must not be confused with **`idp`** (identity provider): only `idtyp` is used by this package. Its value is `app` in app-only tokens. See [Optional claims reference](https://learn.microsoft.com/entra/identity-platform/optional-claims-reference).

Changes to the app registrations apply to the tokens issued afterwards. Tokens are cached by the client for about an hour: restart it to get a new token immediately.

## Protecting an API

Add to the _appsettings.json_ file a section that contains the settings of the app registration of the API, in the format used by Microsoft.Identity.Web:

```json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>", // Required: it must be the GUID of a single tenant
    "ClientId": "<api-client-id>" // Required
    //"Audience": "api://custom-uri" // Optional: see below
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
- both version 1.0 and 2.0 access tokens are accepted. By default, the audience must be the client ID of the API for version 2.0 tokens, and `api://<api-client-id>` for version 1.0 tokens: if the Application ID URI of the API is different, specify it with the _Audience_ setting;
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

The policy is bound to the Microsoft Entra ID scheme, so it works even if another scheme is the default one, and it evaluates only the claims of the token validated by that scheme: a token accepted by another scheme cannot satisfy the requirement, even if it contains the same claims. Roles are case-sensitive. If you have registered the scheme with a custom name, pass it to the policy:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OrdersRead", policy => policy.RequireEntraIdApplicationRole(["Orders.Read"], schemeName: "Partners"));
```

Only **app-only** tokens are accepted: a token is considered app-only when it contains the `idtyp` claim with the `app` value and no `scp` claim. Tokens issued on behalf of a user are rejected, even if the user has been assigned to the required role.

## Calling another API

An API can call another API protected by Microsoft Entra ID using its own identity. Add the credentials of the application to the Microsoft Entra ID section (see [Credentials](#credentials)) and create a section for every API to call:

```json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-api-client-id>",
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
| _Scope_ | The scope used to request the token: the Application ID URI of the target API followed by `/.default`. App-only tokens always contain all the roles granted to the client on that API. |
| _AuthenticationScheme_ | The name of the Microsoft.Identity.Web scheme whose configuration (client ID and credentials) is used to acquire the token (default: _EntraId_). |
| _AllowHttpOnLoopback_ | Set it to `true` to allow a _BaseAddress_ that uses HTTP, only if it refers to the local machine (for example `http://localhost:5000/`), where the token never leaves the computer. Use it for local development only (default: `false`). |
| _Resilience_ | The optional settings of the resilience pipeline, bound to [HttpStandardResilienceOptions](https://learn.microsoft.com/dotnet/api/microsoft.extensions.http.resilience.httpstandardresilienceoptions). |

Settings are validated when the application starts.

**Reusing an existing Microsoft.Identity.Web scheme**

If the application already protects its endpoints with Microsoft.Identity.Web (for example for its users), you don't need to call `AddEntraIdJwtBearer` just to call other APIs: enable token acquisition on the existing registration and set _AuthenticationScheme_ to the name of that scheme. The credentials go in the configuration section of the existing scheme.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdSettings"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// In the configuration of the client: "AuthenticationScheme": "Bearer"
builder.Services.AddEntraIdHttpClient("Inventory", builder.Configuration.GetSection("DownstreamApis:Inventory"));
```

**Using Refit**

The client is a standard `IHttpClientBuilder`, so it can be used with [Refit](https://github.com/reactiveui/refit) interfaces:

```csharp
builder.Services.AddEntraIdHttpClient("Inventory", builder.Configuration.GetSection("DownstreamApis:Inventory"))
    .AddTypedClient(client => RestService.For<IInventoryApi>(client));
```

Refit combines the base address and the relative paths of the methods (for example `[Get("/products")]`) correctly, even if the base address ends with a slash.

**Security**

The access token identifies the application, so the HTTP client makes sure that it can be sent only to the API it has been requested for:

- requests can be sent only to addresses that start with _BaseAddress_. A request that targets another host, port, scheme or path fails with an `InvalidOperationException`, before acquiring the token. Changing `HttpClient.BaseAddress` at runtime does not change the allowed destination;
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

The client always uses a single pipeline, otherwise retries would be multiplied: resilience handlers added to all the HTTP clients using `ConfigureHttpClientDefaults` are removed, while adding or removing resilience handlers on the builder returned by `AddEntraIdHttpClient` causes an `InvalidOperationException` when the client is created.

Only the requests that use a safe HTTP method (_GET_, _HEAD_, _OPTIONS_ and _TRACE_) are retried. The other ones can write data, and the API may have already processed them even if the response has not been received, so they are sent only once and this behavior cannot be changed with the resilience settings. If you know that a request is idempotent (for example because the API supports idempotency keys), you can explicitly allow retries for it:

```csharp
using var request = new HttpRequestMessage(HttpMethod.Put, $"api/products/{id}") { Content = JsonContent.Create(product) };
using var response = await httpClient.SendAsync(request.MarkAsIdempotent(), cancellationToken);
```

## Credentials

Credentials are required only by the applications that call other APIs. They are configured with the _ClientCredentials_ setting of Microsoft.Identity.Web, in the same section that contains the client ID.

_ClientCredentials_ is an **ordered list**: Microsoft.Identity.Web tries the credentials in order and uses the first one that works. In configuration keys, the index identifies the element of the list: for example `AzureAd:ClientCredentials:0:SourceType` is the `SourceType` of the first credential. The name must be exactly _ClientCredentials_: settings with a different name are silently ignored, and the error appears only when the first token is requested.

The recommended credential depends on where the application runs:

| Environment | Credential | `SourceType` |
| --- | --- | --- |
| Azure (App Service, Container Apps, AKS, ...) | Managed identity federated with the app registration: no secret at all | `SignedAssertionFromManagedIdentity` |
| Local development or on-premises | Certificate stored in Azure Key Vault, downloaded with the identity of the developer or of the server | `KeyVault` |
| Local development or on-premises | Certificate in the certificate store | `StoreWithThumbprint`, `StoreWithDistinguishedName` |
| Local development only | Client secret, with a short expiration | `ClientSecret` |

For example, the following configuration uses the managed identity in Azure and falls back to a certificate stored in Key Vault elsewhere:

```json
"ClientCredentials": [
    {
        "SourceType": "SignedAssertionFromManagedIdentity",
        "ManagedIdentityClientId": "<managed-identity-client-id>"
    },
    {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://<vault-name>.vault.azure.net/",
        "KeyVaultCertificateName": "<certificate-name>"
    }
]
```

To use a **managed identity**, assign a user-assigned managed identity to the Azure resource, add it as federated credential of the app registration (**Certificates & secrets** → **Federated credentials** → **Managed Identity** scenario) and set its client ID in _ManagedIdentityClientId_. Refer to the [official documentation](https://learn.microsoft.com/entra/msidweb/authentication/certificateless) for the complete procedure.

To use a **certificate**, upload its public part to the app registration (**Certificates & secrets** → **Certificates**). See [Credentials overview for Microsoft.Identity.Web](https://learn.microsoft.com/entra/msidweb/authentication/credentials-overview) for all the supported sources.

> [!WARNING]
> Never store client secrets in the _appsettings.json_ files or in other files under source control. Leave an empty value in the files and provide the secret with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) during development, or with environment variables or Azure Key Vault in the other environments. A secret that has been exposed (for example committed, logged or pasted in a chat) must be deleted and replaced.

For example, during development:

```json
"ClientCredentials": [
    { "SourceType": "ClientSecret", "ClientSecret": "" }
]
```

```shell
dotnet user-secrets set "AzureAd:ClientCredentials:0:ClientSecret" "<client-secret>"
```

## Migrating from API keys

When an API that is called with API keys starts accepting Microsoft Entra ID tokens, the existing clients usually need to keep working until they are migrated. In this case, the endpoints must accept **either** the API key **or** an app-only token with the required role.

The requirements of an authorization policy are all mandatory, so the alternative must be expressed with a single assertion. The policy authenticates the request with both schemes, accepts it if the API key scheme has authenticated the caller, and otherwise evaluates the application role requirement:

```csharp
builder.Services.AddSimpleAuthentication(builder.Configuration)
    .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"));

var apiKeySchemeName = "ApiKey"; // The SchemeName of the API key settings
var applicationRoleRequirement = new EntraIdApplicationRoleRequirement(EntraIdDefaults.AuthenticationScheme, ["Orders.Read"]);

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder(apiKeySchemeName, EntraIdDefaults.AuthenticationScheme)
        .RequireAssertion(async context =>
        {
            // Simple Authentication uses the scheme name as authentication type of the identities it creates.
            if (context.User.Identities.Any(identity => identity.IsAuthenticated && identity.AuthenticationType == apiKeySchemeName))
            {
                return true;
            }

            var entraIdContext = new AuthorizationHandlerContext([applicationRoleRequirement], context.User, context.Resource);
            await applicationRoleRequirement.HandleAsync(entraIdContext);

            return entraIdContext.HasSucceeded;
        })
        .Build());
```

In this example the policy replaces the default one, so it applies to all the endpoints that use `[Authorize]` or `RequireAuthorization()` without a policy name. To limit it to specific endpoints, register it as a named policy instead.

On the client side, replace the HTTP client that sends the API key with the one registered by `AddEntraIdHttpClient`. When all the clients have been migrated, remove the API key scheme and use `RequireEntraIdApplicationRole` directly.

## Verifying the token

To check the content of the token that a client receives, request one with the same credentials and decode it locally. The following PowerShell script uses a client secret stored in the user secrets of the client project, and prints only the claims, never the secret or the token:

```powershell
$line   = dotnet user-secrets list --project <client-project> | Select-String '^AzureAd:ClientCredentials:0:ClientSecret = '
$secret = ($line.ToString() -split ' = ', 2)[1]
$body = @{
    grant_type    = 'client_credentials'
    client_id     = '<client-api-client-id>'
    client_secret = $secret
    scope         = 'api://<target-api-client-id>/.default'
}
$token = (Invoke-RestMethod -Method Post -Uri 'https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token' -Body $body).access_token
Remove-Variable secret, body, line
$payload = $token.Split('.')[1].Replace('-', '+').Replace('_', '/')
switch ($payload.Length % 4) { 2 { $payload += '==' } 3 { $payload += '=' } }
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload)) | ConvertFrom-Json | ConvertTo-Json
```

An app-only token that satisfies the requirements of this package contains:

| Claim | Expected value |
| --- | --- |
| `aud` | `api://<target-api-client-id>` (version 1.0) or `<target-api-client-id>` (version 2.0) |
| `iss` | `https://sts.windows.net/<tenant-id>/` (version 1.0) or `https://login.microsoftonline.com/<tenant-id>/v2.0` (version 2.0) |
| `tid` | `<tenant-id>` |
| `appid` (version 1.0) or `azp` (version 2.0) | `<client-api-client-id>` |
| `idtyp` | `app` |
| `roles` | the roles granted to the client, for example `["Orders.Read"]` |
| `scp` | **absent** |

> [!WARNING]
> An access token works as a password until it expires. Don't paste it in online decoders or in issue trackers.

To see the claims of the tokens actually received by the target API, you can temporarily log them during development, chaining the events of the scheme:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<JwtBearerOptions>(EntraIdDefaults.AuthenticationScheme, options =>
    {
        var onTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
            await onTokenValidated(context);

            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var claims = context.SecurityToken is JsonWebToken token ? token.Claims : context.Principal?.Claims ?? [];
            logger.LogInformation("Microsoft Entra ID token for {Path}: {Claims}", context.Request.Path, string.Join(", ", claims.Select(claim => $"{claim.Type}={claim.Value}")));
        };
    });
}
```

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| The application doesn't start: `TenantId` or `ClientId` must be a GUID | Missing configuration, or a multi-tenant value like `common` | Set the GUIDs of the tenant and of the app registration |
| The application doesn't start: `OptionsValidationException` on _BaseAddress_ or _Scope_ | Invalid settings of an HTTP client | Use an HTTPS base address that ends with `/` and a scope in the `<Application ID URI>/.default` format |
| `AADSTS7000215` in the log of the client | Wrong or expired client secret | Create a new credential |
| `AADSTS700016` in the log of the client | Wrong client ID or tenant | Check `ClientId` and `TenantId` of the client |
| `AADSTS500011` in the log of the client | The scope doesn't match the Application ID URI of the target API | Check _Scope_ and **Expose an API** of the target API |
| The client reports that no credential is configured | _ClientCredentials_ is missing or misspelled | Check the name of the setting and the index of the elements |
| 401 on the target API with `IDW10201` | The token has neither roles nor scopes: the client has not been granted any role | Grant the application permission and admin consent to the client |
| 401 on the target API with `IDX10214` | Audience not valid | Use the right scope in the client, or set _Audience_ in the target API |
| 401 on the target API with an issuer or tenant error | The token comes from another tenant | Check `TenantId` on both sides |
| 403 on the target API | `idtyp` missing, `scp` present, or role missing | Add the `idtyp` optional claim to the **target API** registration and grant the role to the client; then restart the client to drop the cached token |
| The client fails with `InvalidOperationException` before sending the request | Destination outside _BaseAddress_, or `Authorization`/`Host` header set by the caller | Use relative paths and don't add authentication headers |
| The client fails with `InvalidOperationException` when it is created | Token acquisition not enabled, or resilience handlers added or removed after the registration | Call `EnableTokenAcquisitionToCallDownstreamApi()` and a token cache; configure resilience with the _Resilience_ section |
| The caller returns 404 or 500 but the target API has no errors | The caller converts the errors of the target API | Check the log of the target API, not only the one of the caller |

## Requirements and limitations

- .NET 8, 9, 10 or 11, and the ASP.NET Core shared framework, also for the applications that only call other APIs (for example worker services).
- The package depends on Microsoft.Identity.Web, Microsoft.Extensions.Http.Resilience and on the latest patch of `Microsoft.AspNetCore.Authentication.JwtBearer` for each target framework. If the application references an older patch directly, NuGet reports a downgrade error (`NU1605`): update that reference.
- Only single-tenant applications are supported.
- User-delegated scenarios (sign-in of users, On-Behalf-Of) are not supported.
- Handlers of other libraries that retry requests (for example the policies of _Microsoft.Extensions.Http.Polly_) are not detected: don't add them to all the HTTP clients with `ConfigureHttpClientDefaults`, otherwise requests that can write data could be retried.
- Redirects and cookies are disabled only when the primary handler is a `SocketsHttpHandler` or an `HttpClientHandler`.

## Samples

The [EntraIdMicroservices](https://github.com/marcominerva/SimpleAuthentication/tree/master/samples/MinimalApis/EntraIdMicroservices) sample contains two APIs: the first one requires an application role and calls the second one using its own identity.

## Contribute

The project is constantly evolving. Contributions are welcome. Feel free to file issues and pull requests in the repository, and we'll address them as we can.
