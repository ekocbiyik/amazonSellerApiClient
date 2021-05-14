using Amazon;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SellingPartnerAPIAA;
using RestSharp;
using System;
using System.Net;

namespace AmazonSP_API
{
    public class Program
    {
        static readonly string OAUTH_URL = "https://api.amazon.com/auth/o2/token";
        static readonly string BASE_URL = "https://sellingpartnerapi-na.amazon.com";
        static readonly string ENDPOINT = "/vendor/directFulfillment/orders/v1/purchaseOrders";
        static readonly string CLIENT_ID = "my_client_id_from_seller_api";
        static readonly string CLIENT_SECRET = "my_client_secret_from_seller_api";
        static string ACCESS_KEY = "my_access_key_from_aws_console";
        static string SECRET_KEY = "my_secret_key_from_aws_console";
        static readonly string ROLE_ARN = "my_ROLE_arn_from_aws_console";
        static readonly string APPLICATION_ID = "my_application_id_from_seller_api";
        static readonly string REGION = "us-east-1";
        static string REFRESH_TOKEN = "my_refresh_token_from_seller_api";
        static string ACCESS_TOKEN = "";
        static string SESSION_TOKEN = "";


        static void Main(string[] args)
        {

            // access token
            LWAAuthorizationCredentials lwaAuthCreds = new LWAAuthorizationCredentials
            {
                Scopes = new System.Collections.Generic.List<string>() { ScopeConstants.ScopeNotificationsAPI },
                ClientId = CLIENT_ID,
                ClientSecret = CLIENT_SECRET,
                Endpoint = new Uri(OAUTH_URL)
            };
            ACCESS_TOKEN = new LWAClient(lwaAuthCreds).GetAccessToken();


            // ROLE_ARN - SECURITY_TOKEN
            if (false)
            {
                AssumeRoleRequest assumeRoleRequest = new AssumeRoleRequest()
                {
                    RoleArn = ROLE_ARN,
                    RoleSessionName = Guid.NewGuid().ToString()
                };

                AssumeRoleResponse assumeRoleResponse = new AmazonSecurityTokenServiceClient(
                    ACCESS_KEY,
                    SECRET_KEY,
                    RegionEndpoint.GetBySystemName(REGION)).AssumeRoleAsync(assumeRoleRequest).Result;

                if (assumeRoleResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("ERROR!");
                    return;
                }

                ACCESS_KEY = assumeRoleResponse.Credentials.AccessKeyId;
                SECRET_KEY = assumeRoleResponse.Credentials.SecretAccessKey;
                SESSION_TOKEN = assumeRoleResponse.Credentials.SessionToken;

            }
            else
            {
                GetSessionTokenResponse sessionToken = new AmazonSecurityTokenServiceClient(
                ACCESS_KEY,
                SECRET_KEY,
                RegionEndpoint.GetBySystemName(REGION)).GetSessionTokenAsync().Result;

                if (sessionToken.HttpStatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("ERROR!");
                    return;
                }

                ACCESS_KEY = sessionToken.Credentials.AccessKeyId;
                SECRET_KEY = sessionToken.Credentials.SecretAccessKey;
                SESSION_TOKEN = sessionToken.Credentials.SessionToken;

            }

            RestClient restClient = new RestClient(BASE_URL);
            restClient.UserAgent = "Amazon Client";

            IRestRequest restRequest = new RestRequest(ENDPOINT, Method.GET);

            restRequest.AddHeader("x-amz-access-token", ACCESS_TOKEN);
            restRequest.AddHeader("x-amz-security-token", SESSION_TOKEN);
            restRequest.AddHeader("user-agent", "Amazon Client");
            //restRequest.AddHeader("Content-type", "application/x-www-form-urlencoded");

            AWSAuthenticationCredentials awsAuthCreds = new AWSAuthenticationCredentials
            {
                AccessKeyId = ACCESS_KEY,
                SecretKey = SECRET_KEY,
                Region = REGION
            };

            restRequest = new AWSSigV4Signer(awsAuthCreds).Sign(restRequest, restClient.BaseUrl.Host);
            var result = restClient.Execute(restRequest);

            foreach (var item in restRequest.Parameters)
            {
                Console.WriteLine(item.Name + ":" + item.Value);
            }

            Console.WriteLine("\n");
            Console.WriteLine(result.Content);
            Console.WriteLine(result.StatusCode);

        }

    }
}
