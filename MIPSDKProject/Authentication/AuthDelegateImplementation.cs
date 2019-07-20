using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.InformationProtection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace MIPSDKProject
{
    public class AuthDelegateImplementation: IAuthDelegate
    {
        // Set the redirect URI from the AAD Application Registration.
        private static readonly string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static readonly string clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];

        private ApplicationInfo appInfo;
        private readonly string tenantId;
        private TokenCache tokenCache = new TokenCache();

        public AuthDelegateImplementation(ApplicationInfo appInfo, string tenantId)
        {
            this.appInfo = appInfo;
            this.tenantId = tenantId;
        }

        /// <summary>
        /// AcquireToken is called by the SDK when auth is required for an operation. 
        /// Adding or loading an IFileEngine is typically where this will occur first.
        /// The SDK provides all three parameters below.Identity from the EngineSettings.
        /// Authority and resource are provided from the 401 challenge.
        /// The SDK cares only that an OAuth2 token is returned.How it's fetched isn't important.
        /// In this sample, we fetch the token using Active Directory Authentication Library(ADAL).
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="authority"></param>
        /// <param name="resource"></param>
        /// <returns>The OAuth2 token for the user</returns>
        public string AcquireToken(Identity identity, string authority, string resource)
        {
            // Append tenant to authority.
            authority = string.Format("{0}/{1}", authority, tenantId);

            AuthenticationContext authContext = new AuthenticationContext(authority, tokenCache);
            AuthenticationResult result;

            var clientCred = new ClientCredential(appInfo.ApplicationId, clientSecret);
            result = authContext.AcquireTokenAsync(resource, clientCred).Result;

            // Return the token. The token is sent to the resource.
            return result.AccessToken;
        }
    }
}