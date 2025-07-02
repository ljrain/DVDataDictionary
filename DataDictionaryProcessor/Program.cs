using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace DataDictionaryProcessor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            #region "Get connection information from appSettings.json"  
            // Load configuration from appSettings.json  
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .Build();

            string CRMURL = configuration["CRMURL"];
            string CLIENTID = configuration["CLIENTID"];
            string CLIENTSECRET = configuration["CLIENTSECRET"];
            string TENANTID = configuration["TENANTID"];
            //string[] solutions = configuration.GetSection("SOLUTIONS")
            // Get SOLUTIONS as a string array from appSettings.json
            string[] solutions = configuration.GetSection("SOLUTIONS")
                                              .GetChildren()
                                              .Select(x => x.Value)
                                              .ToArray();

            string connectionString = $@"  
                                AuthType=ClientSecret;  
                                Url={CRMURL};  
                                ClientId={CLIENTID};  
                                ClientSecret={CLIENTSECRET};  
                                TenantId={TENANTID};  
                            ";

            #endregion

            DictionaryOrchestrator orchestrator = new DictionaryOrchestrator(connectionString);
            try
            {
                orchestrator.BuildDataDictionary(solutions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while collecting data: " + ex.Message);
            }
        }
    }
}
