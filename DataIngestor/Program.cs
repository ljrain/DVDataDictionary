using System;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;

namespace DataIngestor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string crmUrl = "https://loudev3.crm.dynamics.com";
            string clientId = "88b9ffc2-7bff-4a5b-8c28-9696906a9d63";
            string clientSecret = "yFD8Q~feXPu58g9_LHWlkwiy1fHiinbvu~xdmb0u";
            string tenantId = "e362c86f-e64a-4e58-b26a-c8bf314b1093";

            string connectionString = $@"  
                    AuthType=ClientSecret;  
                    Url={crmUrl};  
                    ClientId={clientId};  
                    ClientSecret={clientSecret};  
                    TenantId={tenantId};  
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