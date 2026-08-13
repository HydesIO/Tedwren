using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// A reusable endpoint filter that enforces a module entitlement server-side (SF-22, §10.1 Q2). It is the
/// authoritative gate — client navigation only hides unpurchased modules, but every request to a gated group
/// is checked here and <b>fails closed</b>: no resolvable tenant, an error, or a company without the module all
/// yield 403. Apply with <c>group.AddEndpointFilter(ModuleGate.Require("forms"))</c>.
/// </summary>
public static class ModuleGate
{
    /// <summary>Builds an endpoint filter that requires the given module to be enabled for the caller's company.</summary>
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> Require(string moduleKey) =>
        async (context, next) =>
        {
            var services = context.HttpContext.RequestServices;
            var currentUser = services.GetRequiredService<ICurrentUserService>();
            var entitlements = services.GetRequiredService<IEntitlementService>();
            var cancellationToken = context.HttpContext.RequestAborted;

            try
            {
                var user = await currentUser.GetCurrentAsync(cancellationToken);
                if (user.CompanyId is { } companyId && await entitlements.IsEnabledAsync(companyId, moduleKey, cancellationToken))
                {
                    return await next(context);
                }
            }
            catch
            {
                // Fall through to the fail-closed response below (Q2).
            }

            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Module not enabled",
                detail: "This module is not enabled for your organisation.");
        };
}
