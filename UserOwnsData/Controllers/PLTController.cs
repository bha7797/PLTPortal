using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Web.Mvc;
using UserOwnsData.Models;
using UserOwnsData.Services.Security;

namespace UserOwnsData.Controllers
{
    public class PLTController : Controller
    {
        private static string getConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["TestDBEntities"].ConnectionString;
        }

        private static AuthDetails getUserDetails()
        {
            var userName = ClaimsPrincipal.Current.FindFirst("name").Value;

            var accessToken = (string)System.Web.HttpContext.Current.Session["AccessToken"];

            var UserEmail = (string)System.Web.HttpContext.Current.Session["UserEmail"];

            AuthDetails authDetails = new AuthDetails
            {
                UserName = userName,
                AccessToken = accessToken,
                UserEmail = UserEmail
            };

            return authDetails;
        }

        public static void storeUserDetails()
        {
            AuthDetails authDetails = getUserDetails();

            string selectQuery = "SELECT userId FROM dbo.UsersInfo where userName = @userName";
            string query;
            using (SqlConnection newConnection = new SqlConnection(getConnectionString()))
            {
                SqlCommand selectCommandSelect = new SqlCommand(selectQuery, newConnection);
                selectCommandSelect.Parameters.AddWithValue("@userName", authDetails.UserName);
                selectCommandSelect.Connection.Open();

                using (SqlDataReader reader = selectCommandSelect.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        query = @"
                                Declare @Token VARCHAR(2000)
                                SELECT @Token = @accessToken
                                Declare @Encrypt varbinary(4000)
                                SELECT @Encrypt = EncryptByPassPhrase('mysupersecretkey', @Token)
                                UPDATE dbo.UsersInfo SET accessToken = @Encrypt WHERE userName = @userName";
                    }
                    else
                    {
                        query = @"Declare @Token VARCHAR(2000)
                                SELECT @Token = @accessToken
                                Declare @Encrypt varbinary(4000)
                                SELECT @Encrypt = EncryptByPassPhrase('mysupersecretkey', @Token)
                                INSERT INTO dbo.UsersInfo (userName, accessToken) VALUES (@userName, @Encrypt)";
                    }
                }
                selectCommandSelect.Connection.Close();
                SqlCommand selectCommand = new SqlCommand(query, newConnection);
                selectCommand.Parameters.AddWithValue("@userName", authDetails.UserName);
                selectCommand.Parameters.AddWithValue("@accessToken", authDetails.AccessToken);
                selectCommand.Parameters.AddWithValue("@key", ConfigurationManager.AppSettings["dbencryptionkey"]);
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
        }

        private static List<PLTModel> getPLTDetails(string selectQuery, List<PLTModel> pltDetails, string workspaceName, string reportName, string userName)
        {
            using (SqlConnection newConnection = new SqlConnection(getConnectionString()))
            {
                SqlCommand selectCommandSelect = new SqlCommand(selectQuery, newConnection);
                selectCommandSelect.Parameters.AddWithValue("@userName", userName);
                if (workspaceName != null)
                {
                    selectCommandSelect.Parameters.AddWithValue("@workspacename", workspaceName);
                    if(reportName != null) selectCommandSelect.Parameters.AddWithValue("@reportname", reportName);
                }
                selectCommandSelect.Connection.Open();

                using (SqlDataReader reader = selectCommandSelect.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            PLTModel pltDetail = new PLTModel
                            {
                                Id = reader.GetInt32(0),
                                WorkspaceName = reader.GetString(1),
                                ReportName = reader.GetString(2),
                                PageName = reader.GetString(3),
                                EndTime = reader.GetString(5),
                                PLT = reader.GetDecimal(6)
                            };
                            pltDetails.Add(pltDetail);
                        }
                    }
                }
                selectCommandSelect.Connection.Close();
            }
            return pltDetails;
        }

        [Authorize]
        // GET: PLT
        public ActionResult PLTView(string workspaceName = null, string reportName = null)
        {
            List<PLTModel> pltDetails = new List<PLTModel>();

            storeUserDetails();

            try
            {
                AuthDetails userDetails = getUserDetails();
                string selectQuery = (workspaceName != null) ?
                    (
                    (reportName != null) ?
                @"SELECT L.Id
	                ,L.WorkspaceName
	                ,L.ReportName
	                ,L.PageName
                    ,COALESCE(P.EndTime, GETDATE()) as OrderDateColumn
	                ,COALESCE(P.EndTime, '')
	                ,COALESCE(P.PLT, 0)
                FROM dbo.LinksInfo L
                LEFT JOIN dbo.PLTTimeDetails P ON P.LinksId = L.Id
                LEFT JOIN dbo.UsersInfo U ON U.userId = L.UserId
                WHERE L.WorkspaceName = @workspacename
	                AND L.ReportName = @reportname
                    AND U.userName = @userName
                ORDER BY OrderDateColumn DESC" :
                @"SELECT L.Id
	                ,L.WorkspaceName
	                ,L.ReportName
	                ,L.PageName
                    ,COALESCE(P.EndTime, GETDATE()) as OrderDateColumn
	                ,COALESCE(P.EndTime, '')
	                ,COALESCE(P.PLT, 0)
                FROM dbo.LinksInfo L
                LEFT JOIN dbo.PLTTimeDetails P ON P.LinksId = L.Id
                LEFT JOIN dbo.UsersInfo U ON U.userId = L.UserId
                WHERE L.WorkspaceName = @workspacename
                    AND U.userName = @userName
                ORDER BY OrderDateColumn DESC"
                    ) :
                @"SELECT L.Id
	                ,L.WorkspaceName
	                ,L.ReportName
	                ,L.PageName
                    ,COALESCE(P.EndTime, GETDATE()) as OrderDateColumn
	                ,COALESCE(P.EndTime, '')
	                ,COALESCE(P.PLT, 0)
                FROM dbo.LinksInfo L
                LEFT JOIN dbo.PLTTimeDetails P ON P.LinksId = L.Id
                LEFT JOIN dbo.UsersInfo U ON U.userId = L.UserId
                WHERE U.userName = @userName
                ORDER BY OrderDateColumn DESC";
                pltDetails = getPLTDetails(selectQuery, pltDetails, workspaceName, reportName, userDetails.UserName);
                AuthPLTViewModel authPLTDetails = new AuthPLTViewModel()
                {
                    PLTModel = pltDetails,
                    AuthDetails = userDetails
                };
                return View(authPLTDetails);
            }
            catch (Exception)
            {
                return View("Error");
            }
        }
    }
}