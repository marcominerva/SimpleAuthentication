# Simple Authentication for ASP.NET Core

[![GitHub Super-Linter](https://github.com/marcominerva/SimpleAuthentication/workflows/Lint%20Code%20Base/badge.svg)](https://github.com/marketplace/actions/super-linter)
[![Nuget](https://img.shields.io/nuget/v/SimpleAuthenticationTools)](https://www.nuget.org/packages/SimpleAuthenticationTools)
[![Nuget](https://img.shields.io/nuget/dt/SimpleAuthenticationTools)](https://www.nuget.org/packages/SimpleAuthenticationTools)


A library to easily integrate Authentication in ASP.NET Core projects. Currently supports JWT Bearer and API Key Authentication.

**Installation**

The library is available on [NuGet](https://www.nuget.org/packages/SimpleAuthenticationTools). Just search *SimpleAuthenticationTools* in the **Package Manager GUI** or run the following command in the **Package Manager Console**:

    Install-Package SimpleAuthenticationTools

**Usage Video**

Take a look to a quick demo showing how to integrate the library:

[![Simple Authentication for ASP.NET Core](https://img.youtube.com/vi/SVZuaPE2yNc/0.jpg)](https://www.youtube.com/watch?v=SVZuaPE2yNc)

**Configuration**

Authentication can be totally configured adding an _Authentication_ section in the _appsettings.json_ file:

    "Authentication": {
      "DefaultAuthenticationScheme": "Bearer",  // Required
      "JwtBearer": {
          "SecurityKey": "supersecretsecuritykey42!",  // Required
          "Algorithm": "HS256", // Default HS256
          "Issuers": [ "issuer" ], // Optional
          "Audiences": [ "audience" ], // Optional
          "ExpirationTime": "01:00:00", // Default no expiration
          "ClockSkew": "00:02:00", // Default 5 minutes
          "EnableJwtBearerService": true // Default true
      },
      "ApiKey": {
          "SchemeName": "MyApiKeyScheme",  // Required
          // You can specify either HeaderName, QueryName or both
          "HeaderName": "x-api-key",
          "QueryName": "code",
          // Uncomment this line if you want to validate the API Key against a fixed value.
          // Otherwise, you need to register an IApiKeyValidator implementation that will be used
          // to validate the API Key.
          //"ApiKeyValue": "f1I7S5GXa4wQDgLQWgz0",
          "DefaultUsername": "ApiUser"  // Required ApiKeyValue is used
      }
    }


You can configure only the kind of authentication you want to use, or you can include both.

The _DefaultAuthenticationScheme_ attribute is used to specify what kind of authentication must be configured as default. Allowed values are _Bearer_ or the value of the _SchemeName_ defined in the _ApiKey_ section (_MyApiKeyScheme_ in this example).

**Contribute**

The project is constantly evolving. Contributions are welcome. Feel free to file issues and pull requests on the repo and we'll address them as we can. 
