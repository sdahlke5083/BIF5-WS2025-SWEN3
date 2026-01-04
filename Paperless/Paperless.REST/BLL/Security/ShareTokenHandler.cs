using Microsoft.AspNetCore.Authorization;
using Paperless.REST.DAL.Repositories;
using System.Security.Claims;

/// <summary>
/// Handles authorization for shared document access using either user role claims or a document-specific share
/// password.
/// </summary>
/// <remarks><para> <b>ShareTokenHandler</b> enables access to protected documents by evaluating two authorization
/// mechanisms: </para> <list type="bullet"> <item> If the user possesses a role claim, access is granted automatically.
/// </item> <item> If no role claim is present, the handler checks for a valid <c>X-Share-Password</c> header and
/// verifies it against the document's share password. </item> </list> <para> This handler is typically used in
/// scenarios where documents can be shared securely with users who do not have standard authentication credentials.
/// </para> <para> The document identifier is extracted from route values or query parameters. If a valid document ID is
/// not provided, or if the share password is missing or incorrect, authorization fails silently. </para> <para> This
/// class is intended for use with ASP.NET Core's authorization infrastructure and should be registered as part of the
/// application's dependency injection configuration. </para></remarks>
public class ShareTokenHandler : AuthorizationHandler<ShareTokenRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDocumentRepository _documentRepository;

    public ShareTokenHandler(IHttpContextAccessor httpContextAccessor, IDocumentRepository documentRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _documentRepository = documentRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ShareTokenRequirement requirement)
    {
        // 1) Wenn eingeloggter Benutzer mit Role-Claim -> succeed
        var user = context.User;
        var hasRoleClaim = user?.Claims.Any(c =>
            c.Type == ClaimTypes.Role || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)) ?? false;

        if (hasRoleClaim)
        {
            context.Succeed(requirement);
            return;
        }

        // 2) Sonst: Prüfung des X-Share-Password Headers gegen das dokumentenspezifische Passwort aus DB
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        // Versuche document id aus RouteValues oder Query zu lesen
        string? idStr = null;
        if (httpContext.Request.RouteValues.TryGetValue("id", out var r1)) idStr = r1?.ToString();
        if (string.IsNullOrEmpty(idStr) && httpContext.Request.RouteValues.TryGetValue("documentId", out var r2)) idStr = r2?.ToString();
        if (string.IsNullOrEmpty(idStr) && httpContext.Request.RouteValues.TryGetValue("document_id", out var r3)) idStr = r3?.ToString();

        if (string.IsNullOrEmpty(idStr))
        {
            // Fallback: Query string
            if (httpContext.Request.Query.TryGetValue("documentId", out var q1)) idStr = q1.FirstOrDefault();
            if (string.IsNullOrEmpty(idStr) && httpContext.Request.Query.TryGetValue("id", out var q2)) idStr = q2.FirstOrDefault();
        }

        if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var documentId))
        {
            // Kein gültiges DocumentId -> Policy nicht erfüllt
            return;
        }

        // Prüfe Header
        if (!httpContext.Request.Headers.TryGetValue("X-Share-Password", out var headerValues))
            return;

        var provided = headerValues.FirstOrDefault();
        if (string.IsNullOrEmpty(provided))
            return;

        // Hole das dokumentenspezifische Passwort via Repository
        string? expected = null;
        try
        {
            // Erwartete Repository-Methode: GetSharePasswordAsync(Guid)
            expected = await _documentRepository.GetSharePasswordAsync(documentId);
        }
        catch
        {
            // Repository-Aufruf schlägt fehl -> nicht erfolgreich (keine Exception in Handler werfen)
            expected = null;
        }

        if (!string.IsNullOrEmpty(expected) && string.Equals(provided, expected, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
            return;
        }

        // ansonsten: nicht erfolgreich
    }
}
