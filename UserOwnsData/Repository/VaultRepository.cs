using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace UserOwnsData.Repository
{
    public static class VaultRepository
    {
        /// <summary>
        /// VaultURI is the URL of Azure Key Vault
        /// </summary>
        internal static string VaultURI = ConfigurationManager.AppSettings["VaultUri"];

        /// <summary>
        /// Gets the secret when application user
        /// </summary>
        /// <param name="secretKey">The secret key for key vault</param>
        /// <returns>The secret value corresponding to secret key</returns>
        public static string GetSecret(string secretKey)
        {
            string secretValue = string.Empty;
            if (!string.IsNullOrWhiteSpace(secretKey))
            {
                try
                {
                    AzureServiceTokenProvider provider = new AzureServiceTokenProvider();
                    // Get Key Vault Client
                    KeyVaultClient client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(provider.KeyVaultTokenCallback));
                    // Fetch secret
                    var secret = Task.Run(() => client.GetSecretAsync(VaultURI, secretKey, "")).ConfigureAwait(false).GetAwaiter().GetResult();
                    secretValue = secret.Value;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
            return secretValue;
        }
    }
}