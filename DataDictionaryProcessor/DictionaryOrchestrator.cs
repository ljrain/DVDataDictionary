using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Web.UI.WebControls;

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

        private CrmServiceClient _serviceClientScanner;
        private CrmServiceClient _serviceClientSaver;


        #endregion


        public static void LogEvent(string message,ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }


        public DictionaryOrchestrator(string connectionStringSaver,string connectionStringScanner)
        {

            _serviceClientScanner = new CrmServiceClient(connectionStringScanner);
            if (_serviceClientScanner.IsReady)
            {
                DictionaryOrchestrator.LogEvent("Connected to Dynamics CRM!");
            }
            else
            {
                DictionaryOrchestrator.LogEvent("Failed to connect to Dynamics CRM: " + _serviceClientScanner.LastCrmError);
                throw new Exception("CRM connection failed");
            }
            _serviceClientSaver = new CrmServiceClient(connectionStringSaver);
            if (_serviceClientSaver.IsReady)
            {
                DictionaryOrchestrator.LogEvent("Connected to Dynamics CRM for saving!");
            }
            else
            {
                DictionaryOrchestrator.LogEvent("Failed to connect to Dynamics CRM for saving: " + _serviceClientSaver.LastCrmError);
                throw new Exception("CRM connection for saving failed");
            }
        }


        public void BuildDataDictionary(string[] solutionNames)
        {
            DictionaryOrchestrator.LogEvent("Building Data Dictionary...");

            DateTime startTime = DateTime.Now;
            DvCollector collector = new DvCollector(_serviceClientScanner, solutionNames);
            collector.CollectData();
            var elapsedTime = DateTime.Now.Subtract(startTime).TotalSeconds.ToString("F2");
            DictionaryOrchestrator.LogEvent($"Data collection took {elapsedTime} seconds.",ConsoleColor.Red );

            startTime = DateTime.Now;
            DvProcessor processor = new DvProcessor();
            processor.AllowedLogicalNames = collector.AllowedLogicalNames;
            processor.AllowedTableAttributes = collector.AllowedTableAttributes;
            processor.Modifications = collector.Modifications;
            processor.ProcessData(collector.DDSolutions);
            processor.PrintDataDictionary();

            var totalTime = DateTime.Now.Subtract(startTime).TotalSeconds.ToString("F2");
            DictionaryOrchestrator.LogEvent($"Total time taken for processing: {totalTime} seconds.", ConsoleColor.Red);

            startTime = DateTime.Now;
            // Save to Dataverse
            DvSaver saver = new DvSaver(_serviceClientSaver, processor.DdModel);
            saver.SaveToDataverse();
            DateTime saveStartTime = DateTime.Now;
            DictionaryOrchestrator.LogEvent($"Data saved to Dataverse in {DateTime.Now.Subtract(saveStartTime).TotalSeconds.ToString("F2")} seconds.", ConsoleColor.Red);

            DictionaryOrchestrator.LogEvent("Data Dictionary built successfully!",ConsoleColor.Green);
        }
    }
}
