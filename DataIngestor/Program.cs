using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using DataIngestor.Tests;

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
            Console.WriteLine("=== Data Dictionary Ingestor (Refactored) ===");
            
            // Check for command line arguments
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "test":
                        RunTests();
                        return;
                    case "help":
                        ShowHelp();
                        return;
                }
            }

            // Load configuration from appsettings.json  
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            CRMURL = configuration["CRMURL"];
            CLIENTID = configuration["CLIENTID"];
            CLIENTSECRET = configuration["CLIENTSECRET"];
            TENANTID = configuration["TENANTID"];

            if (string.IsNullOrEmpty(CRMURL) || string.IsNullOrEmpty(CLIENTID))
            {
                Console.WriteLine("Configuration missing. Please check appsettings.json");
                Console.WriteLine("Run 'DataIngestor.exe help' for usage information");
                return;
            }

            string connectionString = $@"  
                            AuthType=ClientSecret;  
                            Url={CRMURL};  
                            ClientId={CLIENTID};  
                            ClientSecret={CLIENTSECRET};  
                            TenantId={TENANTID};  
                        ";

            try
            {
                using (var serviceClient = new CrmServiceClient(connectionString))
                {
                    if (serviceClient.IsReady)
                    {
                        Console.WriteLine("✅ Connected to Dataverse successfully!");
                        
                        // Get solution names from user or use defaults
                        string[] solutionNames = GetSolutionNames(args);
                        
                        // Create and run the refactored injestor
                        var injestor = new InjestorV2(serviceClient);
                        
                        Console.WriteLine($"Processing {solutionNames.Length} solution(s)...");
                        injestor.ProcessSolutions(solutionNames);
                        
                        // Display results
                        var stats = injestor.GetProcessingStatistics();
                        DisplayResults(stats);
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to connect to Dataverse: " + serviceClient.LastCrmError);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine("Run 'DataInjestor.exe test' to verify functionality");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void RunTests()
        {
            Console.WriteLine("=== Running Test Suite ===");
            
            try
            {
                // Run original JavaScript tests
                Console.WriteLine("\n--- Original JavaScript Parsing Tests ---");
                JavaScriptParsingTests.RunAllTests();
                
                // Run refactored component tests
                Console.WriteLine("\n--- Refactored Component Tests ---");
                RefactoredIngestorTests.RunAllTests();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test execution failed: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Data Dictionary Ingestor - Usage Information");
            Console.WriteLine("============================================");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  DataInjestor.exe [command] [solution1] [solution2] ...");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  test     - Run the comprehensive test suite");
            Console.WriteLine("  help     - Show this help information");
            Console.WriteLine("  (none)   - Run data dictionary processing");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  DataInjestor.exe test");
            Console.WriteLine("  DataInjestor.exe MySolution AnotherSolution");
            Console.WriteLine("  DataInjestor.exe");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine("  Edit appsettings.json to configure Dataverse connection");
            Console.WriteLine();
            Console.WriteLine("For more information, see docs/DeveloperGuide.md");
        }

        private static string[] GetSolutionNames(string[] args)
        {
            // If solutions are provided as command line arguments (after the first arg)
            if (args.Length > 1)
            {
                var solutions = new string[args.Length - 1];
                Array.Copy(args, 1, solutions, 0, args.Length - 1);
                return solutions;
            }
            
            // If no arguments provided, ask user or use default
            if (args.Length == 0)
            {
                Console.WriteLine("Enter solution unique names (comma-separated) or press Enter for default:");
                var input = Console.ReadLine();
                
                if (!string.IsNullOrWhiteSpace(input))
                {
                    return input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim())
                              .ToArray();
                }
            }
            
            // Default solution
            return new[] { "SampleSolution" };
        }

        private static void DisplayResults(Dictionary<string, object> stats)
        {
            Console.WriteLine("\n=== Processing Results ===");
            Console.WriteLine($"📊 Total Solutions: {stats["TotalSolutions"]}");
            Console.WriteLine($"📋 Total Entities: {stats["TotalEntities"]}");
            Console.WriteLine($"🔧 Total Components: {stats["TotalComponents"]}");
            Console.WriteLine($"📜 Total Web Resources: {stats["TotalWebResources"]}");
            Console.WriteLine($"✏️  Total Field Modifications: {stats["TotalFieldModifications"]}");
            Console.WriteLine();
            Console.WriteLine("✅ Data Dictionary processing completed successfully!");
        }
    }
}