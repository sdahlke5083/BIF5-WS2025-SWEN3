using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Adds OpenAPI security requirements to operations that require authorization.
/// </summary>
/// <remarks>This operation filter inspects API actions and controllers for the presence of the <c>Authorize</c>
/// attribute. If authorization is required, it adds the specified <see cref="OpenApiSecurityScheme"/> to the
/// operation's security requirements, enabling tools like Swagger UI to prompt for authentication details when testing
/// secured endpoints.</remarks>
public class AuthorizeCheckOperationFilter : IOperationFilter  
{
    private readonly OpenApiSecurityScheme _securityScheme;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeCheckOperationFilter"/> class using the specified security
    /// scheme.
    /// </summary>
    /// <param name="securityScheme">The <see cref="OpenApiSecurityScheme"/> to be used for configuring authorization requirements in the operation
    /// filter. Cannot be <see langword="null"/>.</param>
    public AuthorizeCheckOperationFilter(OpenApiSecurityScheme securityScheme)
    {
        _securityScheme = securityScheme;
    }
    
    /// <summary>
    /// Applies a security requirement to the specified OpenAPI operation if it is decorated with an <c>Authorize</c>
    /// attribute.
    /// </summary>
    /// <remarks>This method checks whether the target operation or its declaring type is marked with the <see
    /// cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/>. If so, it adds a security requirement to the
    /// operation, indicating that authentication is required for this endpoint.</remarks>
    /// <param name="operation">The OpenAPI operation to which the security requirement will be applied.</param>
    /// <param name="context">The context containing metadata about the API operation, including method and type information.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if(context.MethodInfo.DeclaringType is null)
            return;

        var hasAuthorizeTag = context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                              context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();
        if (hasAuthorizeTag)
        {
            if (operation.Security == null)
                operation.Security = new List<OpenApiSecurityRequirement>();
            var securityRequirement = new OpenApiSecurityRequirement
            {
                { _securityScheme, new string[] { } }
            };
            operation.Security.Add(securityRequirement);
        }
    }
}