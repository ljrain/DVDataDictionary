using DataIngestor.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataIngestor.Tests
{
    /// <summary>
    /// Mock implementation of IDataverseService for testing purposes
    /// </summary>
    public class MockDataverseService : IDataverseService
    {
        private readonly Dictionary<string, List<Entity>> _mockData;
        private readonly List<OrganizationRequest> _executedRequests;

        public MockDataverseService()
        {
            _mockData = new Dictionary<string, List<Entity>>();
            _executedRequests = new List<OrganizationRequest>();
            InitializeMockData();
        }

        private void InitializeMockData()
        {
            // Initialize mock solutions
            _mockData["solution"] = new List<Entity>
            {
                CreateMockSolution("sample_solution", "Sample Solution", Guid.NewGuid()),
                CreateMockSolution("test_solution", "Test Solution", Guid.NewGuid())
            };

            // Initialize mock entities
            _mockData["entity"] = new List<Entity>
            {
                CreateMockEntity("account", "Account", Guid.NewGuid()),
                CreateMockEntity("contact", "Contact", Guid.NewGuid())
            };

            // Initialize mock components
            _mockData["solutioncomponent"] = new List<Entity>
            {
                CreateMockComponent(1, Guid.NewGuid(), true), // Entity component
                CreateMockComponent(2, Guid.NewGuid(), false), // Attribute component
                CreateMockComponent(61, Guid.NewGuid(), false) // Web resource component
            };

            // Initialize mock web resources
            _mockData["webresource"] = new List<Entity>
            {
                CreateMockWebResource("test_script.js", "Test Script", CreateSampleJavaScript())
            };

            // Initialize mock attributes
            _mockData["attribute"] = new List<Entity>
            {
                CreateMockAttribute("firstname", Guid.NewGuid(), Guid.NewGuid()),
                CreateMockAttribute("lastname", Guid.NewGuid(), Guid.NewGuid())
            };
        }

        private Entity CreateMockSolution(string uniqueName, string friendlyName, Guid solutionId)
        {
            var solution = new Entity("solution", solutionId);
            solution["uniquename"] = uniqueName;
            solution["friendlyname"] = friendlyName;
            solution["solutionid"] = solutionId;
            return solution;
        }

        private Entity CreateMockEntity(string logicalName, string name, Guid entityId)
        {
            var entity = new Entity("entity", entityId);
            entity["logicalname"] = logicalName;
            entity["name"] = name;
            entity["entityid"] = entityId;
            entity["objecttypecode"] = logicalName == "account" ? 1 : 2;
            entity["entitysetname"] = logicalName + "s";
            entity["basetablename"] = logicalName + "base";
            entity["collectionname"] = logicalName + "set";
            entity["isactivity"] = false;
            entity["componentstate"] = new OptionSetValue(0);
            return entity;
        }

        private Entity CreateMockComponent(int componentType, Guid objectId, bool isMetadata)
        {
            var component = new Entity("solutioncomponent", Guid.NewGuid());
            component["componenttype"] = componentType;
            component["objectid"] = objectId;
            component["ismetadata"] = isMetadata;
            component["rootsolutioncomponentid"] = Guid.NewGuid();
            return component;
        }

        private Entity CreateMockWebResource(string name, string displayName, string content)
        {
            var webResource = new Entity("webresource", Guid.NewGuid());
            webResource["name"] = name;
            webResource["displayname"] = displayName;
            webResource["content"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));
            webResource["dependencyxml"] = "<Dependencies><Dependency componentType=\"Entity\"><Attribute entityName=\"account\" attributeName=\"name\" /></Dependency></Dependencies>";
            webResource["webresourcetype"] = 3; // JavaScript
            webResource["webresourceid"] = webResource.Id;
            return webResource;
        }

        private Entity CreateMockAttribute(string logicalName, Guid attributeId, Guid entityId)
        {
            var attribute = new Entity("attribute", attributeId);
            attribute["logicalname"] = logicalName;
            attribute["attributeid"] = attributeId;
            attribute["attributeof"] = entityId;
            attribute["attributetypeid"] = 1;
            attribute["componentstate"] = new OptionSetValue(0);
            attribute["externalname"] = logicalName + "_external";
            return attribute;
        }

        private string CreateSampleJavaScript()
        {
            return @"
                function onLoad(executionContext) {
                    var formContext = executionContext.getFormContext();
                    
                    // Hide firstname field
                    formContext.getControl('firstname').setVisible(false);
                    
                    // Make lastname required
                    formContext.getAttribute('lastname').setRequiredLevel('required');
                    
                    // Set default website value
                    formContext.getAttribute('websiteurl').setValue('https://example.com');
                    
                    // Make phone disabled
                    formContext.getControl('telephone1').setDisabled(true);
                    
                    // API call
                    Xrm.WebApi.retrieveRecord('account', accountId, '?$select=name').then(
                        function success(result) {
                            formContext.getAttribute('accountname').setValue(result.name);
                        }
                    );
                }
            ";
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            var queryExpression = query as QueryExpression;
            if (queryExpression == null)
                return new EntityCollection();

            var entityName = queryExpression.EntityName;
            
            if (!_mockData.ContainsKey(entityName))
                return new EntityCollection();

            var entities = _mockData[entityName];
            var filteredEntities = new List<Entity>();

            // Simple filtering based on conditions
            if (queryExpression.Criteria?.Conditions?.Count > 0)
            {
                foreach (var entity in entities)
                {
                    bool matchesAllConditions = true;
                    
                    foreach (var condition in queryExpression.Criteria.Conditions)
                    {
                        if (!EntityMatchesCondition(entity, condition))
                        {
                            matchesAllConditions = false;
                            break;
                        }
                    }
                    
                    if (matchesAllConditions)
                    {
                        filteredEntities.Add(entity);
                    }
                }
            }
            else
            {
                filteredEntities = entities.ToList();
            }

            var result = new EntityCollection();
            result.Entities.AddRange(filteredEntities);
            return result;
        }

        private bool EntityMatchesCondition(Entity entity, ConditionExpression condition)
        {
            if (!entity.Contains(condition.AttributeName))
                return false;

            var attributeValue = entity[condition.AttributeName];
            
            switch (condition.Operator)
            {
                case ConditionOperator.Equal:
                    return attributeValue?.Equals(condition.Values.FirstOrDefault()) == true;
                    
                case ConditionOperator.In:
                    return condition.Values.Contains(attributeValue);
                    
                default:
                    return true; // For simplicity, assume other operators match
            }
        }

        public void ExecuteBatch(List<OrganizationRequest> requests)
        {
            if (requests == null)
                return;

            _executedRequests.AddRange(requests);
            Console.WriteLine($"Mock: Executed batch with {requests.Count} requests");
        }

        public Guid Create(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var id = entity.Id != Guid.Empty ? entity.Id : Guid.NewGuid();
            Console.WriteLine($"Mock: Created entity {entity.LogicalName} with ID {id}");
            return id;
        }

        public void Update(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Console.WriteLine($"Mock: Updated entity {entity.LogicalName} with ID {entity.Id}");
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            if (!_mockData.ContainsKey(entityName))
                return null;

            var entity = _mockData[entityName].FirstOrDefault(e => e.Id == id);
            return entity;
        }

        // Test helper methods
        public List<OrganizationRequest> GetExecutedRequests()
        {
            return _executedRequests.ToList();
        }

        public void ClearExecutedRequests()
        {
            _executedRequests.Clear();
        }

        public void AddMockEntity(string entityName, Entity entity)
        {
            if (!_mockData.ContainsKey(entityName))
            {
                _mockData[entityName] = new List<Entity>();
            }
            
            _mockData[entityName].Add(entity);
        }
    }
}