using Microsoft.Rest;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;

namespace DataDictionaryProcessor
{
    public class DvSaver
    {


        public DvSaver(CrmServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }


        #region Private Fields

        private CrmServiceClient _serviceClient;
        DataDictionary.Models.DataDictionary _ddModel = new DataDictionary.Models.DataDictionary();


        #endregion


        public void SaveToDataverse(DataDictionary.Models.DataDictionary ddModel)
        {
            _ddModel = ddModel;
            if (_serviceClient == null || !_serviceClient.IsReady)
            {
                throw new InvalidOperationException("Service client is not initialized or not connected.");
            }
            try
            {
                // Save entities
                foreach (var entity in _ddModel.Entities.Values)
                {
                    // Logic to save entity to Dataverse
                    Console.WriteLine($"Saving entity: {entity.EntitySetName}");

                    foreach (var attribute in entity.Attributes)
                    {
                        // Logic to save attribute to Dataverse
                        Console.WriteLine($"  Saving attribute: {attribute.AttributeName}");

                        foreach (var modification in attribute.Modifications)
                        {
                            // Logic to save modification to Dataverse
                            Console.WriteLine($"    Saving modification: {modification.FieldName} with value {modification.FieldName}");
                        }
                    }
                }
                Console.WriteLine("Data dictionary saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data dictionary: {ex.Message}");
            }

        }





        #region Private Methods

        private void SaveToDataverse()
        {
            // Use ExecuteMultipleRequest for batch saving to Dataverse
            var batchSize = 1000; // Adjust as needed for performance and Dataverse limits

            // Track alternate key values to detect duplicates in the current batch
            var seenAltKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entity in _ddModel.Entities)
            {
                foreach (var attribute in entity.Value.Attributes)
                {
                    // Construct the alternate key value
                    var altKey = $"{entity.Key}-{attribute.AttributeName}";
                    if (string.IsNullOrWhiteSpace(altKey))
                    {
                        Console.WriteLine("Skipping record with null/empty alternate key.");
                        continue;
                    }
                    // Check for duplicate alternate key in the current batch
                    if (!seenAltKeys.Add(altKey))
                    {
                        Console.WriteLine($"Duplicate alternate key detected in batch: '{altKey}'. Skipping this record.");
                        continue;
                    }
                    var attr = new Entity("ljr_datadictionaryattributemetadata");
                    attr["ljr_name"] = attribute.AttributeName;
                    attr["ljr_entityname"] = entity.Key;
                    attr["ljr_entitysetname"] = entity.Value.EntitySetName;
                    attr["ljr_objecttypecode"] = entity.Value.ObjectTypeCode;
                    attr["ljr_logicalname"] = attribute.LogicalName;
                    
                    // Add any additional fields as needed
                    




                }


                //Console.WriteLine($"Processing entity: {entity.EntitySetName}");
            }

            #endregion
        }

    }
}
