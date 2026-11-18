# Microsoft Entra ID microservices sample

This sample shows how two APIs can talk to each other using Microsoft Entra ID, without sharing secrets:

- **OrdersApi** exposes the `GET /api/orders` endpoint, that requires an app-only access token with the `Orders.Read` application role. To build the response, it calls the Inventory API using its own identity.
- **InventoryApi** exposes the `GET /api/products` endpoint, that requires an app-only access token with the `Inventory.Read` application role.

```
Client application --(Orders.Read)--> OrdersApi --(Inventory.Read)--> InventoryApi
```

The Orders API registers Microsoft Entra ID next to the schemes of [Simple Authentication](https://github.com/marcominerva/SimpleAuthentication) (none is configured in this sample), while the Inventory API uses the standard authentication builder. In both cases Microsoft Entra ID is the only scheme, so ASP.NET Core automatically uses it as the default one, but only the endpoints that call `RequireEntraIdApplicationRole` require a token: the registration doesn't add a fallback policy.

## Configure Microsoft Entra ID

The sample needs a Microsoft Entra ID tenant where you can register applications and grant admin consent. The following steps use the [Microsoft Entra admin center](https://entra.microsoft.com). The meaning of each setting is explained in the [Microsoft Entra ID setup](https://github.com/marcominerva/SimpleAuthentication/tree/master/src/SimpleAuthentication.EntraId#microsoft-entra-id-setup) section of the package documentation.

In this sample, the Inventory API is the *target API* of the Orders API, and the Orders API is in turn the *target API* of the client application: each target API needs its application role and the `idtyp` optional claim.

**1. Register the Inventory API**

1. Go to **App registrations** and create a new single-tenant registration named, for example, _Inventory API_.
2. In **Expose an API**, set the **Application ID URI** to the proposed value (`api://<inventory-api-client-id>`).
3. In **App roles**, create a role with **Allowed member types** set to _Applications_ and **Value** set to `Inventory.Read`.
4. In **Token configuration**, select **Add optional claim**, **Access** and then **idtyp**. This claim allows the API to accept app-only tokens only.
5. In **Manifest**, set `requestedAccessTokenVersion` (in the `api` section) to `2`, so that the API receives version 2.0 access tokens.

**2. Register the Orders API**

1. Create another single-tenant registration named, for example, _Orders API_, and repeat the steps 2-5 using `Orders.Read` as the value of the application role.
2. In **API permissions**, select **Add a permission**, **APIs my organization uses**, then choose the Inventory API and add the `Inventory.Read` **application permission**.
3. Select **Grant admin consent**. Now the Orders API is allowed to get tokens for the Inventory API that contain the `Inventory.Read` role.

**3. Register a client application**

To call the Orders API you need an application that has been granted the `Orders.Read` role: create a third registration (for example _Orders client_) with a client secret, add the `Orders.Read` application permission of the Orders API and grant admin consent, as in the previous step.

Refer to the official documentation for more information about [app roles](https://learn.microsoft.com/entra/identity-platform/howto-add-app-roles-in-apps) and [optional claims](https://learn.microsoft.com/entra/identity-platform/optional-claims).

## Configure the sample

The _appsettings.json_ files contain placeholders only. The APIs do not start until `TenantId` and `ClientId` contain valid identifiers, so you can immediately see what is missing.

The projects are already enabled to use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), that are stored outside of the repository. From the folder of the sample, set the identifiers of the Inventory API (they are not secrets, so you can also write them in its _appsettings.json_ file):

```shell
dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>" --project InventoryApi
dotnet user-secrets set "AzureAd:ClientId" "<inventory-api-client-id>" --project InventoryApi
```

Then, configure the Orders API:

```shell
dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>" --project OrdersApi
dotnet user-secrets set "AzureAd:ClientId" "<orders-api-client-id>" --project OrdersApi
dotnet user-secrets set "DownstreamApis:Inventory:Scope" "api://<inventory-api-client-id>/.default" --project OrdersApi
```

The Orders API needs credentials to prove its identity when it requests a token for the Inventory API.

**Local development**

Managed identities are not available on a development machine, so use an app registration dedicated to development and provide its credentials with user secrets or environment variables, so that they never end up in source control. For example, create a client secret in **Certificates & secrets** and run the following commands:

```shell
dotnet user-secrets set "AzureAd:ClientCredentials:0:SourceType" "ClientSecret" --project OrdersApi
dotnet user-secrets set "AzureAd:ClientCredentials:0:ClientSecret" "<client-secret>" --project OrdersApi
```

You can also use a certificate: see [Credentials overview for Microsoft.Identity.Web](https://learn.microsoft.com/entra/msidweb/authentication/credentials-overview) for all the supported options.

**Azure**

When the Orders API is hosted in Azure, use a managed identity federated with its app registration, so that no secret is needed:

1. Create a user-assigned managed identity and assign it to the Azure resource that hosts the Orders API.
2. In the app registration of the Orders API, go to **Certificates & secrets**, **Federated credentials** and add a credential with the **Managed Identity** scenario, selecting the managed identity.
3. Set `AzureAd:ClientCredentials:0:ManagedIdentityClientId` to the client ID of the managed identity (the `SignedAssertionFromManagedIdentity` source type is already configured in _appsettings.json_) and `DownstreamApis:Inventory:BaseAddress` to the HTTPS address of the Inventory API.

The complete procedure is described in [Configure an application to trust a managed identity](https://learn.microsoft.com/entra/workload-id/workload-identity-federation-config-app-trust-managed-identity).

## Run the sample

Start both the APIs:

```shell
dotnet run --project InventoryApi
dotnet run --project OrdersApi
```

The Inventory API listens on `https://localhost:7102` and the Orders API on `https://localhost:7101`. They use HTTPS with the ASP.NET Core development certificate: if it is not trusted yet, run `dotnet dev-certs https --trust`.

Get an app-only token for the Orders API using the client application registered before, then call the endpoint:

```shell
curl -X POST "https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token" -d "grant_type=client_credentials" -d "client_id=<client-application-id>" -d "client_secret=<client-application-secret>" -d "scope=api://<orders-api-client-id>/.default"

curl "https://localhost:7101/api/orders" -H "Authorization: Bearer <access-token>"
```

The response contains the orders, with the availability of the products provided by the Inventory API. If you call the Orders API without a token, with a token that does not contain the `Orders.Read` role or with a token issued on behalf of a user, the request is rejected. The same happens to the Orders API if it has not been granted the `Inventory.Read` role.

If something doesn't work, the [Verifying the token](https://github.com/marcominerva/SimpleAuthentication/tree/master/src/SimpleAuthentication.EntraId#verifying-the-token) and [Troubleshooting](https://github.com/marcominerva/SimpleAuthentication/tree/master/src/SimpleAuthentication.EntraId#troubleshooting) sections of the package documentation show how to check the content of the tokens and the most common causes of errors.

## What to look at

- The HTTP client of the Orders API can send requests only to the configured base address, that must use HTTPS. To use HTTP on the local machine, set `DownstreamApis:Inventory:AllowHttpOnLoopback` to `true` in the development settings.
- Requests that can write data (_POST_, _PUT_, _PATCH_, _DELETE_) are never retried automatically. Timeouts, retries and circuit breaker can be configured in the `DownstreamApis:Inventory:Resilience` section.
- The token received by the Orders API is never forwarded to the Inventory API.

See the [documentation of the package](https://github.com/marcominerva/SimpleAuthentication/tree/master/src/SimpleAuthentication.EntraId) for all the details.
