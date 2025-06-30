using System;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json; // Add this namespace

namespace DataIngestor
{
    internal class Program
    {
        public static string CRMURL;
        public static string CLIENTID;
        public static string CLIENTSECRET;
        public static string TENANTID;

        static void Main(string[] args)
        {
            // Load configuration from appsettings.json  
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Requires Microsoft.Extensions.Configuration.Json
                .Build();

            CRMURL = configuration["CRMURL"];
            CLIENTID = configuration["CLIENTID"];
            CLIENTSECRET = configuration["CLIENTSECRET"];
            TENANTID = configuration["TENANTID"];

            // Check if test mode is requested  
            if (args.Length > 0 && args[0].ToLower() == "test")
            {
                Console.WriteLine("Running in test mode...");
                //JavaScriptParsingTests.RunAllTests(); // Restore the test run
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            string connectionString = $@"  
                            AuthType=ClientSecret;  
                            Url={CRMURL};  
                            ClientId={CLIENTID};  
                            ClientSecret={CLIENTSECRET};  
                            TenantId={TENANTID};  
                        ";

            using (var serviceClient = new CrmServiceClient(connectionString))
            {
                if (serviceClient.IsReady)
                {
                    Console.WriteLine("Connected to Dynamics CRM!");
                    // You can now use serviceClient to interact with CRM  

                    string[] pars = { "SampleSolution" }; // Replace with your actual solution names  

                    InjestorV2 injestor = new InjestorV2(serviceClient);
                    injestor.ProcessSolutions(pars);
                }
                else
                {
                    Console.WriteLine("Failed to connect: " + serviceClient.LastCrmError);
                }
            }
        }
    }
}