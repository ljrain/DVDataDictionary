using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataIngestor.Services
{
    /// <summary>
    /// Interface for Dataverse operations to enable better testing and dependency injection
    /// </summary>
    public interface IDataverseService
    {
        /// <summary>
        /// Retrieves multiple records from Dataverse
        /// </summary>
        EntityCollection RetrieveMultiple(QueryBase query);

        /// <summary>
        /// Executes a batch of organization requests
        /// </summary>
        void ExecuteBatch(List<OrganizationRequest> requests);

        /// <summary>
        /// Creates a record in Dataverse
        /// </summary>
        Guid Create(Entity entity);

        /// <summary>
        /// Updates a record in Dataverse
        /// </summary>
        void Update(Entity entity);

        /// <summary>
        /// Retrieves a single record from Dataverse
        /// </summary>
        Entity Retrieve(string entityName, Guid id, ColumnSet columnSet);
    }
}