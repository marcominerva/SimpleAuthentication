using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JwtBearerSample.Swagger;

public class DateTimeParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (context.PropertyInfo == null && parameter.Schema.Type == "string"
            && (context.ApiParameterDescription.Type == typeof(DateTime) || context.ApiParameterDescription.Type == typeof(DateTime?)))
        {
            parameter.Schema.Format = "date-time";
        }
    }
}
