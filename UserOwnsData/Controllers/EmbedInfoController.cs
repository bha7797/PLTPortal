// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// ----------------------------------------------------------------------------

namespace UserOwnsData.Controllers
{
	using System;
	using System.Net;
	using System.Security.Claims;
	using System.Web.Mvc;
	using UserOwnsData.Models;
	using UserOwnsData.Services;
	using UserOwnsData.Services.Security;
    using System.Data.SqlClient;

    public class EmbedInfoController : Controller
	{
        /// <summary>
        /// Returns Embed view when client is authorized
        /// </summary>
        /// <returns>Returns Embed view with authentication details</returns>
        [Authorize]
		public ActionResult Embed()
		{
			try
			{
				var userName = ClaimsPrincipal.Current.FindFirst("name").Value;

				 var accessToken = TokenManager.GetAccessToken(PowerBIPermissionScopes.ReadUserWorkspaces);
				// var accessToken = (string)System.Web.HttpContext.Current.Session["AccessToken"];

                AuthDetails authDetails = new AuthDetails
				{
					UserName = userName,
					UserEmail= (string)System.Web.HttpContext.Current.Session["UserEmail"],
					AccessToken = accessToken
				};

                return View("embed", authDetails);
			}
			catch (Exception ex)
			{
				ErrorModel errorModel = Utils.GetErrorModel((HttpStatusCode)500, ex.ToString());
				return View("Error", errorModel);
			}
		}
	}
}
