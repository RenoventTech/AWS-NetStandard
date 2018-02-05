using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using XamarinAWS.Models;

namespace XamarinAWS
{
    static class Credentials
    {
        public static Amazon.RegionEndpoint CognitoEndpoint { get; private set; } = RegionEndpoint.USEast1;
        public static SessionAWSCredentials SessionCreds { get; private set; }
        public enum CredentialsProvider { Amazon, Cognito, Facebook, Google };
        public static string UserIdentityString { get; private set; }        
        private static string CognitoIdentityPoolId = "YOUR_IDENTITY_POOL_ID_HERE";
        private static CognitoAWSCredentials CognitoCredentials { get; set; }

        /// <summary>
        /// Adds login to credentials from Identity Provider
        /// **Note: This has only been verified with Facebook
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="providerToken"></param>
        /// <returns></returns>
        public static Result<bool> AddLoginToCredentials(CredentialsProvider provider, string providerToken)
        {
            Result<bool> r = new Result<bool>();
            string providerPath = "";

            if (CognitoCredentials == null)
            {
                CognitoCredentials = new CognitoAWSCredentials(CognitoIdentityPoolId, CognitoEndpoint);
            };

            switch (provider)
            {
                case CredentialsProvider.Amazon:
                    throw new NotImplementedException();
                case CredentialsProvider.Cognito:
                    throw new NotImplementedException();
                case CredentialsProvider.Facebook:
                    providerPath = "graph.facebook.com";
                    break;
                case CredentialsProvider.Google:
                    throw new NotImplementedException();
                default:
                    break;
            }

            try
            {
                CognitoCredentials.AddLogin(providerPath, providerToken);
            }
            catch (Exception e)
            {
                return r.AsError(e.Message);
            }

            return r.AsSuccess(true);
        }

        public static CognitoAWSCredentials GetCredentials()
        {
            if (CognitoCredentials == null)
            {                
                CognitoCredentials = new CognitoAWSCredentials(CognitoIdentityPoolId, CognitoEndpoint);
            }

            return CognitoCredentials;
        }

        public static (string AccessKey, string SecretKey) GetAccessKeys()
        {
            var credsWithKeys = GetCredentials().GetCredentials();

            return (credsWithKeys.AccessKey, credsWithKeys.SecretKey);
        }
    }
}
