using System;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json; // Add this namespace
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Logging;

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
            // Configure Serilog first
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
                .WriteTo.File("logs/dataingestor-.log", 
                    rollingInterval: Serilog.RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
                .Enrich.WithProperty("Application", "DataIngestor")
                .CreateLogger();

            try
            {
                var loggerFactory = LoggerFactory.Create(builder => 
                    builder.AddSerilog());

                var logger = loggerFactory.CreateLogger<Program>();
                
                logger.LogInformation("DataIngestor starting up");

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
                    logger.LogInformation("Running in test mode");
                    JavaScriptParsingTests.RunAllTests();
                    logger.LogInformation("Test mode completed. Press any key to exit");
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
                        logger.LogInformation("Connected to Dynamics CRM successfully. URL: {CrmUrl}", CRMURL);
                        // You can now use serviceClient to interact with CRM  

                        string[] pars = { "SampleSolution" }; // Replace with your actual solution names  

                        DataIngestorOrchestrator injestor = new DataIngestorOrchestrator(serviceClient);
                        injestor.ProcessSolutions(pars);
                    }
                    else
                    {
                        logger.LogError("Failed to connect to CRM: {ErrorMessage}", serviceClient.LastCrmError);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}