using DataDictionary.Models;
using DataIngestor.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataIngestor
{
    public class InjestorV2
    {
        public InjestorV2(CrmServiceClient servClient)
        {
            _service = servClient ?? throw new ArgumentNullException(nameof(servClient));
        }

        #region Private Fields

        private CrmServiceClient _service;
        private Dictionary<string, DataDictionarySolution> _ddSolutions = new Dictionary<string, DataDictionarySolution>();


        #endregion


        /// <summary>
        /// Processes the specified solutions by their unique names.
        /// </summary>
        /// <remarks>This method will process a string array of solution unique names to build a data dictionary.</remarks>
        /// <param name="solutionUniqueNames">An array of unique names representing the solutions to be processed.  Each name must be non-null and
        /// non-empty.</param>
        public void ProcessSolutions(string[] solutionUniqueNames)
        {
            Console.WriteLine("Processing Solutions...");
            GetSolutions(solutionUniqueNames);
            Console.WriteLine($"Retrieved {_ddSolutions.Count} solutions.");
            Console.WriteLine("Processing Components in Solutions...");
            foreach (var ddSolution in _ddSolutions.Values)
            {
                Console.WriteLine($"Processing Solution: {ddSolution.UniqueName}");
                GetComponentsInSolution(ddSolution);
                // Additional processing can be added here, such as retrieving entities, attributes, etc.
            }
            ProcessEntities();





            Console.WriteLine("Processing Complete.");
        }


        #region Private Methods

        /// <summary>
        /// Takes in a string array of solution unique names and retrieves the solutions from CRM.
        /// A dictionary of DataDictionarySolution objects is built, keyed by the unique name of the solution.
        /// </summary>
        /// <param name="solutionNames">String array of unique names of solutions to process.</param>
        public void GetSolutions(string[] solutionNames)
        {
            foreach (var solutionName in solutionNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename"),
                    Criteria = new FilterExpression
                    {
                        FilterOperator = LogicalOperator.And
                    }
                };

                query.Criteria.AddCondition(new ConditionExpression
                {
                    AttributeName = "uniquename",
                    Operator = ConditionOperator.Equal,
                    Values = { solutionName }
                });

                var solutions = _service.RetrieveMultiple(query);
                foreach (var solution in solutions.Entities)
                {
                    DataDictionarySolution ddSolution = new DataDictionarySolution
                    {
                        FriendlyName = solution.GetAttributeValue<string>("friendlyname"),
                        SolutionId = solution.GetAttributeValue<Guid>("solutionid").ToString(),
                        UniqueName = solution.GetAttributeValue<string>("uniquename")
                    };
                    _ddSolutions.Add(ddSolution.UniqueName, ddSolution);
                }
            }
        }


        public void GetComponentsInSolution(DataDictionarySolution ddSolution)
        {
            // Query solutioncomponent for forms in the solution
            var componentQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype", "ismetadata", "rootcomponentbehavior", "rootsolutioncomponentid", "solutionid"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("solutionid", ConditionOperator.Equal, new Guid(ddSolution.SolutionId)),
            }
                }
            };

            var components = _service.RetrieveMultiple(componentQuery);
            foreach (var component in components.Entities)
            {
                DataDictionarySolutionComponent ddComponent = new DataDictionarySolutionComponent
                {
                    ObjectId = component.GetAttributeValue<Guid>("objectid"),
                    ComponentType = component.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0,
                    IsMetadata = component.GetAttributeValue<bool>("ismetadata"),
                    //RootComponentBehavior = component.GetAttributeValue<OptionSetValue>("rootcomponentbehavior")?.Value ?? 0,
                    RootSolutionComponentId = component.GetAttributeValue<Guid>("rootsolutioncomponentid")
                };
                ddSolution.AddComponent(ddComponent);

                Console.WriteLine($"Component Type: {ddComponent.ComponentType}, Is Metadata: {ddComponent.IsMetadata}, Object Id: {ddComponent.ObjectId}");
            }
        }


        private void ProcessEntities()
        {
            foreach (DataDictionarySolution ddSolution in _ddSolutions.Values)
            {
                foreach (DataDictionarySolutionComponent ddComponent in ddSolution.Components)
                {
                    if (ddComponent.ComponentType == 1) // Assuming 1 is the type for Entity
                    {
                        // Retrieve entity details and attributes here
                        // This is a placeholder for actual entity processing logic
                        Console.WriteLine($"Processing Entity Component: {ddComponent.ObjectId} in Solution: {ddSolution.UniqueName}");

                        var entityQuery = new QueryExpression("entity")
                        {
                            ColumnSet = new ColumnSet("name", "objecttypecode", "basetablename", "collectionname", "componentstate", "entityid", "entitysetname", "isactivity", "logicalname"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("entityid", ConditionOperator.Equal, ddComponent.ObjectId)
                                }
                            }
                        };

                        EntityCollection results = _service.RetrieveMultiple(entityQuery);
                        foreach (var entity in results.Entities)
                        {
                            DataDictionaryEntity ddEntity = new DataDictionaryEntity
                            {
                                Name = entity.GetAttributeValue<string>("name"),
                                ObjectTypeCode = entity.GetAttributeValue<int>("objecttypecode"),
                                EntityId = entity.GetAttributeValue<Guid>("entityid"),
                                EntitySetName = entity.GetAttributeValue<string>("entitysetname"),
                                BaseTableName = entity.GetAttributeValue<string>("basetablename"),
                                CollectionName = entity.GetAttributeValue<string>("collectionname"),
                                ComponentState = entity.GetAttributeValue<OptionSetValue>("componentstate")?.Value ?? 0,
                                IsActivity = entity.GetAttributeValue<bool>("isactivity"),
                                LogicalName = entity.GetAttributeValue<string>("logicalname")
                            };
                            ddSolution.AddEntity(ddEntity);

                            Console.WriteLine($"Entity: {ddEntity.Name}, Object Type Code: {ddEntity.ObjectTypeCode}, Entity Set Name: {ddEntity.EntitySetName}");

                        }
                    }
                }


            }






            #endregion

        }
    }
}
