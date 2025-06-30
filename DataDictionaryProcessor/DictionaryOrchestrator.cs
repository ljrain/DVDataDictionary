using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // This method will orchestrate the collection of data and building of the data dictionary.
            // It will call methods from DvCollector to collect metadata and parse JavaScript files.
            // It will also handle the correlation of metadata with parsed data and saving to Dataverse.
            Console.WriteLine("Building Data Dictionary...");
            // Create an instance of DvCollector and call its methods
            DvCollector collector = new DvCollector(_serviceClient);
            collector.CollectData();
            // Further implementation goes here...

            DvProcessor processor = new DvProcessor();
            processor.AllowedLogicalNames = collector.AllowedLogicalNames;
            processor.AllowedTableAttributes = collector.AllowedTableAttributes;
            processor.ProcessData(collector.DDSolutions);
            processor.PrintDataDictionary();

            Console.WriteLine("Data Dictionary built successfully!");
        }
    }
}
