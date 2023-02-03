using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web.Mvc;
using UserOwnsData.Models;
using UserOwnsData.Services;
using UserOwnsData.Services.Security;

namespace UserOwnsData.Controllers
{
    public class ReportController : Controller
    {
        private static string getConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["TestDBEntities"].ConnectionString;
        }

        private static AuthDetails getAuthDetails()
        {
            var userName = ClaimsPrincipal.Current.FindFirst("name").Value;

            var accessToken = TokenManager.GetAccessToken(PowerBIPermissionScopes.ReadUserWorkspaces); //(string)System.Web.HttpContext.Current.Session["AccessToken"];

            AuthDetails authDetails = new AuthDetails
            {
                UserName = userName,
                AccessToken = accessToken
            };
            return authDetails;
        }

        /// <summary>
        /// Get UserId from DB where User is current loggedIn User
        /// </summary>
        /// <returns></returns>
        private static int getUserId()
        {
            var userId = 0;
            var userName = ClaimsPrincipal.Current.FindFirst("name").Value;
            string selectQuery = "SELECT userId FROM dbo.UsersInfo where userName = @userName";
            using (SqlConnection newConnection = new SqlConnection(getConnectionString()))
            {
                SqlCommand selectCommandSelect = new SqlCommand(selectQuery, newConnection);
                selectCommandSelect.Parameters.AddWithValue("@userName", userName);
                selectCommandSelect.Connection.Open();
                /*SqlDataReader sqlReader;*/

                using (SqlDataReader reader = selectCommandSelect.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            userId = reader.GetInt32(0);
                        }
                    }
                }
                selectCommandSelect.Connection.Close();
                return userId;
            }
        }
        /// <summary>
        /// Returns Index view to the client after validating app configurations
        /// </summary>
        /// <returns>Returns Index view</returns>
        public ActionResult Reportids(string workspaceId, string reportId, string pageId, string workspaceName, string reportName, string pageName)
        {
            int userId = getUserId();
            String query = "INSERT INTO dbo.LinksInfo (WorkspaceId, ReportId, PageId, WorkspaceName, ReportName, PageName, UserId) VALUES (@WorkspaceId, @ReportId, @PageId, @WorkspaceName, @ReportName, @PageName, @UserId)";
            using (SqlConnection newConnection = new SqlConnection(getConnectionString()))
            {
                SqlCommand selectCommand = new SqlCommand(query, newConnection);
                selectCommand.Parameters.AddWithValue("@WorkspaceId", workspaceId);
                selectCommand.Parameters.AddWithValue("@ReportId", reportId);
                selectCommand.Parameters.AddWithValue("@PageId", pageId);
                selectCommand.Parameters.AddWithValue("@UserId", userId);
                selectCommand.Parameters.AddWithValue("@WorkspaceName", workspaceName);
                selectCommand.Parameters.AddWithValue("@ReportName", reportName);
                selectCommand.Parameters.AddWithValue("@PageName", pageName);
                selectCommand.Connection.Open();
                /*SqlDataReader sqlReader;*/
                try
                {
                    selectCommand.ExecuteNonQuery();
                }
                catch
                {
                    Console.WriteLine("Error occurred while Performing Insert operation.");
                    return Json(new { success = false, responseText = "Error has occured. Please try again!" }, JsonRequestBehavior.AllowGet);
                }
                selectCommand.Connection.Close();
            }

            string configValidationResult = ConfigValidatorService.ValidateConfig();
            if (configValidationResult != null)
            {
                ErrorModel errorModel = Utils.GetErrorModel((HttpStatusCode)400, configValidationResult);
                return Json(new { success = false, responseText = "Error has occured. Please try again!" }, JsonRequestBehavior.AllowGet);
            }else return Json(new { success = true, responseText = "PLT Saved!" }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// This controller gets called if user wants to save all the pages of report at once
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="reportId"></param>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public ActionResult ReportidsWithMultiplePage(string workspaceId, string reportId, string[] pageIDs, string workspaceName, string reportName, string[] pageNames)
        {
            int userId = getUserId();
            String query = "";
            for(int i=0; i<pageIDs.Length; i++)
            {
                query += $"INSERT INTO dbo.LinksInfo (WorkspaceId, ReportId, PageId, WorkspaceName, ReportName, PageName, UserId) VALUES (@WorkspaceId, @ReportId, @PageId{i}, @WorkspaceName, @ReportName, @PageName{i}, @UserId);";
            }
            using (SqlConnection newConnection = new SqlConnection(getConnectionString()))
            {
                SqlCommand selectCommand = new SqlCommand(query, newConnection);
                selectCommand.Parameters.AddWithValue("@WorkspaceId", workspaceId);
                selectCommand.Parameters.AddWithValue("@ReportId", reportId);
                selectCommand.Parameters.AddWithValue("@WorkspaceName", workspaceName);
                selectCommand.Parameters.AddWithValue("@ReportName", reportName);
                for(int i=0; i<pageIDs.Length; i++)
                {
                    selectCommand.Parameters.AddWithValue($"@PageId{i}", pageIDs[i]);
                    selectCommand.Parameters.AddWithValue($"@PageName{i}", pageNames[i]);
                }
                selectCommand.Parameters.AddWithValue("@UserId", userId);
                selectCommand.Connection.Open();
                try
                {
                    selectCommand.ExecuteNonQuery();
                }
                catch
                {
                    Console.WriteLine("Error occurred while Performing Insert operation.");
                }
                selectCommand.Connection.Close();
            }

            string configValidationResult = ConfigValidatorService.ValidateConfig();
            if (configValidationResult != null)
            {
                ErrorModel errorModel = Utils.GetErrorModel((HttpStatusCode)400, configValidationResult);
                return Json(new { success = false, responseText = "Error has occured. Please try again!" }, JsonRequestBehavior.AllowGet);
            }else return Json(new { success = true, responseText = "PLT Saved!" }, JsonRequestBehavior.AllowGet);
        }
    }
}