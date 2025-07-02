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
    public class DvCollector
    {

        public DvCollector(CrmServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }

        #region "Private Fields"

        private CrmServiceClient _serviceClient;
        private Dictionary<string, DataDictionarySolution> _ddSolutions = new Dictionary<string, DataDictionarySolution>();
        private List<string> _allowedLogicalNames = new List<string>();
        private Dictionary<string, List<string>> _allowedtableAttributes = new Dictionary<string, List<string>>();
        private DvJavaScriptParser _dvJavaScriptParser;
        List<DataDictionaryJavaScriptFieldModification> modifications;
        private string[] _solutionObjectIds;


        #endregion


        #region "Public Properties"

        public Dictionary<string, DataDictionarySolution> DDSolutions
        {
            get { return _ddSolutions; }
        }

        public List<DataDictionaryJavaScriptFieldModification> Modifications
        {
            get { return modifications; }
        }

        public List<string> AllowedLogicalNames
        {
            get { return _allowedLogicalNames; }
            set { _allowedLogicalNames = value; }
        }

        public Dictionary<string, List<string>> AllowedTableAttributes
        {
            get { return _allowedtableAttributes; }
            set { _allowedtableAttributes = value; }
        }

        public string[] SolutionObjectIds
        {
            get { return (_solutionObjectIds); }
            set { _solutionObjectIds = value; }
        }

        #endregion



        public void CollectData()
        {
            /*
             * Collect all metadata and store in models
             * 1) Solutions
             * 2) Solution Components
             * 3) Attribute Metadata
             */
            // below will be a parameter or setting that accepts an array of solution unique names
            GetSolutions(new string[] { "partnertracker" });
            Console.WriteLine("Solutions collected: " + _ddSolutions.Count);

            // Iterate through the dictionary and pass each DataDictionarySolution object to GetComponentsInSolution
            foreach (var ddSolution in _ddSolutions.Values)
            {
                GetComponentsInSolution(ddSolution);
                List<Guid> attributeIds = GetAttributeComponentObjectIds(ddSolution);
                GetAttributesBySolutionObjectIds(attributeIds.ToArray());

                Console.WriteLine($"Solution: {ddSolution.UniqueName}, Components Count: {ddSolution.Components.Count}");
                //TODO: Investigating below
                //QuerySolutionComponentAttributes(new Guid(ddSolution.SolutionId));

            }
            _solutionObjectIds = GetWebResourceObjectIds();


            Console.WriteLine("Web Resource Object IDs collected: " + _solutionObjectIds.Length);
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

            Console.WriteLine("Web Resources retrieved: " + webResources.Count);

            _dvJavaScriptParser = new DvJavaScriptParser(_serviceClient, _ddSolutions, _allowedLogicalNames, _allowedtableAttributes);
            // parse javascript that was found in web resources
            foreach (var webResource in webResources)
            {
                modifications = _dvJavaScriptParser.ParseFieldModifications(webResource.GetAttributeValue<string>("content"),
                    webResource.GetAttributeValue<Guid>("webresourceid"),
                    webResource.GetAttributeValue<string>("name"));
            }

            ProcessEntities();
            Console.WriteLine("Entities processed: " + _ddSolutions.Values.Sum(s => s.Entities.Count));
            Console.WriteLine("Allowed Logical Names Count: : " + _allowedLogicalNames.Count);
            LogSchema();
            Console.WriteLine("Schema logged for Default solution with " + _ddSolutions["Default"].AttributeMetadata.Count + " attributes.");

            Console.WriteLine("Data collection completed.");
        }

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
                    Console.WriteLine("Table found");
                }

            }

            return results;
        }



        private void GetComponentsInSolution(DataDictionarySolution ddSolution)
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

                Console.WriteLine($"Component Type: {ddComponent.ComponentType}, Is Metadata: {ddComponent.IsMetadata}, Object Id: {ddComponent.ObjectId}");
            }
        }

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

            //_tracingService?.Trace("Retrieving web resources for object IDs: {0}", string.Join(", ", objectIds));

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

            // Add the 'In' condition for the webresourceid
            var guidList = objectIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(id => new Guid(id))
                .ToList();

            if (guidList.Count == 0)
                return new List<Entity>();

            query.Criteria.AddCondition(new ConditionExpression("webresourceid", ConditionOperator.In, guidList.Cast<object>().ToArray()));

            var results = _serviceClient.RetrieveMultiple(query);

            //_tracingService?.Trace("Found {0} web resources.", results.Entities.Count);

            return results.Entities.ToList();
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
                            _allowedLogicalNames.Add(ddEntity.LogicalName); // Add logical name to allowed list

                            Console.WriteLine($"Entity: {ddEntity.Name}, Object Type Code: {ddEntity.ObjectTypeCode}, Entity Set Name: {ddEntity.EntitySetName}");
                        }
                    }
                }
            }
        }


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

            // Fix for CS0200: Use AddRange to populate the PropertyNames collection instead of direct assignment
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
                Console.WriteLine($"Logical Name: {attributeMetadata.LogicalName}, Display Name: {attributeMetadata.DisplayName?.UserLocalizedLabel?.Label}");
            }
        }

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
                Console.Write("Retrieving Metadata .");
                request = new RetrieveAllEntitiesRequest()
                {
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = false,
                };
                response = (RetrieveAllEntitiesResponse)_serviceClient.Execute(request);

                // Pseudocode plan:
                // 1. Accept a list of allowed logical names (e.g., List<string> allowedLogicalNames or string[] allowedLogicalNames).
                // 2. Filter the EntityMetadata results so that only those with LogicalName in the allowed list are included.
                // 3. Apply this filter in the LINQ query where results are built.

                // Example: Add a parameter to LogSchema or make allowedLogicalNames available in scope
                // For demonstration, assume a variable allowedLogicalNames is available (e.g., string[] allowedLogicalNames).

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
                            //ddMeta.IsCalculated = attribute.IsCalculated.Value ?? false;
                            ddMeta.LangCode = attribute.DisplayName.UserLocalizedLabel?.LanguageCode ?? 0;
                            ddMeta.ModifiedOn = attribute.ModifiedOn ?? DateTime.MinValue;
                            ddMeta.AttributeOf = entity.MetadataId.ToString(); // Assuming AttributeOf is the Entity's MetadataId
                            ddMeta.AttributeType = attribute.AttributeType.Value.ToString();
                            //ddMeta.AttributeTypeName = attribute.AttributeTypeName ?? string.Empty;
                            //ddMeta.AutoNumberFormat = (attribute as AutoNumberAttributeMetadata)?.Format ?? string.Empty;
                            ddMeta.CanBeSecuredForCreate = attribute.CanBeSecuredForCreate;
                            ddMeta.CanBeSecuredForRead = attribute.CanBeSecuredForRead;
                            ddMeta.CanBeSecuredForUpdate = attribute.CanBeSecuredForUpdate;
                            //ddMeta.CanModifiedAdditionalSettings = attribute.CanModifyAdditionalSettings;
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
                            //ddMeta.IsAuditEnabled = attribute.IsAuditEnabled ?? false;
                            ddMeta.IsCustomAttribute = attribute.IsCustomAttribute ?? false;
                            ddMeta.IsCustomizable = attribute.IsCustomizable?.Value ?? false;

                            switch (attribute.AttributeType)
                            {
                                //case AttributeTypeCode.Boolean:
                                //    break;

                                case AttributeTypeCode.BigInt:
                                    ddMeta.MinValue = (Int64?)((BigIntAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((BigIntAttributeMetadata)attribute).MaxValue;
                                    break;

                                //case AttributeTypeCode.CalendarRules:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((CalendarRulesAttributeMetadata)attribute).MinValue, ((BigIntAttributeMetadata)attribute).MaxValue);
                                //    break;

                                //case AttributeTypeCode.Customer:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((CustomerAttributeMetadata)attribute).MinValue, ((BigIntAttributeMetadata)attribute).MaxValue);
                                //    break;

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

                                //case AttributeTypeCode.EntityName:
                                //    // !! sb.AppendFormat("\t\t{0}\t{1}", ((EntityNameAttributeMetadata)attribute).IsPrimaryId, ((EntityNameAttributeMetadata)attribute).IsPrimaryName);
                                //    break;

                                case AttributeTypeCode.Integer:
                                    FormulaDefinition = ((IntegerAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((IntegerAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((IntegerAttributeMetadata)attribute).MaxValue;
                                    break;

                                case AttributeTypeCode.Lookup:
                                    ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).Targets != null
                                        ? string.Join(",", ((LookupAttributeMetadata)attribute).Targets)
                                        : null;

                                    ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).Targets != null
                                        ? string.Join(",", ((LookupAttributeMetadata)attribute).Targets)
                                        : null;

                                    break;

                                //case AttributeTypeCode.ManagedProperty:
                                //    // sb.AppendFormat("\t{0}\t\t", ((ManagedPropertyAttributeMetadata)attribute));
                                //    break;

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

                                //case AttributeTypeCode.Owner:
                                //    sb.AppendFormat("\t\t\t{0}\t{1}", ((OwnerAttributeMetadata)attribute).MinValue);
                                //    break;

                                //case AttributeTypeCode.PartyList:
                                //    sb.AppendFormat("\t\t{0}\t\t", ((PartyListAttributeMetadata)attribute).OptionSet.OptionSetType.ToString());
                                //    break;

                                case AttributeTypeCode.Picklist:
                                    ddMeta.DataType = "Picklist";
                                    FormulaDefinition = ((PicklistAttributeMetadata)attribute).FormulaDefinition;
                                    PicklistAM = (PicklistAttributeMetadata)attribute;

                                    switch (PicklistAM.OptionSet.Options.Count)
                                    {
                                        case 0:
                                            //sb.Append("\t\t\t");
                                            break;
                                        case 1:
                                            om = PicklistAM.OptionSet.Options[0];

                                            break;
                                        default:
                                            om = PicklistAM.OptionSet.Options[0];
                                            //sb.AppendFormat("\t{0}\t{1}\t{2}", om.Value, om.Label.UserLocalizedLabel.Label, om.Label.UserLocalizedLabel.LanguageCode);
                                            for (int j = 1; j < PicklistAM.OptionSet.Options.Count; j++)
                                            {
                                                //sb.AppendLine();
                                                //for (int i = 0; i < tabIndex; i++) { //sb.Append("\t"); }
                                                ////om = PicklistAM.OptionSet.Options[j];
                                                ////sb.AppendFormat("\t{0}\t{1}\t{2}", om.Value, om.Label.UserLocalizedLabel.Label, om.Label.UserLocalizedLabel.LanguageCode);
                                            }
                                            break;
                                    }
                                    //sb.AppendLine();
                                    break;

                                //case AttributeTypeCode.State:
                                //    sb.AppendFormat("\t{0}\t\t", ((StateAttributeMetadata)attribute).OptionSet.OptionSetType.ToString());
                                //    break;

                                //case AttributeTypeCode.Status:
                                //    sb.AppendFormat("\t{0}\t\t", ((StatusAttributeMetadata)attribute).OptionSet.OptionSetType.ToString());
                                //    break;

                                case AttributeTypeCode.String:
                                    ddMeta.DataType = "String";
                                    FormulaDefinition = ((StringAttributeMetadata)attribute).FormulaDefinition;
                                    //sb.AppendFormat("\t{0}\t{1}\t\t{2}\t\t\t"
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (FormulaDefinition.Trim().StartsWith("<?"))
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (!FormulaDefinition.Trim().StartsWith("<?") && FormulaDefinition.Trim().Length > 0)
                                    //    , ((StringAttributeMetadata)attribute).MaxLength
                                    //    );
                                    break;

                                //case AttributeTypeCode.Uniqueidentifier:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((UniqueIdentifierAttributeMetadata)attribute).IsPrimaryId, ((UniqueIdentifierAttributeMetadata)attribute).MaxValue);
                                //    break;

                                //case AttributeTypeCode.Virtual:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((VirtualAttributeMetadata)attribute).MinValue, ((VirtualAttributeMetadata)attribute).MaxValue);
                                //    break;

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
    }
}
