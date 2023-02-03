// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// ----------------------------------------------------------------------------

namespace UserOwnsData.Services.Security
{
	using Microsoft.Identity.Client;
	using Microsoft.IdentityModel.Tokens;
	using Microsoft.Owin.Security;
	using Microsoft.Owin.Security.Cookies;
	using Microsoft.Owin.Security.Notifications;
	using Microsoft.Owin.Security.OpenIdConnect;
	using Owin;
	using System;
	using System.Configuration;
	using System.Security.Claims;
	using System.Threading.Tasks;
	using System.Web;
	using UserOwnsData.Repository;

	public class OwinOpenIdConnect
	{
		private static readonly string tenantCommonAuthority = ConfigurationManager.AppSettings["authorityUrl"];
		private static readonly string clientId = ConfigurationManager.AppSettings["clientId"];
		// private static readonly string clientId = ConfigurationManager.AppSettings["clientId"];
		private static readonly string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
		private static readonly string redirectUri = ConfigurationManager.AppSettings["redirectUri"];

		public static void ConfigureAuth(IAppBuilder app)
		{
			app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

			app.UseCookieAuthentication(new CookieAuthenticationOptions());

			app.UseOpenIdConnectAuthentication(
					new OpenIdConnectAuthenticationOptions
					{
						ClientId = clientId,
						Authority = tenantCommonAuthority,
						TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false },
						RedirectUri = redirectUri,
						Scope = "openid email profile " + String.Join(" ", PowerBIPermissionScopes.ReadUserWorkspaces),
						PostLogoutRedirectUri = redirectUri,
						Notifications = new OpenIdConnectAuthenticationNotifications()
						{
							AuthorizationCodeReceived = OnAuthorizationCodeCallback,
                            RedirectToIdentityProvider = (context) =>
                            {
                                // This ensures that the address used for sign in and sign out is picked up dynamically from the request
                                // this allows you to deploy your app (to Azure Web Sites, for example) without having to change settings
                                return Task.FromResult(0);
                            },
                            AuthenticationFailed = context =>
                            {
                                context.HandleResponse();
                                if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["REFERENCE_ID"] != null)
                                    context.Response.Redirect($"/Error?message=Something went Wrong&refid={Convert.ToString(HttpContext.Current.Session["REFERENCE_ID"])}");
                                else
                                    context.Response.Redirect($"/Error?message=Something went Wrong");
                                return Task.FromResult(0);
                            }
                        }
					});
		}

		private static async Task OnAuthorizationCodeCallback(AuthorizationCodeReceivedNotification context)
		{
			try
			{
                ClaimsIdentity userClaims = context.AuthenticationTicket.Identity;
                string userName = userClaims.Name;
                string tenantId = userClaims.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

                // Create URL for tenant-specific authority
                string tenantSpecificAuthority = tenantCommonAuthority.Replace("common", tenantId);

                IConfidentialClientApplication appConfidential = ConfidentialClientApplicationBuilder.Create(clientId)
                                                     .WithClientSecret(clientSecret)
                                                     .WithRedirectUri(redirectUri)
                                                     .WithAuthority(tenantSpecificAuthority)
                                                     .Build();

                MSALPerUserMemoryTokenCache userTokenCache = new MSALPerUserMemoryTokenCache(appConfidential.UserTokenCache);

                string[] scopes = PowerBIPermissionScopes.ReadUserWorkspaces;

                IAccount user = appConfidential.GetAccountAsync(userName).Result;
                HttpContext.Current.Session.Add("ConfidentialClient", appConfidential);
                var authResult = await appConfidential.AcquireTokenByAuthorizationCode(scopes, context.Code).ExecuteAsync();
                HttpContext.Current.Session.Add("AccessToken", authResult.AccessToken);
                HttpContext.Current.Session.Add("UserEmail", authResult.Account.Username);
            }
			catch(Exception e)
			{
				Console.WriteLine(e);
            }
		}
	}
}
