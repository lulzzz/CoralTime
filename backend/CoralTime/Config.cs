using CoralTime.Common.Constants;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace CoralTime
{
    public class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource("roles", new List<string> { "role" })
            };
        }

        // Api resources.
        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("WebAPI" ) {
                    UserClaims = { JwtClaimTypes.Email, JwtClaimTypes.NickName, JwtClaimTypes.Name, JwtClaimTypes.Role, JwtClaimTypes.Id}
                }
            };
        }

        // Clients want to access resources.
        public static IEnumerable<Client> GetClients(int accessTokenLifetime, int refreshTokenLifetime)
        {
            // Clients credentials.
            return new List<Client>
            {
                // Local authentication client
                new Client
                {
                    ClientId = "coraltimeapp",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword, // Resource Owner Password Credential grant.
                    AllowAccessTokensViaBrowser = true,

                    //AlwaysIncludeUserClaimsInIdToken = true,  // Include claims in token
                    RequireClientSecret = false, // This client does not need a secret to request tokens from the token endpoint.
                    AccessTokenLifetime = accessTokenLifetime,
                    AbsoluteRefreshTokenLifetime = refreshTokenLifetime,
                    RefreshTokenExpiration = TokenExpiration.Absolute,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId, // For UserInfo endpoint.
                        IdentityServerConstants.StandardScopes.Profile,
                        "roles",
                        "WebAPI"
                    },
                    AllowOfflineAccess = true, // For refresh token.
                },

                // Authentication client for Azure AD
                new Client
                {
                    ClientId = "coraltimeazure",
                    RequireClientSecret = false, // This client does not need a secret to request tokens from the token endpoint.

                    //ClientSecrets =
                    //{
                    //    new Secret("secret".Sha256())
                    //},
                    AllowedGrantTypes = GrantTypes.List("azureAuth"),

                    AllowedScopes =
                    {
                       IdentityServerConstants.StandardScopes.OpenId, // For UserInfo endpoint.
                       IdentityServerConstants.StandardScopes.Profile,
                       "roles",
                       "WebAPI"
                    },

                    AccessTokenLifetime = accessTokenLifetime,
                    AbsoluteRefreshTokenLifetime = refreshTokenLifetime,
                    RefreshTokenExpiration = TokenExpiration.Absolute,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    AllowOfflineAccess = true
                }
            };
        }

        public static void CreateAuthorizatoinOptions(AuthorizationOptions options)
        {
            // main Policies
            options.AddPolicy(Constants.AdminRole, policy =>
            {
                policy.RequireClaim("role", Constants.AdminRole);
            });

            options.AddPolicy(Constants.UserRole, policy =>
            {
                policy.RequireClaim("role", Constants.UserRole);
            });
        }
    }
}
