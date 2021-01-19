using System;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Microsoft.Rest;
using Microsoft.Identity.Client;

namespace ComputerVisionWithAAD
{
    class Program
    {
        private static string clientId = "";
        private static string clientSecret="";
        private static string tenantId="";
        private static string computerVisionEndpoint="";
        static async Task Main(string[] args)
        {
            var token = await GetAccessToken();
            var client = Authenticate(token);
            var imageUrl = "https://www.velvetjobs.com/resume/document-processing-resume-sample.jpg";
            await ReadFileUrl(client, imageUrl);
        }
        private static ComputerVisionClient Authenticate(string token)
        {
            var tokenCredential = new TokenCredentials(token);
            return new ComputerVisionClient(tokenCredential)
                    { 
                        Endpoint = computerVisionEndpoint 
                    };
        }
        private static async Task<string> GetAccessToken()
        {
            var scopes = new string[] {"https://cognitiveservices.azure.com/.default"};
            var msalClient = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg, true)
                .WithTenantId(tenantId)
                .Build();
            var result = await msalClient.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        public static async Task ReadFileUrl(ComputerVisionClient client, string urlFile)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("READ FILE FROM URL");
            Console.WriteLine();

            // Read text from URL
            var textHeaders = await client.ReadAsync(urlFile, language: "en");
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Console.WriteLine($"Extracting text from URL file {Path.GetFileName(urlFile)}...");
            Console.WriteLine();
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

            // Display the found text.
            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    Console.WriteLine(line.Text);
                }
            }
            Console.WriteLine();
        }
    }
}
