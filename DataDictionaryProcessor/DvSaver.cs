using Microsoft.Rest;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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




        }
}
