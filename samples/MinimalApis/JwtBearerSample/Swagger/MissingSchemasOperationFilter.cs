using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JwtBearerSample.Swagger;

internal class MissingSchemasOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters?.Any() ?? false)
        {
            foreach (var parameter in operation.Parameters)
            {
                if (parameter.In is ParameterLocation.Query or ParameterLocation.Path or ParameterLocation.Header)
                {
                    parameter.Content = null;
                }
            }
        }

        if (context.ApiDescription.ParameterDescriptions is not null)
        {
            foreach (var parameterDescription in context.ApiDescription.ParameterDescriptions)
            {
                var schema = GetSchema(parameterDescription);
                if (schema.Format is not null)
                {
                    var parameter = operation.Parameters?.FirstOrDefault(p => p.Name == parameterDescription.Name && p.Schema.Type == "string");
                    if (parameter is not null)
                    {
                        parameter.Schema.Format = schema.Format;
                        parameter.Schema.Example = schema.Example is not null ? new OpenApiString(schema.Example) : null;
                    }
                }
            }
        }

        // We provide a way to return also the example, even if at this moment we actually don't use it.
        static (string? Format, string? Example) GetSchema(ApiParameterDescription parameterDescription)
        {
            string? format = null;
            string? example = null;

            if (parameterDescription.Type == typeof(Guid) || parameterDescription.Type == typeof(Guid?))
            {
                (format, example) = ("uuid", null);
            }
            else if (parameterDescription.Type == typeof(DateTime) || parameterDescription.Type == typeof(DateTime?))
            {
                (format, example) = ("date-time", null);
            }
            else if (parameterDescription.Type == typeof(DateOnly) || parameterDescription.Type == typeof(DateOnly?))
            {
                (format, example) = ("date", null);
            }
            else if (parameterDescription.Type == typeof(TimeOnly) || parameterDescription.Type == typeof(TimeOnly?))
            {
                (format, example) = ("time", null);
            }

            return (format, example);
        }
    }
}
