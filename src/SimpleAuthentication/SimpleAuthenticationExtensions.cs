using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;
using SimpleAuthentication.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication;

/// <summary>
/// Providers extension methods for adding authentication support in ASP.NET Core.
/// </summary>
/// <seealso cref="ISimpleAuthenticationBuilder"/>
/// <seealso cref="AuthenticationBuilder"/>
public static class SimpleAuthenticationExtensions
{
    /// <summary>
    /// Registers services required by authentication services, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <returns>A <see cref="ISimpleAuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <exception cref="ArgumentException">Configuration is invalid.</exception>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="IServiceCollection"/>
    /// <seealso cref="IConfiguration"/>
    /// <seealso cref="ISimpleAuthenticationBuilder"/>
    public static ISimpleAuthenticationBuilder AddSimpleAuthentication(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication")
    {
        var defaultAuthenticationScheme = configuration.GetValue<string>($"{sectionName}:DefaultScheme");

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = defaultAuthenticationScheme;
            options.DefaultChallengeScheme = defaultAuthenticationScheme;
        });

        return builder.AddSimpleAuthentication(configuration, sectionName);
    }

    /// <summary>
    /// Registers services required by authentication services, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <returns>A <see cref="ISimpleAuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <exception cref="ArgumentException">Configuration is invalid.</exception>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="AuthenticationBuilder"/>
    /// <seealso cref="IConfiguration"/>
    /// <seealso cref="ISimpleAuthenticationBuilder"/>
    public static ISimpleAuthenticationBuilder AddSimpleAuthentication(this AuthenticationBuilder builder, IConfiguration configuration, string sectionName = "Authentication")
    {
        CheckAddJwtBearer(builder, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(builder, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(builder, configuration.GetSection($"{sectionName}:Basic"));

        return new DefaultSimpleAuthenticationBuilder(configuration, builder);

        static void CheckAddJwtBearer(AuthenticationBuilder builder, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(settings.SchemeName, nameof(JwtBearerSettings.SchemeName));
            ArgumentNullException.ThrowIfNull(settings.SecurityKey, nameof(JwtBearerSettings.SecurityKey));
            ArgumentNullException.ThrowIfNull(settings.Algorithm, nameof(JwtBearerSettings.Algorithm));

            builder.Services.Configure<JwtBearerSettings>(section);

            builder.AddJwtBearer(settings.SchemeName, options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = settings.Issuers?.Any() ?? false,
                    ValidIssuers = settings.Issuers,
                    ValidateAudience = settings.Audiences?.Any() ?? false,
                    ValidAudiences = settings.Audiences,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey)),
                    RequireExpirationTime = true,
                    ValidateLifetime = settings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero,
                    ClockSkew = settings.ClockSkew
                };
            });

            if (settings.EnableJwtBearerService)
            {
                builder.Services.TryAddSingleton<IJwtBearerService, JwtBearerService>();
            }
        }

        static void CheckAddApiKey(AuthenticationBuilder builder, IConfigurationSection section)
        {
            var settings = section.Get<ApiKeySettings>();
            if (settings is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(settings.SchemeName, nameof(ApiKeySettings.SchemeName));

            if (string.IsNullOrWhiteSpace(settings.HeaderName) && string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                throw new ArgumentException("You need to specify either a header name or a query string parameter key");
            }

            if (!string.IsNullOrWhiteSpace(settings.ApiKeyValue))
            {
                ArgumentNullException.ThrowIfNull(settings.DefaultUserName, nameof(ApiKeySettings.DefaultUserName));
            }

            builder.Services.Configure<ApiKeySettings>(section);

            builder.AddScheme<ApiKeySettings, ApiKeyAuthenticationHandler>(settings.SchemeName, options =>
            {
                options.SchemeName = settings.SchemeName;
                options.HeaderName = settings.HeaderName;
                options.QueryStringKey = settings.QueryStringKey;
                options.ApiKeyValue = settings.ApiKeyValue;
                options.DefaultUserName = settings.DefaultUserName;
            });
        }

        static void CheckAddBasicAuthentication(AuthenticationBuilder builder, IConfigurationSection section)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(settings.SchemeName, nameof(BasicAuthenticationSettings.SchemeName));

            if (!string.IsNullOrWhiteSpace(settings.UserName))
            {
                ArgumentNullException.ThrowIfNull(settings.Password, nameof(BasicAuthenticationSettings.Password));
            }

            if (!string.IsNullOrWhiteSpace(settings.Password))
            {
                ArgumentNullException.ThrowIfNull(settings.UserName, nameof(BasicAuthenticationSettings.UserName));
            }

            builder.Services.Configure<BasicAuthenticationSettings>(options =>
            {
                options.SchemeName = settings.SchemeName;
                options.UserName = settings.UserName;
                options.Password = settings.Password;
                options.IsEnabled = true;
            });

            builder.AddScheme<BasicAuthenticationSettings, BasicAuthenticationHandler>(settings.SchemeName, options =>
            {
                options.SchemeName = settings.SchemeName;
                options.UserName = settings.UserName;
                options.Password = settings.Password;
            });
        }
    }

    /// <summary>
    /// Adds the <see cref="AuthenticationMiddleware"/> and <see cref="AuthorizationMiddleware"/> middlewares to the
    /// specified <see cref="IApplicationBuilder"/>, which enable authentication and authorization capabilities.
    /// When authorizing a resource that is routed using endpoint routing, this call
    /// must appear between the calls to <c>app.UseRouting()</c> and <c>app.UseEndpoints(...)</c> for
    /// the middleware to function correctly.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>A reference to <paramref name="app"/> after the operation has completed.</returns>
    /// <seealso cref="AuthenticationMiddleware"/>
    /// <seealso cref="AuthorizationMiddleware"/>
    /// <seealso cref="IApplicationBuilder"/>
    public static IApplicationBuilder UseAuthenticationAndAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Adds authentication support in Swagger, enabling the Authorize button in the Swagger UI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, string sectionName = "Authentication")
    {
        // Adds a security definition for each authentication method that has been configured.
        CheckAddJwtBearer(options, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(options, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(options, configuration.GetSection($"{sectionName}:Basic"));

        // This filters automatically adds a security requirement to each endpoint that requires authentication.
        options.OperationFilter<AuthenticationOperationFilter>();

        options.DocumentFilter<ProblemDetailsDocumentFilter>();

        static void CheckAddJwtBearer(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddAuthentication(options, settings.SchemeName, SecuritySchemeType.Http, JwtBearerDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert the Bearer Token");
        }

        static void CheckAddApiKey(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<ApiKeySettings>();
            if (settings is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.HeaderName))
            {
                AddAuthentication(options, $"{settings.SchemeName} in Header", SecuritySchemeType.ApiKey, null, ParameterLocation.Header, settings.HeaderName, "Insert the API Key");
            }

            if (!string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                AddAuthentication(options, $"{settings.SchemeName} in Query String", SecuritySchemeType.ApiKey, null, ParameterLocation.Query, settings.QueryStringKey, "Insert the API Key");
            }
        }

        static void CheckAddBasicAuthentication(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            AddAuthentication(options, settings.SchemeName, SecuritySchemeType.Http, BasicAuthenticationDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert user name and password");
        }

        static void AddAuthentication(SwaggerGenOptions options, string name, SecuritySchemeType securitySchemeType, string? scheme, ParameterLocation location, string parameterName, string description)
            => options.AddSecurityDefinition(name, new OpenApiSecurityScheme
            {
                In = location,
                Name = parameterName,
                Description = description,
                Type = securitySchemeType,
                Scheme = scheme
            });
    }

#if NET7_0_OR_GREATER

    /// <summary>
    /// Adds an OpenAPI annotation that specifies authentication requirements to <see cref="Endpoint.Metadata" /> associated
    /// with the current endpoint, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    /// <seealso cref="IEndpointConventionBuilder"/>
    /// <seealso cref="Endpoint.Metadata"/>
    /// <seealso cref="IConfiguration"/>
    public static TBuilder WithOpenApiAuthentication<TBuilder>(this TBuilder builder, IConfiguration configuration, string sectionName = "Authentication") where TBuilder : IEndpointConventionBuilder
    {
        var securityRequirement = new OpenApiSecurityRequirement();

        // Each method, if the corresponding security scheme is defined, adds a security requirement.
        CheckAddJwtBearer(configuration.GetSection($"{sectionName}:JwtBearer"), ref securityRequirement);
        CheckAddApiKey(configuration.GetSection($"{sectionName}:ApiKey"), ref securityRequirement);
        CheckAddBasicAuthentication(configuration.GetSection($"{sectionName}:Basic"), ref securityRequirement);

        // Adds all the security requirements that have been defined in the configuration.
        builder.WithOpenApi(operation =>
        {
            operation = new(operation)
            {
                Security = { securityRequirement }
            };

            operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), AuthenticationOperationFilter.GetResponse(HttpStatusCode.Unauthorized.ToString()));
            operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), AuthenticationOperationFilter.GetResponse(HttpStatusCode.Forbidden.ToString()));

            return operation;
        });

        return builder;

        static void CheckAddJwtBearer(IConfigurationSection section, ref OpenApiSecurityRequirement securityRequirement)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddAuthentication(settings.SchemeName, ref securityRequirement);
        }

        static void CheckAddApiKey(IConfigurationSection section, ref OpenApiSecurityRequirement securityRequirement)
        {
            var settings = section.Get<ApiKeySettings>();
            if (settings is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.HeaderName))
            {
                AddAuthentication($"{settings.SchemeName} in Header", ref securityRequirement);
            }

            if (!string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                AddAuthentication($"{settings.SchemeName} in Query String", ref securityRequirement);
            }
        }

        static void CheckAddBasicAuthentication(IConfigurationSection section, ref OpenApiSecurityRequirement securityRequirement)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            AddAuthentication(settings.SchemeName, ref securityRequirement);
        }

        static void AddAuthentication(string name, ref OpenApiSecurityRequirement securityRequirement)
        {
            // Creates a security scheme using the information that comes from the configuration.
            // This scheme is then inserted in the security requirement that, in turn, is
            // added as security information to the OpenAPI information of the endpoint.

            var securityScheme = new OpenApiSecurityScheme()
            {
                Reference = new()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = name,
                }
            };

            securityRequirement.Add(securityScheme, Array.Empty<string>());
        }
    }

#endif
}
