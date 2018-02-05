using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Amazon.Runtime;
using System.Threading.Tasks;
using XamarinAWS.Models;
using System.Text;
using System.Linq;

namespace XamarinAWS
{
    public class ApiGateway
    {
        AWSRegion ServiceRegion { get; set; }
        const string ServiceName = "execute-api";
        const string Algorithm = "AWS4-HMAC-SHA256";
        const string ContentType = "application/json";
        string Host = "YOUR_API_ROUTE_HERE";
        const string SignedHeaders = "host;x-amz-date;x-amz-security-token";

        public async Task<Result<string>> RequestGet(string canonicalUri, params KeyValuePair<string, string>[] queryStringParameters)
        {
            Result<string> r = new Result<string>();

            var credentials = await Credentials.GetCredentials().GetCredentialsAsync();

            string hashedRequestPayload = CreateRequestPayload("");

            //Compose Query String
            string canonicalQueryString = "?";
           
            for(var i = 0; i < queryStringParameters.Length; i++)
            {
                canonicalQueryString += queryStringParameters[i].Key + "=" + queryStringParameters[i].Value;

                //if last element, don't append '&' symbol
                if (i != queryStringParameters.Length - 1)
                {
                    canonicalQueryString += "&";
                }
            }            

            string authorization = Sign(credentials, hashedRequestPayload, "GET", canonicalUri, canonicalQueryString);
            string requestDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

            WebRequest webRequest = WebRequest.Create("https://" + Host + canonicalUri + "?" + canonicalQueryString);

            webRequest.Method = "GET";
            webRequest.ContentType = ContentType;
            webRequest.Headers["x-amz-date"] = requestDate;
            webRequest.Headers["Authorization"] = authorization;
            webRequest.Headers["x-amz-content-sha256"] = hashedRequestPayload;
            webRequest.Headers["x-amz-security-token"] = credentials.Token;

            WebResponse returnObject;

            try
            {
                returnObject = await webRequest.GetResponseAsync();
            }
            catch (Exception e)
            {
                return r.AsError(e.Message);
            }

            StreamReader reader = new StreamReader(returnObject.GetResponseStream());

            string responseFromServer = await reader.ReadToEndAsync();

            return r.AsSuccess(responseFromServer.ToString());
        }

        public async Task<Result<string>> RequestPost(string canonicalUri, string jsonString, params KeyValuePair<string, string>[] queryStringParameters)
        {
            Result<string> r = new Result<string>();

            var credentials = await Credentials.GetCredentials().GetCredentialsAsync();

            string hashedRequestPayload = CreateRequestPayload(jsonString);

            //Compose Query String
            string canonicalQueryString = "?";

            for (var i = 0; i < queryStringParameters.Length; i++)
            {
                canonicalQueryString += queryStringParameters[i].Key + "=" + queryStringParameters[i].Value;

                //if last element, don't append '&' symbol
                if (i != queryStringParameters.Length - 1)
                {
                    canonicalQueryString += "&";
                }
            }

            string authorization = Sign(credentials, hashedRequestPayload, "POST", canonicalUri, canonicalQueryString);
            string requestDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "Z";

            WebRequest webRequest = WebRequest.Create("https://" + Host + canonicalUri + "?" + canonicalQueryString);

            webRequest.Method = "POST";
            webRequest.ContentType = ContentType;
            webRequest.Headers["x-amz-date"] = requestDate;
            webRequest.Headers["Authorization"] = authorization;
            webRequest.Headers["x-amz-content-sha256"] = hashedRequestPayload;
            webRequest.Headers["x-amz-security-token"] = credentials.Token;

            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            Stream newStream = await webRequest.GetRequestStreamAsync();
            newStream.Write(data, 0, data.Length);

            WebResponse returnObject;

            try
            {
                returnObject = await webRequest.GetResponseAsync();
            }
            catch (Exception e)
            {
                return r.AsError(e.InnerException.Message);
            }

            StreamReader reader = new StreamReader(returnObject.GetResponseStream());

            string responseFromServer = await reader.ReadToEndAsync();

            return r.AsSuccess(responseFromServer.ToString());
        }

        private string CreateRequestPayload(string jsonString)
        {
            string hashedRequestPayload = HexEncode(Hash(ToBytes(jsonString)));

            return hashedRequestPayload;
        }

        private string Sign(ImmutableCredentials credentialsImmutable, string hashedRequestPayload, string requestMethod, string canonicalUri, string canonicalQueryString)
        {
            var currentDateTime = DateTime.UtcNow;
            var dateStamp = currentDateTime.ToString("yyyMMdd");
            var requestDate = currentDateTime.ToString("yyyyMMddTHHmmssZ");

            var credentialsScope = string.Format("{0}/{1}/{2}/aws4_request", dateStamp, ServiceRegion, ServiceName);

            var headers = new SortedDictionary<string, string>
            {
                {"content-type", ContentType },
                {"host", Host },
                {"x-amz-date", requestDate },
                {"x-amz-security-token", credentialsImmutable.Token }
            };

            string canonicalHeaders = string.Join("\n", headers.Select(x => x.Key.ToLowerInvariant() + ":" + x.Value.Trim())) + "\n";

            // Task 1: Create a Canonical Request For Signature Version 4
            string canonicalRequest = requestMethod + "\n" + canonicalUri + "\n" + canonicalQueryString + "\n" + canonicalHeaders + "\n" + SignedHeaders + "\n" + hashedRequestPayload;
            string hashedCanonicalRequest = HexEncode(Hash(ToBytes(canonicalRequest)));

            // Task 2: Create a String to Sign for Signature Version 4
            string stringToSign = Algorithm + "\n" + requestDate + "\n" + credentialsScope + "\n" + hashedCanonicalRequest;

            // Task 3: Calculate the AWS Signature Version 4
            byte[] signingKey = GetSignatureKey(credentialsImmutable.SecretKey, dateStamp, ServiceRegion, ServiceName);
            string signature = HexEncode(HmacSha256(stringToSign, signingKey));

            // Task 4: Prepare a signed request
            // Authorization: algorithm Credential=access key ID/credential scope, SignedHeadaers=SignedHeaders, Signature=signature

            string authorization = string.Format("{0} Credential={1}/{2}/{3}/{4}/aws4_request, SignedHeaders={5}, Signature={6}",
                Algorithm,
                credentialsImmutable.AccessKey,
                dateStamp,
                ServiceRegion,
                ServiceName,
                SignedHeaders,
                signature
            );

            return authorization;
        }

        private byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
        {
            byte[] kDate = HmacSha256(dateStamp, ToBytes("AWS4" + key));
            byte[] kRegion = HmacSha256(regionName, kDate);
            byte[] kService = HmacSha256(serviceName, kRegion);
            return HmacSha256("aws4_request", kService);
        }

        private byte[] ToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str.ToCharArray());
        }

        private string HexEncode(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        private byte[] Hash(byte[] bytes)
        {
            var hasher = PCLCrypto.WinRTCrypto.HashAlgorithmProvider.OpenAlgorithm(PCLCrypto.HashAlgorithm.Sha256);

            return hasher.HashData(bytes);
        }

        private byte[] HmacSha256(string data, byte[] key)
        {
            var hasher = PCLCrypto.WinRTCrypto.MacAlgorithmProvider.OpenAlgorithm(PCLCrypto.MacAlgorithm.HmacSha256);
            var hashResult = hasher.CreateHash(key);
            hashResult.Append(ToBytes(data));
            byte[] mac = hashResult.GetValueAndReset();

            return mac;
        }
    }
}