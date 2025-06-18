using DataDictionary.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.IO;
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
            ProcessAttributesAsync();
            LogSchema();
            Console.WriteLine($"Processed {_ddSolutions.Count} solutions with components and entities.");




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
                        Console.WriteLine($"Processing Entity Component: {ddComponent.ObjectId} in Solution: {ddSolution.UniqueName}");

                        var entityQuery = new QueryExpression("entity")
                        {
                            ColumnSet = new ColumnSet("name", "objecttypecode", "basetablename", "collectionname", "componentstate", "entityid", "entitysetname", "isactivity", "logicalname"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("entityid", ConditionOperator.Equal, ddComponent.ObjectId) // Corrected spelling error: entityid -> entityId
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
                                EntityId = entity.GetAttributeValue<Guid>("entityid"), // Corrected spelling error: entityid -> entityId
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
        }
        #endregion

        public async Task ProcessAttributesAsync()
        {
            foreach (DataDictionarySolution ddSolution in _ddSolutions.Values)
            {
                foreach (DataDictionarySolutionComponent ddComponent in ddSolution.Components)
                {
                    if (ddComponent.ComponentType == 2) // 2 is the type for Attribute
                    {
                        // Retrieve attribute details here
                        Console.WriteLine($"Processing Attribute Component: {ddComponent.ObjectId} in Solution: {ddSolution.UniqueName}");
                        var attributeQuery = new QueryExpression("attribute")
                        {
                            ColumnSet = new ColumnSet("attributeid", "attributeof", "attributetypeid", "componentstate", "externalname", "logicalname"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("attributeid", ConditionOperator.Equal, ddComponent.ObjectId)
                                }
                            }
                        };

                        var results = _service.RetrieveMultiple(attributeQuery);
                        foreach (var attribute in results.Entities)
                        {
                            DataDictionaryAttribute ddAttribute = new DataDictionaryAttribute
                            {
                                AttributeId = attribute.GetAttributeValue<Guid>("attributeid"),
                                AttributeOf = attribute.GetAttributeValue<Guid>("attributeof"),
                                AttributeTypeId = attribute.GetAttributeValue<Guid>("attributetypeid"), // Fixed CS0029: Correct type conversion
                                LogicalName = attribute.GetAttributeValue<string>("logicalname"),
                            };
                           
                            ddSolution.AddAttribute(ddSolution, ddAttribute);
                            Console.WriteLine($"Attribute: {ddAttribute.AttributeName}, Logical Name: {ddAttribute.LogicalName}");
                        }


                    }
                }
            }

            //var accessToken = await DataverseWebApiHelper.GetAccessTokenAsync(Program.TENANTID, Program.CLIENTID, Program.CLIENTSECRET, Program.CRMURL);
            //Console.WriteLine("Access Token Retrieved Successfully."); ;

        }


        public void LogSchema()
        {
            RetrieveAllEntitiesRequest request = null;
            RetrieveAllEntitiesResponse response = null;
            //StringBuilder sb = null;
            PicklistAttributeMetadata PicklistAM = null;
            OptionMetadata om = null;
            //int tabDepth = 0;
            //int tabIndex = 0;
            string FormulaDefinition = null;

            try
            {
                Console.Write("Retrieving Metadata .");
                request = new RetrieveAllEntitiesRequest()
                {
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes,                   RetrieveAsIfPublished = false,
                };
                response = (RetrieveAllEntitiesResponse)_service.Execute(request);

                if (response != null)
                {
                    foreach (EntityMetadata entity in response?.EntityMetadata)
                    {
                        foreach (AttributeMetadata attribute in entity.Attributes)
                        {   //                0                             6
                            //sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", entity.LogicalName, (attribute.DisplayName.UserLocalizedLabel == null ? String.Empty : attribute.DisplayName.UserLocalizedLabel.Label), attribute.LogicalName, attribute.SchemaName, attribute.AttributeType.Value.ToString(), attribute.IsCustomAttribute, attribute.IsAuditEnabled.Value);

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
                            
                            switch (attribute.AttributeType)
                            {
                                //case AttributeTypeCode.Boolean:
                                //    break;

                                case AttributeTypeCode.BigInt:
                                    //ddMeta.DataType = "BigInt";
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
                                    //ddMeta.DataType = "DateTime";
                                    ddMeta.FormulaDefinition = ((DateTimeAttributeMetadata)attribute).FormulaDefinition;
                                    FormulaDefinition = ((DateTimeAttributeMetadata)attribute).FormulaDefinition;
                                    //sb.AppendFormat("\t{0}\t{1}\t\t\t\t\t"
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (FormulaDefinition.Trim().StartsWith("<?"))
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (!FormulaDefinition.Trim().StartsWith("<?") && FormulaDefinition.Trim().Length > 0)
                                    //    );
                                    break;

                                case AttributeTypeCode.Decimal:
                                    ddMeta.DataType = "Decimal";
                                    FormulaDefinition = ((DecimalAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((DecimalAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((DecimalAttributeMetadata)attribute).MaxValue;
                                    ddMeta.Precision = ((DecimalAttributeMetadata)attribute).Precision;
                                    //sb.AppendFormat("\t{0}\t{1}\t\t\t{2}\t{3}\t{4}"
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (FormulaDefinition.Trim().StartsWith("<?"))
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (!FormulaDefinition.Trim().StartsWith("<?") && FormulaDefinition.Trim().Length > 0)
                                    //    , ((DecimalAttributeMetadata)attribute).MinValue
                                    //    , ((DecimalAttributeMetadata)attribute).MaxValue
                                    //    , ((DecimalAttributeMetadata)attribute).Precision);
                                    break;

                                case AttributeTypeCode.Double:
                                    ddMeta.DataType = "Double";
                                    ddMeta.MinValue = (Int64?)((DoubleAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((DoubleAttributeMetadata)attribute).MaxValue;
                                    //sb.AppendFormat("\t\t\t{0}\t{1}\t", ((DoubleAttributeMetadata)attribute).MinValue, ((DoubleAttributeMetadata)attribute).MaxValue);
                                    break;

                                //case AttributeTypeCode.EntityName:
                                //    // !! sb.AppendFormat("\t\t{0}\t{1}", ((EntityNameAttributeMetadata)attribute).IsPrimaryId, ((EntityNameAttributeMetadata)attribute).IsPrimaryName);
                                //    break;

                                case AttributeTypeCode.Integer:
                                    ddMeta.DataType = "Integer";
                                    FormulaDefinition = ((IntegerAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((IntegerAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((IntegerAttributeMetadata)attribute).MaxValue;
                                    //sb.AppendFormat("\t{0}\t{1}\t\t\t{2}\t{3}\t"
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (FormulaDefinition.Trim().StartsWith("<?"))
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (!FormulaDefinition.Trim().StartsWith("<?") && FormulaDefinition.Trim().Length > 0)
                                    //    , ((IntegerAttributeMetadata)attribute).MinValue
                                    //    , ((IntegerAttributeMetadata)attribute).MaxValue
                                    //    );
                                    break;

                                case AttributeTypeCode.Lookup:
                                    ddMeta.DataType = "Lookup";
                                    // Fix for CS1061: 'LookupAttributeMetadata' does not contain a definition for 'TargetEntityType'
                                    // The error indicates that 'TargetEntityType' is not a valid property of 'LookupAttributeMetadata'.
                                    // Based on the provided type signature, 'LookupAttributeMetadata' has a 'Targets' property which is an array of strings.
                                    // Replace 'TargetEntityType' with 'Targets' and adjust the code accordingly.

                                    ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).Targets != null
                                        ? string.Join(",", ((LookupAttributeMetadata)attribute).Targets)
                                        : null;
                                    // Fix for CS1061: 'LookupAttributeMetadata' does not contain a definition for 'TargetEntityType'
                                    // The error indicates that 'TargetEntityType' is not a valid property of 'LookupAttributeMetadata'.
                                    // Based on the provided type signature, 'LookupAttributeMetadata' has a 'Targets' property which is an array of strings.
                                    // Replace 'TargetEntityType' with 'Targets' and adjust the code accordingly.

                                    ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).Targets != null
                                        ? string.Join(",", ((LookupAttributeMetadata)attribute).Targets)
                                        : null;
                                    //ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).TargetEntityType;
                                    //sb.AppendFormat("\t\t\t{0}\t\t\t\t", string.Join(",", ((LookupAttributeMetadata)attribute).Targets));
                                    break;

                                //case AttributeTypeCode.ManagedProperty:
                                //    // sb.AppendFormat("\t{0}\t\t", ((ManagedPropertyAttributeMetadata)attribute));
                                //    break;

                                case AttributeTypeCode.Memo:
                                    ddMeta.DataType = "Memo";
                                    ddMeta.MaxLength = ((MemoAttributeMetadata)attribute).MaxLength;
                                    //sb.AppendFormat("\t\t\t\t{0}\t\t\t", ((MemoAttributeMetadata)attribute).MaxLength);
                                    break;

                                case AttributeTypeCode.Money:
                                    ddMeta.DataType = "Money";
                                    FormulaDefinition = ((MoneyAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((MoneyAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((MoneyAttributeMetadata)attribute).MaxValue;
                                    ////sb.AppendFormat("\t{0}\t{1}\t\t\t{2}\t{3}\t"
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (FormulaDefinition.Trim().StartsWith("<?"))
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (!FormulaDefinition.Trim().StartsWith("<?") && FormulaDefinition.Trim().Length > 0)
                                    //    , ((MoneyAttributeMetadata)attribute).MinValue
                                    //    , ((MoneyAttributeMetadata)attribute).MaxValue
                                    //    );
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
                                    // sb.AppendFormat("\t\t\t\t", ((PicklistAttributeMetadata)attribute).OptionSet.OptionSetType.ToString());
                                    //sb.AppendFormat("\t{0}\t{1}\t\t\t\t\t"
                                        //, String.IsNullOrEmpty(FormulaDefinition) ? false : (FormulaDefinition.Trim().StartsWith("<?"))
                                        //, String.IsNullOrEmpty(FormulaDefinition) ? false : (!FormulaDefinition.Trim().StartsWith("<?") && FormulaDefinition.Trim().Length > 0)
                                        //);
                                    //tabDepth = 17;
                                    //tabIndex = 14;
                                    PicklistAM = (PicklistAttributeMetadata)attribute;
                                    //if (!String.IsNullOrEmpty(PicklistAM.OptionSet.Name)) { sb.AppendFormat("\t{0}", PicklistAM.OptionSet.Name); }
                                    //else { sb.Append("\t"); }

                                    switch (PicklistAM.OptionSet.Options.Count)
                                    {
                                        case 0:
                                            //sb.Append("\t\t\t");
                                            break;
                                        case 1:
                                            om = PicklistAM.OptionSet.Options[0];
                                            //sb.AppendFormat("\t{0}\t{1}\t{2}", om.Value, om.Label.UserLocalizedLabel.Label, om.Label.UserLocalizedLabel.LanguageCode);
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
                                    //sb.Append("\t\t\t\t\t\t\t");
                                    break;
                            }

                            //sb.AppendLine();
                            _ddSolutions["SampleSolution"].AttributeMetadata.Add(ddMeta);
                        }
                    }
                    //string outputfilename = string.Format("{0}_{1}_{2}_{3}_schema.txt", DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("t").Replace(":", ""), connection.ConnectedOrgUniqueName, connection.ConnectedOrgVersion.ToString(3));
                    //File.WriteAllText(string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), outputfilename), sb.ToString());
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
                //tabIndex = 0;
                //tabDepth = 0;
                om = null;
                PicklistAM = null;
                //sb = null;
                response = null;
                request = null;
            }

        }




    }
}
