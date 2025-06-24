using DataDictionary.Models;
using DataIngestor.Services;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataIngestor.Processors
{
    /// <summary>
    /// Handles all entity-related operations including retrieving entities and their attributes
    /// </summary>
    public class EntityProcessor
    {
        private readonly IDataverseService _dataverseService;

        public EntityProcessor(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService ?? throw new ArgumentNullException(nameof(dataverseService));
        }

        /// <summary>
        /// Processes all entities within the provided solutions
        /// </summary>
        /// <param name="solutions">Dictionary of solutions to process entities for</param>
        public void ProcessEntities(Dictionary<string, DataDictionarySolution> solutions)
        {
            if (solutions == null || !solutions.Any())
                throw new ArgumentException("Solutions cannot be null or empty", nameof(solutions));

            try
            {
                foreach (var solutionPair in solutions)
                {
                    var solution = solutionPair.Value;
                    ProcessEntitiesForSolution(solution);
                }

                Console.WriteLine($"Completed processing entities for {solutions.Count} solutions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing entities: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Processes entities for a specific solution
        /// </summary>
        /// <param name="solution">The solution to process entities for</param>
        private void ProcessEntitiesForSolution(DataDictionarySolution solution)
        {
            if (solution?.Components == null)
                return;

            var entityComponents = solution.Components.Where(c => c.ComponentType == 1).ToList(); // 1 = Entity
            
            if (!entityComponents.Any())
            {
                Console.WriteLine($"No entity components found in solution: {solution.UniqueName}");
                return;
            }

            Console.WriteLine($"Processing {entityComponents.Count} entity components for solution: {solution.UniqueName}");

            foreach (var component in entityComponents)
            {
                try
                {
                    ProcessEntityComponent(solution, component);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing entity component {component.ObjectId}: {ex.Message}");
                    // Continue processing other entities
                }
            }
        }

        /// <summary>
        /// Processes a single entity component
        /// </summary>
        /// <param name="solution">The parent solution</param>
        /// <param name="component">The entity component to process</param>
        private void ProcessEntityComponent(DataDictionarySolution solution, DataDictionarySolutionComponent component)
        {
            Console.WriteLine($"Processing Entity Component: {component.ObjectId} in Solution: {solution.UniqueName}");

            var entityQuery = new QueryExpression("entity")
            {
                ColumnSet = new ColumnSet("name", "objecttypecode", "basetablename", "collectionname", 
                                        "componentstate", "entityid", "entitysetname", "isactivity", "logicalname"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("entityid", ConditionOperator.Equal, component.ObjectId)
                    }
                }
            };

            var entityResults = _dataverseService.RetrieveMultiple(entityQuery);

            foreach (var entity in entityResults.Entities)
            {
                var ddEntity = new DataDictionaryEntity
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

                solution.AddEntity(ddEntity);

                Console.WriteLine($"Entity: {ddEntity.Name}, Object Type Code: {ddEntity.ObjectTypeCode}, Entity Set Name: {ddEntity.EntitySetName}");
            }
        }

        /// <summary>
        /// Processes attributes for entities within solutions
        /// </summary>
        /// <param name="solutions">Dictionary of solutions to process attributes for</param>
        public void ProcessAttributes(Dictionary<string, DataDictionarySolution> solutions)
        {
            if (solutions == null || !solutions.Any())
                throw new ArgumentException("Solutions cannot be null or empty", nameof(solutions));

            try
            {
                foreach (var solutionPair in solutions)
                {
                    var solution = solutionPair.Value;
                    ProcessAttributesForSolution(solution);
                }

                Console.WriteLine($"Completed processing attributes for {solutions.Count} solutions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing attributes: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Processes attributes for a specific solution
        /// </summary>
        /// <param name="solution">The solution to process attributes for</param>
        private void ProcessAttributesForSolution(DataDictionarySolution solution)
        {
            var attributeComponents = solution.Components.Where(c => c.ComponentType == 2).ToList(); // 2 = Attribute
            
            if (!attributeComponents.Any())
            {
                Console.WriteLine($"No attribute components found in solution: {solution.UniqueName}");
                return;
            }

            Console.WriteLine($"Processing {attributeComponents.Count} attribute components for solution: {solution.UniqueName}");

            foreach (var component in attributeComponents)
            {
                try
                {
                    ProcessAttributeComponent(solution, component);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing attribute component {component.ObjectId}: {ex.Message}");
                    // Continue processing other attributes
                }
            }
        }

        /// <summary>
        /// Processes a single attribute component
        /// </summary>
        /// <param name="solution">The parent solution</param>
        /// <param name="component">The attribute component to process</param>
        private void ProcessAttributeComponent(DataDictionarySolution solution, DataDictionarySolutionComponent component)
        {
            Console.WriteLine($"Processing Attribute Component: {component.ObjectId} in Solution: {solution.UniqueName}");

            var attributeQuery = new QueryExpression("attribute")
            {
                ColumnSet = new ColumnSet("attributeid", "attributeof", "attributetypeid", "componentstate", 
                                        "externalname", "logicalname"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("attributeid", ConditionOperator.Equal, component.ObjectId)
                    }
                }
            };

            var attributeResults = _dataverseService.RetrieveMultiple(attributeQuery);

            foreach (var attribute in attributeResults.Entities)
            {
                var ddAttribute = new DataDictionaryAttribute
                {
                    AttributeId = attribute.GetAttributeValue<Guid>("attributeid"),
                    AttributeOf = attribute.GetAttributeValue<Guid>("attributeof"),
                    AttributeTypeId = attribute.GetAttributeValue<int>("attributetypeid"),
                    ComponentState = attribute.GetAttributeValue<OptionSetValue>("componentstate")?.Value ?? 0,
                    ExternalName = attribute.GetAttributeValue<string>("externalname"),
                    LogicalName = attribute.GetAttributeValue<string>("logicalname")
                };

                solution.AddAttribute(solution, ddAttribute);

                Console.WriteLine($"Attribute: {ddAttribute.LogicalName}, Type: {ddAttribute.AttributeTypeId}, Entity: {ddAttribute.AttributeOf}");
            }
        }
    }
}