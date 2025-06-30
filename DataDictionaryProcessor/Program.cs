using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json; // Add this namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDictionaryProcessor
{
    internal class Program
    {
        static void Main(string[] args)
        {

            #region "Get connection information from appSettings.json"
            // Load configuration from appSettings.json  
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true) // Corrected spelling to 'appSettings.json'
                .Build();

            string CRMURL = configuration["CRMURL"];
            string CLIENTID = configuration["CLIENTID"];
            string CLIENTSECRET = configuration["CLIENTSECRET"];
            string TENANTID = configuration["TENANTID"];

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
                orchestrator.BuildDataDictionary();

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while collecting data: " + ex.Message);
            }



        }
    }
}
