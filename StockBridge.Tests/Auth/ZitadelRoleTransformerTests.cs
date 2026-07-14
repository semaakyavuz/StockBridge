using System.Security.Claims;
using StockBridge.API.Auth;

namespace StockBridge.Tests.Auth
{
    public class ZitadelRoleTransformerTests
    {
        private const string RolesClaimType = "urn:zitadel:iam:org:project:roles";

        private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task TransformAsync_SingleRoleClaim_AddsRoleClaim()
        {
            var principal = BuildPrincipal(new Claim(RolesClaimType, "{\"admin\":{\"orgId\":\"1\"}}"));
            var sut = new ZitadelRoleTransformer();

            var result = await sut.TransformAsync(principal);

            Assert.True(result.IsInRole("admin"));
        }

        [Fact]
        public async Task TransformAsync_MultipleRoles_AddsAllRoleClaims()
        {
            var principal = BuildPrincipal(new Claim(
                RolesClaimType,
                "{\"admin\":{\"orgId\":\"1\"},\"user\":{\"orgId\":\"1\"}}"));
            var sut = new ZitadelRoleTransformer();

            var result = await sut.TransformAsync(principal);

            var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            Assert.Contains("admin", roles);
            Assert.Contains("user", roles);
            Assert.Equal(2, roles.Count);
        }

        [Fact]
        public async Task TransformAsync_NoRolesClaim_AddsNoRoleClaims()
        {
            var principal = BuildPrincipal(new Claim(ClaimTypes.Name, "someuser"));
            var sut = new ZitadelRoleTransformer();

            var result = await sut.TransformAsync(principal);

            Assert.Empty(result.FindAll(ClaimTypes.Role));
        }

        [Fact]
        public async Task TransformAsync_MalformedRolesJson_DoesNotThrowAndAddsNoRoleClaims()
        {
            var principal = BuildPrincipal(new Claim(RolesClaimType, "not-valid-json"));
            var sut = new ZitadelRoleTransformer();

            var result = await sut.TransformAsync(principal);

            Assert.Empty(result.FindAll(ClaimTypes.Role));
        }

        [Fact]
        public async Task TransformAsync_RoleClaimAlreadyPresent_DoesNotAddDuplicate()
        {
            var principal = BuildPrincipal(
                new Claim(RolesClaimType, "{\"admin\":{\"orgId\":\"1\"}}"),
                new Claim(ClaimTypes.Role, "admin"));
            var sut = new ZitadelRoleTransformer();

            var result = await sut.TransformAsync(principal);

            Assert.Single(result.FindAll(ClaimTypes.Role));
        }

        [Fact]
        public async Task TransformAsync_EmptyRolesObject_AddsNoRoleClaims()
        {
            var principal = BuildPrincipal(new Claim(RolesClaimType, "{}"));
            var sut = new ZitadelRoleTransformer();

            var result = await sut.TransformAsync(principal);

            Assert.Empty(result.FindAll(ClaimTypes.Role));
        }
    }
}
