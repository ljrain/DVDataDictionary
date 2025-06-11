using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using DataDictionary.Models;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Configuration;

namespace DataDictionary
{

    /// <summary>
    /// this class is intended to extract data dictionary information from Dataverse solutions.
    /// 1) Get Solution IDs from unique solution names
    /// 2) Get all components in the solution
    /// 3) Get all attributes in the solution
    /// 4) Get all forms in the solution
    /// 5) Get all web resources in the solution
    /// </summary>
    public class DataDictionaryExtractor
    {

        private DataDictionaryExtractor(IOrganizationService service, ITracingService tracingService)
        {
            // Constructor logic can be added here if needed
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
        }

        public DataDictionaryExtractor GetInstance(IOrganizationService service, ITracingService tracingService)
        {
            // This method can be used to return an instance of the DataDictionaryExtractor
            return new DataDictionaryExtractor(service, tracingService);
        }

        private IOrganizationService _service;
        private ITracingService _tracingService;
        private Dictionary<string,DataDictionarySolution> _solutions = new Dictionary<string, DataDictionarySolution>();

        #region "data extraction methods"

        /// <summary>
        /// Take in the unique solution names and build a dictionary of DataDictionarySolution objects.
        /// </summary>
        /// <param name="solutionNames"></param>
        public void GetSolutions(string[] solutionNames)
        {
            _tracingService?.Trace("Starting data extraction for solutions: {0}", string.Join(", ", solutionNames));

            foreach (var solutionName in solutionNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                _tracingService?.Trace("Processing solution: {0}", solutionName);

                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid","uniquename","friendlyname"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, solutionName)
                    }
                    }
                };

                var solutions = _service.RetrieveMultiple(query);
                foreach (var solution in solutions.Entities)
                {
                    DataDictionarySolution ddSolution = new DataDictionarySolution
                    {
                        Name = solution.GetAttributeValue<string>("uniquename"),
                        SolutionId = solution.GetAttributeValue<Guid>("solutionid").ToString()
                    };
                    _solutions.Add(solutionName, ddSolution);
                }

            }
        }

        /// <summary>
        /// get all solution components for a given solution ID.
        /// </summary>
        /// <param name="solutionId"></param>
        public void GetComponentsInSolution(string solutionId)
        {
            // Query solutioncomponent for forms in the solution
            var componentQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype", "componenttypename", "ismetadata", "ismetadataname", "rootcomponentbehavior", "rootcomponentbehaviorname", "rootsolutioncomponentid", "solutionid"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("solutionid", ConditionOperator.Equal, new Guid(solutionId)),
            }
                }
            };

            var components = _service.RetrieveMultiple(componentQuery);
            foreach (var component in components.Entities)
            {
                var solutionName = _solutions.FirstOrDefault(s => s.Value.SolutionId == solutionId).Key;
                if (_solutions.TryGetValue(solutionName, out DataDictionarySolution ddSolution))
                {
                    DataDictionarySolutionComponent ddComponent = new DataDictionarySolutionComponent
                    {
                        ComponentType = component.GetAttributeValue<string>("componenttype"),
                        ComponentTypeName = component.GetAttributeValue<string>("componenttypename"),
                        IsMetadata = component.GetAttributeValue<bool>("ismetadata"),
                        IsMetadataName = component.GetAttributeValue<string>("ismetadataname"),
                        RootComponentBehavior = component.GetAttributeValue<Guid>("rootcomponentbehavior"),
                        RootComponentBehaviorName = component.GetAttributeValue<string>("rootcomponentbehaviorname"),
                        RootSolutionComponentId = component.GetAttributeValue<Guid>("rootsolutioncomponentid")
                    };
                    ddSolution.AddComponent(ddComponent);
                }
            }
        }

        /// <summary>
        /// get all entities in the solution based on the components returned
        /// </summary>
        /// <param name="solutionId"></param>
        public void GetEntitiesInSolution(string solutionId)
        {
            #region "get all entities in the solution based on the components returned"
            foreach (var solution in _solutions.Values.Where(s => s.SolutionId == solutionId))
            {
                _tracingService?.Trace("Processing solution: {0}", solution.Name);
                var entityQuery = new QueryExpression("entity")
                {
                    ColumnSet = new ColumnSet("entityid", "entitysetname", "basetablename", "collectionname", "componentstate", "componentstatename", "extensiontablename", "externalcollectionname", "externalname", "isactivity", "isactivityname", "logicalcollectionname", "logicalname", "objecttypecode", "originallocalizedcollectionname", "originallocalizedname", "overwritetime", "parentcontrollingattributename", "physicalname", "reportviewname"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("solutionid", ConditionOperator.Equal, new Guid(solutionId))
                        }
                    }
                };
                var entities = _service.RetrieveMultiple(entityQuery);
                foreach (var entity in entities.Entities)
                {
                    _tracingService?.Trace("Found entity: {0} ({1})", entity.GetAttributeValue<string>("displayname"), entity.GetAttributeValue<string>("logicalname"));

                    DataDictionaryEntity ddEntity = new DataDictionaryEntity();
                    ddEntity.EntityId = entity.GetAttributeValue<Guid>("entityid");
                    ddEntity.EntitySetName = entity.GetAttributeValue<string>("entitysetname");
                    ddEntity.BaseTableName = entity.GetAttributeValue<string>("basetablename");
                    ddEntity.CollectionName = entity.GetAttributeValue<string>("collectionname");
                    ddEntity.ComponentState = entity.GetAttributeValue<int>("componentstate");
                    ddEntity.ComponentStateName = entity.GetAttributeValue<string>("componentstatename");
                    ddEntity.ExtensionTableName = entity.GetAttributeValue<string>("extensiontablename");
                    ddEntity.ExternalCollectionName = entity.GetAttributeValue<string>("externalcollectionname");
                    ddEntity.ExternalName = entity.GetAttributeValue<string>("externalname");
                    ddEntity.IsActivity = entity.GetAttributeValue<bool>("isactivity");
                    ddEntity.IsActivityName = entity.GetAttributeValue<string>("isactivityname");
                    ddEntity.LogicalCollectionName = entity.GetAttributeValue<string>("logicalcollectionname");
                    ddEntity.LogicalName = entity.GetAttributeValue<string>("logicalname");

                }
            }
            #endregion

            // check for components that have a componenttype of 1  // which is Entity
            // check for components that also have a behavior of 1, which is RootComponentBehavior






        }
        public void GetAttributesInSolution(string solutionId)
        {
        }
        public void GetFormsInSolution(string solutionId)
        {
        }



        #endregion
    }
}