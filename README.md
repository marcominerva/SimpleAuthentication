# Simple Authentication for ASP.NET Core

[![Lint Code Base](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/linter.yml/badge.svg)](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/linter.yml)
[![CodeQL](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/codeql.yml/badge.svg)](https://github.com/marcominerva/SimpleAuthentication/actions/workflows/codeql.yml)
[![Nuget](https://img.shields.io/nuget/v/SimpleAuthenticationTools)](https://www.nuget.org/packages/SimpleAuthenticationTools)
[![Nuget](https://img.shields.io/nuget/dt/SimpleAuthenticationTools)](https://www.nuget.org/packages/SimpleAuthenticationTools)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/marcominerva/SimpleAuthentication/blob/master/LICENSE)

A library to easily integrate Authentication in ASP.NET Core projects. Currently supports JWT Bearer, API Key and Basic Authentication.

**Installation**

The library is available on [NuGet](https://www.nuget.org/packages/SimpleAuthenticationTools). Just search *SimpleAuthenticationTools* in the **Package Manager GUI** or run the following command in the **Package Manager Console**:

    Install-Package SimpleAuthenticationTools

**Usage Video**

Take a look to a quick demo showing how to integrate the library:

[![Simple Authentication for ASP.NET Core](Screenshot.jpg)](https://www.youtube.com/watch?v=SVZuaPE2yNc)

**Configuration**

Authentication can be totally configured adding an _Authentication_ section in the _appsettings.json_ file:

    "Authentication": {
      "DefaultScheme": "Bearer", // Optional
      "JwtBearer": {
          "SchemeName": "Bearer" // Default: Bearer
          "SecurityKey": "supersecretsecuritykey42!", // Required
          "Algorithm": "HS256", // Default: HS256
          "Issuers": [ "issuer" ], // Optional
          "Audiences": [ "audience" ], // Optional
          "ExpirationTime": "01:00:00", // Default: No expiration
          "ClockSkew": "00:02:00", // Default: 5 minutes
          "EnableJwtBearerService": true // Default: true
      },
      "ApiKey": {
          "SchemeName": "MyApiKeyScheme", // Default: ApiKey
          // You can specify either HeaderName, QueryStringKey or both
          "HeaderName": "x-api-key",
          "QueryStringKey": "code",
          // Uncomment this line if you want to validate the API Key against a fixed value.
          // Otherwise, you need to register an IApiKeyValidator implementation that will be used
          // to validate the API Key.
          //"ApiKeyValue": "f1I7S5GXa4wQDgLQWgz0",
          "DefaultUserName": "ApiUser" // Required ApiKeyValue is used
      },
      "Basic": {
          "SchemeName": "Basic", // Default: Basic
          // Uncomment the following lines if you want to validate user name and password against fixed values.
          // Otherwise, you need to register an IBasicAuthenticationValidator implementation that will be used
          // to validate the credentials.
          //"UserName": "marco",
          //"Password": "P@$$w0rd"
      }
    }


You can configure only the kind of authentication you want to use, or you can include all of them.

The _DefaultScheme_ attribute is used to specify what kind of authentication must be configured as default. Allowed values are the values of the _SchemeName_ attributes.

**Contribute**

The project is constantly evolving. Contributions are welcome. Feel free to file issues and pull requests on the repo and we'll address them as we can. 
