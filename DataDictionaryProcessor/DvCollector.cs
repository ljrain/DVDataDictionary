using DataDictionary.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataDictionaryProcessor
{
    /// <summary>
    /// Collects and processes Dataverse solution metadata, including entities, attributes, and web resources.
    /// </summary>
    /// <remarks>
    /// This class orchestrates the retrieval of solution components, entity and attribute metadata, and JavaScript field modifications
    /// from Microsoft Dataverse using the provided <see cref="CrmServiceClient"/>. It builds a data dictionary model for further analysis or export.
    /// </remarks>
    public class DvCollector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DvCollector"/> class.
        /// </summary>
        /// <param name="serviceClient">The CRM service client used for Dataverse operations.</param>
        /// <param name="solutionNames">An array of solution unique names to process.</param>
        public DvCollector(CrmServiceClient serviceClient, string[] solutionNames)
        {
            _serviceClient = serviceClient;
            _solutionNames = solutionNames;
        }

        #region "Private Fields"

        private CrmServiceClient _serviceClient;
        private Dictionary<string, DataDictionarySolution> _ddSolutions = new Dictionary<string, DataDictionarySolution>();
        private List<string> _allowedLogicalNames = new List<string>();
        private Dictionary<string, List<string>> _allowedtableAttributes = new Dictionary<string, List<string>>();
        private DvJavaScriptParser _dvJavaScriptParser;
        List<DataDictionaryJavaScriptFieldModification> modifications;
        private string[] _solutionObjectIds;
        private string[] _solutionNames;

        #endregion

        #region "Public Properties"

        /// <summary>
        /// Gets the dictionary of collected solutions, keyed by unique name.
        /// </summary>
        public Dictionary<string, DataDictionarySolution> DDSolutions
        {
            get { return _ddSolutions; }
        }

        /// <summary>
        /// Gets the list of JavaScript field modifications found during processing.
        /// </summary>
        public List<DataDictionaryJavaScriptFieldModification> Modifications
        {
            get { return modifications; }
        }

        /// <summary>
        /// Gets or sets the list of allowed entity logical names for filtering.
        /// </summary>
        public List<string> AllowedLogicalNames
        {
            get { return _allowedLogicalNames; }
            set { _allowedLogicalNames = value; }
        }

        /// <summary>
        /// Gets or sets the allowed table attributes for filtering.
        /// </summary>
        public Dictionary<string, List<string>> AllowedTableAttributes
        {
            get { return _allowedtableAttributes; }
            set { _allowedtableAttributes = value; }
        }

        /// <summary>
        /// Gets or sets the array of web resource object IDs collected from solutions.
        /// </summary>
        public string[] SolutionObjectIds
        {
            get { return (_solutionObjectIds); }
            set { _solutionObjectIds = value; }
        }

        #endregion

        /// <summary>
        /// Collects all relevant metadata from Dataverse, including solutions, components, entities, attributes, and web resources.
        /// </summary>
        public void CollectData()
        {
            /*
             * Collect all metadata and store in models
             * 1) Solutions
             * 2) Solution Components
             * 2.1) Entities
             * 2.2) Attributes
             * 2.3) Web Resources
             */
            // Solution names are pulled from appsettings.json under "Solutions" key
            DictionaryOrchestrator.LogEvent("Collecting solutions from " + _serviceClient.ConnectedOrgFriendlyName);
            GetSolutions(_solutionNames);
            DictionaryOrchestrator.LogEvent("Solutions collected: " + _ddSolutions.Count);

            // Iterate through the dictionary and pass each DataDictionarySolution object to GetComponentsInSolution
            foreach (var ddSolution in _ddSolutions.Values)
            {
                GetComponentsInSolution(ddSolution);
                List<Guid> attributeIds = GetAttributeComponentObjectIds(ddSolution);
                GetAttributesBySolutionObjectIds(attributeIds.ToArray());

                DictionaryOrchestrator.LogEvent($"Solution: {ddSolution.UniqueName}, Components Count: {ddSolution.Components.Count}");

                //TODO: Investigating below
                //QuerySolutionComponentAttributes(new Guid(ddSolution.SolutionId));
            }
            _solutionObjectIds = GetWebResourceObjectIds();

            DictionaryOrchestrator.LogEvent("Web Resource Object IDs collected: " + _solutionObjectIds.Length);
            List<Entity> webResources = GetWebResourcesByObjectIds(_solutionObjectIds);
            foreach (var webResource in webResources)
            {
                _ddSolutions[_ddSolutions.FirstOrDefault().Key].WebResources.Add(new DataDictionaryWebResource
                {
                    WebResourceId = webResource.GetAttributeValue<Guid>("webresourceid"),
                    Content = webResource.GetAttributeValue<string>("content"),
                    DisplayName = webResource.GetAttributeValue<string>("name"),
                });
            }

            DictionaryOrchestrator.LogEvent("Web Resources retrieved: " + webResources.Count);

            _dvJavaScriptParser = new DvJavaScriptParser(_serviceClient, _ddSolutions, _allowedLogicalNames, _allowedtableAttributes);
            // parse javascript that was found in web resources
            foreach (var webResource in webResources)
            {
                modifications = _dvJavaScriptParser.ParseFieldModifications(webResource.GetAttributeValue<string>("content"),
                    webResource.GetAttributeValue<Guid>("webresourceid"),
                    webResource.GetAttributeValue<string>("name"));
            }

            ProcessEntities();
            DictionaryOrchestrator.LogEvent("Entities processed: " + _ddSolutions.Values.Sum(s => s.Entities.Count));
            DictionaryOrchestrator.LogEvent("Allowed Logical Names Count: : " + _allowedLogicalNames.Count);
            LogSchema();
            DictionaryOrchestrator.LogEvent("Schema logged for Default solution with " + _ddSolutions["Default"].AttributeMetadata.Count + " attributes.");

            DictionaryOrchestrator.LogEvent("Data collection completed.");
        }

        #region Private Methods

        /// <summary>
        /// Takes in a string array of solution unique names and retrieves the solutions from CRM.
        /// A dictionary of DataDictionarySolution objects is built, keyed by the unique name of the solution.
        /// </summary>
        /// <param name="solutionNames">String array of unique names of solutions to process.</param>
        private void GetSolutions(string[] solutionNames)
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

                var solutions = _serviceClient.RetrieveMultiple(query);
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

        /// <summary>
        /// Queries solution components of type attribute for a given solution.
        /// </summary>
        /// <param name="solutionId">The solution ID to query components for.</param>
        /// <returns>An <see cref="EntityCollection"/> of solution component attributes.</returns>
        private EntityCollection QuerySolutionComponentAttributes(Guid solutionId)
        {
            // Pseudocode:
            // 1. Build a QueryExpression for "solutioncomponent" (alias: sc)
            // 2. Add filter: solutionid = solutionId AND componenttype = 2
            // 3. Add a LinkEntity to "attribute" (alias: attr) where sc.objectid = attr.attributeid
            // 4. Select columns: sc.solutioncomponentid, sc.objectid, sc.componenttype, sc.componenttypename, sc.ismetadataname, attr.attributeid, attr.attributeof, attr.logicalname
            // 5. Execute and return results

            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("solutioncomponentid", "objectid", "componenttype"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 2)
                    }
                }
            };

            var link = query.AddLink("attribute", "objectid", "attributeid", JoinOperator.Inner);
            link.Columns = new ColumnSet("attributeid", "attributeof", "logicalname", "tablecolumnname");
            link.EntityAlias = "attr";

            var results = _serviceClient.RetrieveMultiple(query);

            foreach (var atr in results.Entities)
            {
                if (_allowedtableAttributes.ContainsKey(atr["attr.tablecolumnname"].ToString()))
                {
                    DictionaryOrchestrator.LogEvent("Table found");
                }
            }

            return results;
        }

        /// <summary>
        /// Retrieves and adds all components for a given solution.
        /// </summary>
        /// <param name="ddSolution">The solution to retrieve components for.</param>
        private void GetComponentsInSolution(DataDictionarySolution ddSolution)
        {
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

            var components = _serviceClient.RetrieveMultiple(componentQuery);
            foreach (var component in components.Entities)
            {
                DataDictionarySolutionComponent ddComponent = new DataDictionarySolutionComponent
                {
                    ObjectId = component.GetAttributeValue<Guid>("objectid"),
                    ComponentType = component.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0,
                    IsMetadata = component.GetAttributeValue<bool>("ismetadata"),
                    RootSolutionComponentId = component.GetAttributeValue<Guid>("rootsolutioncomponentid")
                };
                ddSolution.AddComponent(ddComponent);
            }
        }

        /// <summary>
        /// Gets the object IDs of web resource components from all collected solutions.
        /// </summary>
        /// <returns>Array of web resource object ID strings.</returns>
        private string[] GetWebResourceObjectIds()
        {
            List<string> objectIds = new List<string>();
            foreach (var ddSolution in _ddSolutions.Values)
            {
                foreach (var ddComponent in ddSolution.Components)
                {
                    if (ddComponent.ComponentType == 61) // Assuming 61 is the type for Web Resource
                    {
                        objectIds.Add(ddComponent.ObjectId.ToString());
                    }
                }
            }
            return objectIds.ToArray();
        }

        /// <summary>
        /// Retrieves all web resources matching the given object IDs.
        /// </summary>
        /// <param name="objectIds">Array of webresourceid strings (GUIDs).</param>
        /// <returns>List of Entity objects representing web resources.</returns>
        private List<Entity> GetWebResourcesByObjectIds(string[] objectIds)
        {
            if (objectIds == null || objectIds.Length == 0)
                return new List<Entity>();

            var query = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet(
                    "webresourceid",
                    "webresourceidunique",
                    "webresourcetype",
                    "dependencyxml",
                    "description",
                    "displayname",
                    "name",
                    "content",
                    "createdon",
                    "modifiedon"
                ),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And
                }
            };

            var guidList = objectIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(id => new Guid(id))
                .ToList();

            if (guidList.Count == 0)
                return new List<Entity>();

            query.Criteria.AddCondition(new ConditionExpression("webresourceid", ConditionOperator.In, guidList.Cast<object>().ToArray()));

            var results = _serviceClient.RetrieveMultiple(query);

            return results.Entities.ToList();
        }

        /// <summary>
        /// Processes entity components in all collected solutions and adds their metadata.
        /// </summary>
        private void ProcessEntities()
        {
            foreach (DataDictionarySolution ddSolution in _ddSolutions.Values)
            {
                foreach (DataDictionarySolutionComponent ddComponent in ddSolution.Components)
                {
                    if (ddComponent.ComponentType == 1) // Assuming 1 is the type for Entity
                    {
                        DictionaryOrchestrator.LogEvent($"Processing Entity Component: {ddComponent.ObjectId} in Solution: {ddSolution.UniqueName}");

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

                        EntityCollection results = _serviceClient.RetrieveMultiple(entityQuery);
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
                            _allowedLogicalNames.Add(ddEntity.LogicalName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves attribute metadata for the given attribute object IDs.
        /// </summary>
        /// <param name="AttributeObjectIds">Array of attribute object IDs (GUIDs).</param>
        private void GetAttributesBySolutionObjectIds(Guid[] AttributeObjectIds)
        {
            if (AttributeObjectIds == null || AttributeObjectIds.Length == 0)
                return;

            var retrieveMetadataRequest = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Properties = new MetadataPropertiesExpression
                    {
                        AllProperties = false
                    }
                }
            };

            retrieveMetadataRequest.Query.Properties.PropertyNames.AddRange(new[] { "LogicalName", "DisplayName", "MetadataId" });

            retrieveMetadataRequest.Query.Criteria = new MetadataFilterExpression
            {
                FilterOperator = LogicalOperator.And
            };

            foreach (var id in AttributeObjectIds)
            {
                retrieveMetadataRequest.Query.Criteria.Conditions.Add(new MetadataConditionExpression
                {
                    PropertyName = "MetadataId",
                    ConditionOperator = MetadataConditionOperator.Equals,
                    Value = id
                });
            }

            var response = (RetrieveMetadataChangesResponse)_serviceClient.Execute(retrieveMetadataRequest);

            foreach (var attributeMetadata in response.EntityMetadata.SelectMany(e => e.Attributes))
            {
                //DictionaryOrchestrator.LogEvent($"Logical Name: {attributeMetadata.LogicalName}, Display Name: {attributeMetadata.DisplayName?.UserLocalizedLabel?.Label}");
            }
        }

        /// <summary>
        /// Logs the schema (entity and attribute metadata) for all allowed logical names into the Default solution.
        /// </summary>
        private void LogSchema()
        {
            RetrieveAllEntitiesRequest request = null;
            RetrieveAllEntitiesResponse response = null;
            PicklistAttributeMetadata PicklistAM = null;
            OptionMetadata om = null;
            string FormulaDefinition = null;

            // create Default solution to hold metadata
            if (!_ddSolutions.ContainsKey("Default"))
            {
                _ddSolutions.Add("Default", new DataDictionarySolution
                {
                    FriendlyName = "Default Solution",
                    UniqueName = "Default"
                });
            }

            try
            {
                Console.Write("Retrieving Attribute Metadata .");
                request = new RetrieveAllEntitiesRequest()
                {
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = false,
                };
                response = (RetrieveAllEntitiesResponse)_serviceClient.Execute(request);

                IEnumerable<EntityMetadata> results = response.EntityMetadata
                    .Where(e => e.IsCustomizable != null
                        && _allowedLogicalNames.Contains(e.LogicalName))
                    .OrderBy(e => e.LogicalName)
                    .ToList();

                if (results != null)
                {
                    foreach (EntityMetadata entity in results)
                    {
                        foreach (AttributeMetadata attribute in entity.Attributes)
                        {
                            DataDictionaryAttributeMetadata ddMeta = new DataDictionaryAttributeMetadata();
                            ddMeta.Table = entity.LogicalName;
                            ddMeta.ColumnDisplay = (attribute.DisplayName.UserLocalizedLabel == null ? String.Empty : attribute.DisplayName.UserLocalizedLabel.Label);
                            ddMeta.ColumnLogical = attribute.LogicalName;
                            ddMeta.ColumnSchema = attribute.SchemaName;
                            ddMeta.DataType = attribute.AttributeType.Value.ToString();
                            ddMeta.Description = attribute.Description?.UserLocalizedLabel?.Label ?? string.Empty;
                            ddMeta.IsCustom = attribute.IsCustomAttribute ?? false;
                            ddMeta.AuditEnabled = attribute.IsAuditEnabled.Value;
                            ddMeta.LangCode = attribute.DisplayName.UserLocalizedLabel?.LanguageCode ?? 0;
                            ddMeta.ModifiedOn = attribute.ModifiedOn ?? DateTime.MinValue;
                            ddMeta.AttributeOf = entity.MetadataId.ToString();
                            ddMeta.AttributeType = attribute.AttributeType.Value.ToString();
                            ddMeta.CanBeSecuredForCreate = attribute.CanBeSecuredForCreate;
                            ddMeta.CanBeSecuredForRead = attribute.CanBeSecuredForRead;
                            ddMeta.CanBeSecuredForUpdate = attribute.CanBeSecuredForUpdate;
                            ddMeta.ColumnNumber = attribute.ColumnNumber;
                            ddMeta.CreatedOn = attribute.CreatedOn ?? DateTime.MinValue;
                            ddMeta.DeprecatedVersion = attribute.DeprecatedVersion ?? string.Empty;
                            ddMeta.DisplayName = attribute.DisplayName.UserLocalizedLabel?.Label ?? string.Empty;
                            ddMeta.EntityLogicalName = entity.LogicalName;
                            ddMeta.ExtensionData = attribute.ExtensionData?.ToString() ?? string.Empty;
                            ddMeta.ExternalName = attribute.ExternalName ?? string.Empty;
                            ddMeta.HasChanged = attribute.HasChanged ?? false;
                            ddMeta.InheritsFrom = attribute.InheritsFrom?.ToString() ?? string.Empty;
                            ddMeta.IntroducedVersion = attribute.IntroducedVersion ?? string.Empty;
                            ddMeta.IsCustomAttribute = attribute.IsCustomAttribute ?? false;
                            ddMeta.IsCustomizable = attribute.IsCustomizable?.Value ?? false;

                            switch (attribute.AttributeType)
                            {
                                case AttributeTypeCode.BigInt:
                                    ddMeta.MinValue = (Int64?)((BigIntAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((BigIntAttributeMetadata)attribute).MaxValue;
                                    break;
                                case AttributeTypeCode.DateTime:
                                    ddMeta.FormulaDefinition = ((DateTimeAttributeMetadata)attribute).FormulaDefinition;
                                    FormulaDefinition = ((DateTimeAttributeMetadata)attribute).FormulaDefinition;
                                    break;
                                case AttributeTypeCode.Decimal:
                                    ddMeta.DataType = "Decimal";
                                    FormulaDefinition = ((DecimalAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((DecimalAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((DecimalAttributeMetadata)attribute).MaxValue;
                                    ddMeta.Precision = ((DecimalAttributeMetadata)attribute).Precision;
                                    break;
                                case AttributeTypeCode.Double:
                                    ddMeta.DataType = "Double";
                                    ddMeta.MinValue = (Int64?)((DoubleAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((DoubleAttributeMetadata)attribute).MaxValue;
                                    break;
                                case AttributeTypeCode.Integer:
                                    FormulaDefinition = ((IntegerAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((IntegerAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((IntegerAttributeMetadata)attribute).MaxValue;
                                    break;
                                case AttributeTypeCode.Lookup:
                                    ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).Targets != null
                                        ? string.Join(",", ((LookupAttributeMetadata)attribute).Targets)
                                        : null;
                                    break;
                                case AttributeTypeCode.Memo:
                                    ddMeta.DataType = "Memo";
                                    ddMeta.MaxLength = ((MemoAttributeMetadata)attribute).MaxLength;
                                    break;
                                case AttributeTypeCode.Money:
                                    ddMeta.DataType = "Money";
                                    FormulaDefinition = ((MoneyAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((MoneyAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((MoneyAttributeMetadata)attribute).MaxValue;
                                    break;
                                case AttributeTypeCode.Picklist:
                                    ddMeta.DataType = "Picklist";
                                    FormulaDefinition = ((PicklistAttributeMetadata)attribute).FormulaDefinition;
                                    PicklistAM = (PicklistAttributeMetadata)attribute;
                                    switch (PicklistAM.OptionSet.Options.Count)
                                    {
                                        case 0:
                                            break;
                                        case 1:
                                            om = PicklistAM.OptionSet.Options[0];
                                            break;
                                        default:
                                            om = PicklistAM.OptionSet.Options[0];
                                            for (int j = 1; j < PicklistAM.OptionSet.Options.Count; j++)
                                            {
                                                // Option handling
                                            }
                                            break;
                                    }
                                    break;
                                case AttributeTypeCode.String:
                                    ddMeta.DataType = "String";
                                    FormulaDefinition = ((StringAttributeMetadata)attribute).FormulaDefinition;
                                    break;
                                default:
                                    break;
                            }

                            _ddSolutions["Default"].AttributeMetadata.Add(ddMeta);
                        }
                    }
                }
                else
                {
                    Console.Write("Failed to retrieve entity metadata");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                om = null;
                PicklistAM = null;
                response = null;
                request = null;
            }
        }

        /// <summary>
        /// Returns the ObjectIds of components of type 2 (Attribute) for the given DataDictionarySolution.
        /// </summary>
        /// <param name="ddSolution">The solution to filter components from.</param>
        /// <returns>List of ObjectIds (Guid) for components of type 2.</returns>
        private List<Guid> GetAttributeComponentObjectIds(DataDictionarySolution ddSolution)
        {
            // ComponentType == 2 is for Attribute components
            return ddSolution.Components
                .Where(c => c.ComponentType == 2)
                .Select(c => c.ObjectId)
                .ToList();
        }

        #endregion
    }
}
