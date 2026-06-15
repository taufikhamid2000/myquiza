using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyQuiza.Api.Data;

namespace MyQuiza.Api.Auth;

/// <summary>Scoped accessor for the authenticated Supabase user (the JWT `sub` claim).</summary>
public class CurrentUser(IHttpContextAccessor accessor)
{
    public Guid? UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var sub = user?.FindFirstValue("sub") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedAccessException("No authenticated user on the request.");
}

/// <summary>Authorization requirement resolved against platform roles in the DB.</summary>
public sealed class RoleRequirement(string policy) : IAuthorizationRequirement
{
    public string Policy { get; } = policy; // "Admin" | "Moderator"
}

/// <summary>
/// Roles aren't carried in the Supabase JWT, so resolve them from the DB:
/// user_roles.role ('user'|'moderator'|'admin') and user_profiles.school_role ('student'|'teacher'|'admin').
/// </summary>
public class RoleAuthorizationHandler(IServiceScopeFactory scopeFactory)
    : AuthorizationHandler<RoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        var sub = context.User.FindFirstValue("sub")
                  ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var role = await db.UserRoles.Where(r => r.UserId == userId)
            .Select(r => r.Role).FirstOrDefaultAsync();
        var schoolRole = await db.UserProfiles.Where(p => p.Id == userId)
            .Select(p => p.SchoolRole).FirstOrDefaultAsync();

        var isAdmin = role == "admin" || schoolRole == "admin";
        var isModerator = isAdmin || role == "moderator" || schoolRole == "teacher";

        var ok = requirement.Policy switch
        {
            "Admin" => isAdmin,
            "Moderator" => isModerator,
            _ => false,
        };
        if (ok) context.Succeed(requirement);
    }
}

public static class AuthExtensions
{
    /// <summary>
    /// Validates Supabase-issued JWTs. Configure either:
    ///  - Supabase:JwtSecret  (legacy HS256 shared secret), or
    ///  - Supabase:Issuer     (e.g. https://&lt;ref&gt;.supabase.co/auth/v1) for asymmetric keys via JWKS.
    /// Supabase:Audience defaults to "authenticated".
    /// </summary>
    public static IServiceCollection AddSupabaseAuth(this IServiceCollection services, IConfiguration config)
    {
        var issuer = config["Supabase:Issuer"];
        var audience = config["Supabase:Audience"] ?? "authenticated";
        var jwtSecret = config["Supabase:JwtSecret"];

        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddSingleton<IAuthorizationHandler, RoleAuthorizationHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep raw 'sub'
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    // Supabase stamps a `kid` on legacy HS256 tokens, but the symmetric
                    // secret is never published to JWKS. Without this, the handler filters
                    // signing keys by `kid`, finds no match, and fails with
                    // "signature key was not found". Trying all configured keys lets the
                    // shared secret validate the token regardless of the header `kid`.
                    TryAllIssuerSigningKeys = true,
                };

                if (!string.IsNullOrWhiteSpace(jwtSecret))
                {
                    // HS256 path: validate against the project's shared JWT secret.
                    // This is required while the project signs tokens symmetrically
                    // (JWKS is empty). To move to asymmetric keys later, migrate the
                    // Supabase project to JWT signing keys and unset Supabase:JwtSecret
                    // so the issuer/JWKS branch below takes over.
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                }
                else if (!string.IsNullOrWhiteSpace(issuer))
                {
                    // JWKS path (asymmetric keys): only works once Supabase publishes
                    // public keys at /.well-known/jwks.json. An empty JWKS here means the
                    // project is still on the legacy HS256 secret — use Supabase:JwtSecret.
                    options.Authority = issuer;
                    options.MetadataAddress = $"{issuer.TrimEnd('/')}/.well-known/openid-configuration";
                }
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Moderator", p => p.Requirements.Add(new RoleRequirement("Moderator")));
            options.AddPolicy("Admin", p => p.Requirements.Add(new RoleRequirement("Admin")));
        });

        return services;
    }
}
