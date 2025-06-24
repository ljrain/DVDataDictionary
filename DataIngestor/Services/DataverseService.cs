using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataIngestor.Services
{
    /// <summary>
    /// Concrete implementation of IDataverseService that wraps the actual Dataverse service
    /// </summary>
    public class DataverseService : IDataverseService
    {
        private readonly IOrganizationService _service;
        private const int DefaultBatchSize = 100;

        public DataverseService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            try
            {
                return _service.RetrieveMultiple(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving multiple records: {ex.Message}");
                throw;
            }
        }

        public void ExecuteBatch(List<OrganizationRequest> requests)
        {
            if (requests == null || !requests.Any())
                return;

            try
            {
                var batch = new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = true,
                        ReturnResponses = false
                    },
                    Requests = new OrganizationRequestCollection()
                };

                foreach (var request in requests)
                {
                    batch.Requests.Add(request);
                }

                var response = (ExecuteMultipleResponse)_service.Execute(batch);

                if (response.IsFaulted)
                {
                    foreach (var responseItem in response.Responses.Where(r => r.Fault != null))
                    {
                        Console.WriteLine($"Batch execution fault: {responseItem.Fault.Message}");
                    }
                }

                Console.WriteLine($"Batch execution completed for {requests.Count} requests");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Batch save failed: {ex.Message}");
                throw;
            }
        }

        public Guid Create(Entity entity)
        {
            try
            {
                return _service.Create(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating record: {ex.Message}");
                throw;
            }
        }

        public void Update(Entity entity)
        {
            try
            {
                _service.Update(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating record: {ex.Message}");
                throw;
            }
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            try
            {
                return _service.Retrieve(entityName, id, columnSet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving record: {ex.Message}");
                throw;
            }
        }
    }
}