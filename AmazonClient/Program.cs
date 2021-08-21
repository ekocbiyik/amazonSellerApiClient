using Amazon;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SellingPartnerAPIAA;
using RestSharp;
using System;
using System.Threading;

namespace AmazonClient
{
    public class Program
    {
        /*
         
        developer AWS üzerinden IAM user IAM role, policy vb. bilgileri tanımlar
        accessKey ve secretKey bilgisini saklar
        role arn bilgisini seller'a iletir 
        seller, sellerCentral üzerinden APP oluşturur, role arn bilgisini burada APP'e ekler
        APP oluştuktan sonra clientId, clientSecret ve refreshToken bilgilerini developer'a iletir.
        */

        private static string SP_API_URL = "https://sellingpartnerapi-eu.amazon.com"; // REGION bilgisine göre değişir...
        private static string AUTH_ENDPOINT = "https://api.amazon.com/auth/o2/token";
        private static string SP_API_PATH = "/orders/v0/orders";

        // aws üzerinden developer tarafından oluşturulacak bilgiler
        private static string ACCESS_KEY = "AKIA4CHQYJ****XXXX***";
        private static string SECRET_KEY = "HeXXX***ZSgUeuTaqYx0******+XXXXX";
        private static string ROLE_ARN = "arn:aws:iam::616133591461:role/ekocbiyik-role";
        
        // serllerCentral üzerinden APP oluşturularak alınan bilgiler
        private static string CLIENT_ID = "amzn1.application-oa2-client.ekocbiyik1461****";
        private static string CLIENT_SECRET = "8c837XXXX****443b7b96c63bf3e9dc4c487XXX****";
        private static string REFRESH_TOKEN = "Atzr|IwEBIBfnAUW5fQF5VqnVoOtWeqcIHEtO0ZX9aX63grt";

        // seller tarafından dinamik olarak seçilen bilgiler
        private static string REGION = "eu-west-1";
        private static string MARKETPLACE_ID = "A1F83G8C2ARO7P";

        static void Main(string[] args)
        {

            IRestRequest restRequest = new RestRequest(SP_API_PATH, Method.GET);
            var client = new RestClient(SP_API_URL);
            client.UserAgent = "ekocbiyik-APP";

            restRequest.AddHeader("user-agent", "ekocbiyik-APP");

            // kesinlikle büyük/küçük harf duyarlılığına dikkat edilmeli, yoksa Invalid Signature hatası alınıyor!
            restRequest.AddParameter("MarketplaceIds", MARKETPLACE_ID, ParameterType.QueryString); // zorunlu ve önemli alan 
            restRequest.AddParameter("CreatedAfter", DateTime.UtcNow.AddDays(-500).ToString("yyyy-MM-dd"), ParameterType.QueryString);
            restRequest.AddParameter("CreatedBefore", DateTime.UtcNow.ToString("yyyy-MM-dd"), ParameterType.QueryString);

            // accessToken & securityToken
            restRequest = SignWithAccessToken(restRequest, CLIENT_ID, CLIENT_SECRET, REFRESH_TOKEN);
            restRequest = SignWithSTSKeysAndSecurityToken(restRequest, client.BaseUrl.Host, ROLE_ARN, ACCESS_KEY, SECRET_KEY);

            var response = client.Execute(restRequest);
            Console.WriteLine(response.Content);
        }

        private static IRestRequest SignWithAccessToken(IRestRequest restRequest, string clientId, string clientSecret, string refreshToken)
        {
            var lwaAuthorizationCredentials = new LWAAuthorizationCredentials
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Endpoint = new Uri(AUTH_ENDPOINT),
                RefreshToken = refreshToken,
            };
            return new LWAAuthorizationSigner(lwaAuthorizationCredentials).Sign(restRequest);
        }

        private static IRestRequest SignWithSTSKeysAndSecurityToken(IRestRequest restRequest, string host, string roleARN, string accessKey, string secretKey)
        {
            AssumeRoleResponse response = null;
            using (var STSClient = new AmazonSecurityTokenServiceClient(accessKey, secretKey, RegionEndpoint.EUWest1))
            {
                var req = new AssumeRoleRequest()
                {
                    RoleArn = roleARN,
                    DurationSeconds = 950, //put anything you want here
                    RoleSessionName = Guid.NewGuid().ToString()
                };

                response = STSClient.AssumeRoleAsync(req, new CancellationToken()).Result;
            }

            //auth step 3: dönen değerler bizim yeni tokenlarımızı...
            var awsAuthenticationCredentials = new AWSAuthenticationCredentials
            {
                AccessKeyId = response.Credentials.AccessKeyId,
                SecretKey = response.Credentials.SecretAccessKey,
                Region = REGION
            };
            restRequest.AddHeader("x-amz-security-token", response.Credentials.SessionToken);
            return new AWSSigV4Signer(awsAuthenticationCredentials).Sign(restRequest, host);
        }

    }
}
