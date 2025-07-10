using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace DataDictionaryProcessor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Pseudocode:
            // 1. Load configuration from appSettings.json.
            // 2. Read CRMURL, CLIENTID, CLIENTSECRET, TENANTID from DATADICTIONARY section.
            // 3. Read SOLUTIONS array from DATAVERSE section.
            // 4. Use these values as before.

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Get Data Dictionary settings from appSettings.json, this will be used for saving data to Dataverse
            string CRMURL = configuration["DATADICTIONARY:CRMURL"];
            string CLIENTID = configuration["DATADICTIONARY:CLIENTID"];
            string CLIENTSECRET = configuration["DATADICTIONARY:CLIENTSECRET"];
            string TENANTID = configuration["DATADICTIONARY:TENANTID"];
                
            // Get the Dataverse settings to scan for data
            string CRMURLDataverse = configuration["DATAVERSE:CRMURL"];
            string CLIENTIDDataverse = configuration["DATAVERSE:CLIENTID"];
            string CLIENTSECRETDataverse = configuration["DATAVERSE:CLIENTSECRET"];
            string TENANTIDDataverse = configuration["DATAVERSE:TENANTID"];

            // Get SOLUTIONS as a string array from appSettings.json
            string[] solutions = configuration.GetSection("DATAVERSE:SOLUTIONS")
                                    .GetChildren()
                                    .Select(x => x.Value)
                                    .ToArray();

            string connectionStringDataDictionary = $@"  
                                AuthType=ClientSecret;  
                                Url={CRMURL};  
                                ClientId={CLIENTID};  
                                ClientSecret={CLIENTSECRET};  
                                TenantId={TENANTID};  
                            ";

            string connectionStringDataverse = $@"  
                                AuthType=ClientSecret;  
                                Url={CRMURLDataverse};  
                                ClientId={CLIENTIDDataverse};  
                                ClientSecret={CLIENTSECRETDataverse};  
                                TenantId={TENANTIDDataverse};
                            ";

            

            DictionaryOrchestrator orchestrator = new DictionaryOrchestrator(connectionStringDataDictionary, connectionStringDataverse);
            try
            {
                orchestrator.BuildDataDictionary(solutions);
            }
            catch (Exception ex)
            {
                DictionaryOrchestrator.LogEvent("An error occurred while collecting data: " + ex.Message);
            }
        }
    }
}
