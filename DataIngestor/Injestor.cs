using DataDictionary.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;

namespace DataIngestor
{

    /// <summary>
    /// this class is intended to extract data dictionary information from Dataverse solutions.
    /// 1) Get Solution IDs from unique solution names
    /// 2) Get all components in the solution
    /// 3) Get all attributes in the solution
    /// 4) Get all forms in the solution
    /// 5) Get all web resources in the solution
    /// </summary>
    public class Injestor
    {
        public Injestor(CrmServiceClient servClient)
        {
            // Constructor logic can be added here if needed
            _service = servClient ?? throw new ArgumentNullException(nameof(servClient));
        }

        private CrmServiceClient _service;
        private Dictionary<string, DataDictionarySolution> _solutions = new Dictionary<string, DataDictionarySolution>();


        public void Process(string[] uniqueNames)
        {

            GetSolutions(uniqueNames);

            foreach (var solution in _solutions.Values)
            {
                GetComponentsInSolution(solution);
                Console.WriteLine("Collected solutions");

                GetEntitiesInSolution(solution);
                Console.WriteLine("Collected entities");

                GetAttributesInSolution(solution);
                Console.WriteLine("Collected attributes");

                //GetFormsInSolution(solution.SolutionId);
                //Console.WriteLine("Collected forms");
            }


        }


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
                        FriendlyName = solution.GetAttributeValue<string>("uniquename"),
                        SolutionId = solution.GetAttributeValue<Guid>("solutionid").ToString(),
                        UniqueName = solution.GetAttributeValue<string>("uniquename")
                    };
                    _solutions.Add(solutionName, ddSolution);
                }
            }
            Console.WriteLine("Solutions retrieved: " + _solutions.Count);
        }

        /// <summary>
        /// get all solution components for a given solution ID.
        /// </summary>
        /// <param name="ddSolution"></param>
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
                _solutions[ddSolution.UniqueName].AddComponent(ddComponent);

                Console.WriteLine($"Component Type: {ddComponent.ComponentType}, Is Metadata: {ddComponent.IsMetadata}, Root Component Behavior: {ddComponent.RootSolutionComponentId}");
            }
        }

        /// <summary>
        /// get all entities in the solution based on the components returned
        /// </summary>
        /// <param name="solutionId"></param>
        public void GetEntitiesInSolution(DataDictionarySolution ddSolution)
        {
            // first check for any entities that are part of a solution by looking at the solutionid
            foreach (var solution in _solutions.Values.Where(s => s.SolutionId == ddSolution.SolutionId))
            {
                var entityQuery = new QueryExpression("entity")
                {
                    ColumnSet = new ColumnSet("entityid", "entitysetname", "basetablename", "collectionname", "componentstate", "extensiontablename", "externalcollectionname", "externalname", "isactivity", "logicalcollectionname", "logicalname"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("solutionid", ConditionOperator.Equal, new Guid(ddSolution.SolutionId))
                        }
                    }
                };
                var entities = _service.RetrieveMultiple(entityQuery);
                foreach (var entity in entities.Entities)
                {
                    var newEntity = new DataDictionaryEntity
                    {
                        EntityId = entity.GetAttributeValue<Guid>("entityid"),
                        EntitySetName = entity.GetAttributeValue<string>("entitysetname"),
                        BaseTableName = entity.GetAttributeValue<string>("basetablename"),
                        CollectionName = entity.GetAttributeValue<string>("collectionname"),
                        ComponentState = entity.GetAttributeValue<int>("componentstate"),
                        ExtensionTableName = entity.GetAttributeValue<string>("extensiontablename"),
                        ExternalCollectionName = entity.GetAttributeValue<string>("externalcollectionname"),
                        ExternalName = entity.GetAttributeValue<string>("externalname"),
                        IsActivity = entity.GetAttributeValue<bool>("isactivity"),
                        LogicalCollectionName = entity.GetAttributeValue<string>("logicalcollectionname"),
                        LogicalName = entity.GetAttributeValue<string>("logicalname")
                    };

                    solution.AddEntity(newEntity);
                    Console.WriteLine($"Added Entity: {newEntity.EntitySetName} with ID: {newEntity.EntityId}");
                }
            }

            // second, check all components that have a componenttype of 1, which is Entity and a rootComponentBehavior of 1, which is Entity
            string[] objectids = GetComponentsOfTypeAndBehavior();
            GetEntitiesByObjectIds(objectids,ddSolution);
            Console.WriteLine($"Total Entities Collected: {_solutions.Values.Sum(s => s.Entities.Count())}");
        }


        public void GetAttributesInSolution(DataDictionarySolution ddSolution)
        {
            var attributeQuery = new QueryExpression("attribute")
            {
                ColumnSet = new ColumnSet("attributeid", "name", "attributeof"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, new Guid(ddSolution.SolutionId))
                    }
                }
            };

            var attributes = _service.RetrieveMultiple(attributeQuery);
            foreach (var attribute in attributes.Entities)
            {
                DataDictionaryAttribute ddAttribute = new DataDictionaryAttribute();
                ddAttribute.AttributeId = attribute.GetAttributeValue<Guid>("attributeid"); // Fix for CS0029: Use Guid directly
                ddAttribute.LogicalName = attribute.GetAttributeValue<string>("logicalname");
                ddAttribute.AttributeOf = attribute.GetAttributeValue<Guid>("attributeof");

                // Assuming AddAttribute is a valid method in ddSolution
                ddSolution.AddAttribute(ddSolution, ddAttribute); 
                Console.WriteLine($"Added Attribute: {ddAttribute.AttributeName} with ID: {ddAttribute.AttributeId}");
            }
        }
        public void GetFormsInSolution(string solutionId)
        {
        }
        public string[] GetComponentsOfTypeAndBehavior(int componentType = 1, int rootComponentBehavior = 1)
        {
            var result = new List<DataDictionarySolutionComponent>();
            foreach (var solution in _solutions.Values)
            {
                if (solution.Components == null)
                    continue;

                var matchingComponents = solution.Components
                    .Where(c => c.ComponentType == componentType); //&& c.RootComponentBehavior == rootComponentBehavior);

                result.AddRange(matchingComponents);
            }

            string[] objectids = result.ToList().Select(c => c.ObjectId.ToString()).ToArray();



            Console.WriteLine($"Collected Object IDs: {string.Join(", ", objectids)}");

            return objectids;
        }
        // Pseudocode plan:
        // 1. Accept a string array of objectIds (as string representations of GUIDs).
        // 2. Build a QueryExpression for the "entity" table.
        // 3. Add a filter with a ConditionExpression using ConditionOperator.In for "entityid".
        // 4. Convert the string array to Guid array.
        // 5. Set the ColumnSet to the desired columns.
        // 6. Execute the query using _service.RetrieveMultiple.
        // 7. Return the EntityCollection or process as needed.

        public void GetEntitiesByObjectIds(string[] objectIds,DataDictionarySolution ddSolution)
        {
            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("solutioncomponentid", "objectid", "componenttype", "rootcomponentbehavior", "ismetadata"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("solutionid", ConditionOperator.Equal, new Guid("68ef2eef-4d41-f011-877a-6045bd01a1bd")),
                new ConditionExpression("componenttype", ConditionOperator.Equal, 1) // 1 = Entity
            }
                },
                LinkEntities =
        {
            new LinkEntity
            {
                LinkFromEntityName = "solutioncomponent",
                LinkFromAttributeName = "objectid",
                LinkToEntityName = "entity",
                LinkToAttributeName = "entityid",
                Columns = new ColumnSet("logicalname", "addresstablename", "basetablename", "collectionname", "componentstate", "entitysetname", "extensiontablename", "externalname", "isactivity", "logicalcollectionname", "objecttypecode"),
                EntityAlias = "entity"
            }
        }
            };

            var results = _service.RetrieveMultiple(query);

            foreach (var entity in results.Entities)
            {
                Console.WriteLine($"Component ID: {entity.Id}");
                Console.WriteLine($"Component Type: {entity.GetAttributeValue<OptionSetValue>("componenttype")?.Value}");
                Console.WriteLine($"Entity Logical Name: {entity.GetAttributeValue<AliasedValue>("entity.logicalname")?.Value}");
                // Add more fields as needed

                DataDictionaryEntity ddEntity = new DataDictionaryEntity();
                ddEntity.EntityId = entity.Id;
                ddEntity.LogicalName = entity.GetAttributeValue<AliasedValue>("entity.logicalname").Value.ToString();


                ddSolution.AddEntity(ddEntity);

            }
        }
    }
}