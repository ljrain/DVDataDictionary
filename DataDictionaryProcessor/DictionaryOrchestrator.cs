using Microsoft.Xrm.Tooling.Connector;
using System;

namespace DataDictionaryProcessor
{
    public class DictionaryOrchestrator
    {

        /**
         * 1) Collect all metadata and store in models
         * 2) Parse JavaScript files and extract relevant data
         * 3) Corelate metadata with parsed data
         * 4) Save to Dataverse
         * 
         */

        #region "Private Fields"

        private CrmServiceClient _serviceClient;


        #endregion


        public DictionaryOrchestrator(string connectionString)
        {

            _serviceClient = new CrmServiceClient(connectionString);
            if (_serviceClient.IsReady)
            {
                Console.WriteLine("Connected to Dynamics CRM!");
            }
            else
            {
                Console.WriteLine("Failed to connect to Dynamics CRM: " + _serviceClient.LastCrmError);
                throw new Exception("CRM connection failed");
            }
        }


        public void BuildDataDictionary()
        {
            Console.WriteLine("Building Data Dictionary...");

            DateTime startTime = DateTime.Now;
            DvCollector collector = new DvCollector(_serviceClient);
            collector.CollectData();
            var elapsedTime = DateTime.Now.Subtract(startTime).TotalSeconds.ToString("F2");
            Console.WriteLine($"Data collection took {elapsedTime} seconds.");

            startTime = DateTime.Now;
            DvProcessor processor = new DvProcessor();
            processor.AllowedLogicalNames = collector.AllowedLogicalNames;
            processor.AllowedTableAttributes = collector.AllowedTableAttributes;
            processor.Modifications = collector.Modifications;
            processor.ProcessData(collector.DDSolutions);
            processor.PrintDataDictionary();

            var totalTime = DateTime.Now.Subtract(startTime).TotalSeconds.ToString("F2");
            Console.WriteLine($"Total time taken for processing: {totalTime} seconds.");

            startTime = DateTime.Now;
            // Save to Dataverse
            DvSaver saver = new DvSaver(_serviceClient, processor.DdModel);
            saver.SaveToDataverse();
            Console.WriteLine($"Data saved to Dataverse in {DateTime.Now.Subtract(startTime).TotalSeconds.ToString("F2")} seconds.");


            Console.WriteLine("Data Dictionary built successfully!");
        }
    }
}
