using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TheFabricScript.API.Filters;

/// <summary>
/// Swagger operation filter that enriches API documentation with:
/// <list type="bullet">
///   <item>Deprecation notices</item>
///   <item>Default parameter examples from route/query data</item>
///   <item>Standardised error response schemas (400, 401, 403, 404, 500)</item>
/// </list>
/// Registered in <c>Program.cs</c> via <c>c.OperationFilter&lt;SwaggerDefaultValues&gt;()</c>.
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        // Mark deprecated actions
        operation.Deprecated |= apiDescription.IsDeprecated();

        // Fill in parameter descriptions from ApiParameterDescription
        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse
                ? "default"
                : responseType.StatusCode.ToString();

            if (!operation.Responses.TryGetValue(responseKey, out var response))
                continue;

            foreach (var contentType in response.Content.Keys
                .Where(k => responseType.ApiResponseFormats.All(f => f.MediaType != k)))
            {
                response.Content.Remove(contentType);
            }
        }

        // Add standard error responses to every operation
        AddStandardErrorResponses(operation);
    }

    private static void AddStandardErrorResponses(OpenApiOperation operation)
    {
        var errorSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["statusCode"] = new() { Type = "integer", Example = new OpenApiInteger(400) },
                ["message"]    = new() { Type = "string",  Example = new OpenApiString("An error occurred") },
                ["timestamp"]  = new() { Type = "string",  Format = "date-time" }
            }
        };

        var errorContent = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new() { Schema = errorSchema }
        };

        // Only add if not already defined by ProducesResponseType attributes
        operation.Responses.TryAdd("400", new OpenApiResponse
        {
            Description = "Bad Request — invalid input data",
            Content = errorContent
        });
        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized — missing or invalid JWT token"
        });
        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "Forbidden — insufficient role permissions"
        });
        operation.Responses.TryAdd("500", new OpenApiResponse
        {
            Description = "Internal Server Error — unexpected server-side failure",
            Content = errorContent
        });
    }
}
