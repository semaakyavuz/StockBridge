using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace StockBridge.API.Auth
{
    public class ZitadelRoleTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = (ClaimsIdentity)principal.Identity!;

            var rolesClaim = identity.FindFirst(
                "urn:zitadel:iam:org:project:roles");

            if (rolesClaim != null)
            {
                try
                {
                    var rolesJson = JsonDocument.Parse(rolesClaim.Value);
                    foreach (var role in rolesJson.RootElement.EnumerateObject())
                    {
                        if (!identity.HasClaim(ClaimTypes.Role, role.Name))
                            identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
                    }
                }
                catch { }
            }

            return Task.FromResult(principal);
        }
    }
}