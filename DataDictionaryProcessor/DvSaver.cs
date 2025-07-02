using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataDictionaryProcessor
{
    public class DvSaver
    {


        public DvSaver(CrmServiceClient serviceClient, DataDictionary.Models.DataDictionary ddModel)
        {
            _serviceClient = serviceClient;
            _ddModel = ddModel;
        }


        #region Private Fields

        private CrmServiceClient _serviceClient;
        DataDictionary.Models.DataDictionary _ddModel = new DataDictionary.Models.DataDictionary();


        #endregion


        public void OLDSaveToDataverse(DataDictionary.Models.DataDictionary ddModel)
        {
            _ddModel = ddModel;
            if (_serviceClient == null || !_serviceClient.IsReady)
            {
                throw new InvalidOperationException("Service client is not initialized or not connected.");
            }
            try
            {
                // Save entities
                foreach (var entity in _ddModel.Entities.Values)
                {
                    // Logic to save entity to Dataverse
                    DictionaryOrchestrator.LogEvent($"Saving entity: {entity.EntitySetName}");

                    foreach (var attribute in entity.Attributes)
                    {
                        // Logic to save attribute to Dataverse
                        //DictionaryOrchestrator.LogEvent($"  Saving attribute: {attribute.AttributeName}");

                        foreach (var modification in attribute.Modifications)
                        {
                            // Logic to save modification to Dataverse
                            //DictionaryOrchestrator.LogEvent($"    Saving modification: {modification.FieldName} with value {modification.FieldName}");
                        }
                    }
                }
                DictionaryOrchestrator.LogEvent("Data dictionary saved successfully.");
            }
            catch (Exception ex)
            {
                DictionaryOrchestrator.LogEvent($"Error saving data dictionary: {ex.Message}");
            }

        }


        #region Private Methods

        public void SaveToDataverse()
        {

            // Save any web resources first so we can relate to them


            foreach (var resource in _ddModel.WebResources)
            {
                DictionaryOrchestrator.LogEvent($"Saving web resource: {resource.Value}");
                SaveJavascriptToDataverse();

            }

            // Use ExecuteMultipleRequest for batch saving to Dataverse
            const int batchSize = 1000;

            var seenAltKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var requests = new List<OrganizationRequest>(batchSize);

            foreach (var entity in _ddModel.Entities)
            {
                foreach (var attr in entity.Value.Attributes)
                {
                    // Construct the alternate key value
                    var altKey = $"{attr.Metadata.Table}.{attr.Metadata.ColumnSchema}";
                    if (string.IsNullOrWhiteSpace(altKey))
                    {
                        //DictionaryOrchestrator.LogEvent("Skipping record with null/empty alternate key.");
                        continue;
                    }
                    // Check for duplicate alternate key in the current batch
                    if (!seenAltKeys.Add(altKey))
                    {
                        //DictionaryOrchestrator.LogEvent($"Duplicate alternate key detected in batch: '{altKey}'. Skipping this record.");
                        continue;
                    }

                    var newRec = new Entity("ljr_datadictionaryattributemetadata");
                    newRec["ljr_datadictionaryattributemetadata1"] = altKey;
                    newRec["ljr_table"] = attr.Metadata.Table;
                    newRec["ljr_columndisplay"] = attr.Metadata.ColumnDisplay;
                    newRec["ljr_columnlogical"] = attr.Metadata.ColumnLogical;
                    newRec["ljr_columnschema"] = attr.Metadata.ColumnSchema;
                    newRec["ljr_datatype"] = attr.Metadata.DataType;
                    newRec["ljr_formuladefinition"] = attr.Metadata.FormulaDefinition;
                    newRec["ljr_iscustom"] = attr.Metadata.IsCustom;
                    newRec["ljr_auditenabled"] = attr.Metadata.AuditEnabled;
                    newRec["ljr_iscalculated"] = attr.Metadata.IsCalculated;
                    newRec["ljr_isformula"] = attr.Metadata.IsFormula;
                    newRec["ljr_lookupto"] = attr.Metadata.LookupTo;
                    newRec["ljr_metadatamodifiedon"] = attr.Metadata.ModifiedOn != DateTime.MinValue ? (object)attr.Metadata.ModifiedOn : null;
                    newRec["ljr_attributeof"] = attr.Metadata.AttributeOf;
                    newRec["ljr_attributetype"] = attr.Metadata.AttributeType;
                    newRec["ljr_attributetypename"] = attr.Metadata.AttributeTypeName;
                    newRec["ljr_autonumberformat"] = attr.Metadata.AutoNumberFormat;
                    newRec["ljr_canbesecuredforcreate"] = attr.Metadata.CanBeSecuredForCreate ?? false;
                    newRec["ljr_canbesecuredforread"] = attr.Metadata.CanBeSecuredForRead ?? false;
                    newRec["ljr_canbesecuredforupdate"] = attr.Metadata.CanBeSecuredForUpdate ?? false;
                    newRec["ljr_canmodifiedadditionalsettings"] = attr.Metadata.CanModifiedAdditionalSettings ?? false;
                    newRec["ljr_columnnumber"] = attr.Metadata.ColumnNumber ?? 0;
                    newRec["ljr_deprecatedversion"] = attr.Metadata.DeprecatedVersion;
                    newRec["ljr_displayname"] = attr.Metadata.DisplayName;
                    newRec["ljr_entitylogicalname"] = attr.Metadata.EntityLogicalName;
                    newRec["ljr_externalname"] = attr.Metadata.ExternalName;
                    newRec["ljr_haschanged"] = attr.Metadata.HasChanged ?? false;
                    newRec["ljr_inheritsfrom"] = attr.Metadata.InheritsFrom;
                    newRec["ljr_introducedversion"] = attr.Metadata.IntroducedVersion;
                    newRec["ljr_isauditenabled"] = attr.Metadata.IsAuditEnabled ?? false;
                    newRec["ljr_metadatacreatedon"] = attr.Metadata.CreatedOn != DateTime.MinValue ? (object)attr.Metadata.CreatedOn : null;

                    if (attr.Metadata.MaxValue != null)
                        newRec["ljr_maxvalue"] = attr.Metadata.MaxValue;
                    if (attr.Metadata.MinValue != null)
                        newRec["ljr_minvalue"] = attr.Metadata.MinValue;
                    if (attr.Metadata.Precision != null)
                        newRec["ljr_precision"] = attr.Metadata.Precision;
                    if (attr.Metadata.MaxLength != null)
                        newRec["ljr_maxlength"] = attr.Metadata.MaxLength;

                    if (attr.Metadata.OptionSet != null)
                        newRec["ljr_optionset"] = attr.Metadata.OptionSet;
                    if (attr.Metadata.Value != null)
                        newRec["ljr_value"] = attr.Metadata.Value;

                    newRec["ljr_description"] = attr.Metadata.Description;
                    newRec["ljr_langcode"] = attr.Metadata.LangCode;

                    newRec["ljr_ishiddenbyscript"] = attr.Metadata.IsHiddenByScript ?? false;
                    newRec["ljr_isrequiredbyscript"] = attr.Metadata.IsRequiredByScript ?? false;
                    newRec["ljr_hasdefaultvaluebyscript"] = attr.Metadata.HasDefaultValueByScript ?? false;
                    if (!string.IsNullOrWhiteSpace(attr.Metadata.ScriptDefaultValue))
                        newRec["ljr_scriptdefaultvalue"] = attr.Metadata.ScriptDefaultValue;
                    if (!string.IsNullOrWhiteSpace(attr.Metadata.ModifyingWebResources))
                    {
                        newRec["ljr_modifyingwebresource"] = attr.Metadata.ModifyingWebResources;
                        Guid webresourceId = _ddModel.WebResources[attr.Metadata.ModifyingWebResources].WebResourceId;
                        newRec["ljr_webresource"] = new EntityReference("ljr_webresource", webresourceId);
                    }
                    // --- Check if record exists in Dataverse by alternate key ---
                    bool exists = false;
                    Guid existingId = Guid.Empty;

                    // Query by alternate key (ljr_datadictionaryattributemetadata1)
                    var query = new QueryExpression("ljr_datadictionaryattributemetadata")
                    {
                        ColumnSet = new ColumnSet("ljr_datadictionaryattributemetadataid"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("ljr_datadictionaryattributemetadata1", ConditionOperator.Equal, altKey)
                            }
                        },
                        TopCount = 1
                    };

                    var result = _serviceClient.RetrieveMultiple(query);
                    if (result.Entities.Count > 0)
                    {
                        exists = true;
                        existingId = result.Entities[0].Id;
                    }

                    OrganizationRequest req;
                    if (exists)
                    {
                        newRec.Id = existingId;
                        req = new UpdateRequest { Target = newRec };
                    }
                    else
                    {
                        req = new CreateRequest { Target = newRec };
                    }
                    requests.Add(req);

                    // If batch is full, execute and clear
                    if (requests.Count == batchSize)
                    {
                        ExecuteBatch(requests);
                        requests.Clear();
                    }
                }
            }

            // Execute any remaining requests
            if (requests.Count > 0)
            {
                ExecuteBatch(requests);
            }
        }

        private void ExecuteBatch(List<OrganizationRequest> requests)
        {
            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Requests = new OrganizationRequestCollection(),
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                }
            };

            foreach (var req in requests)
            {
                executeMultipleRequest.Requests.Add(req);
            }

            var response = (ExecuteMultipleResponse)_serviceClient.Execute(executeMultipleRequest);

            foreach (var resp in response.Responses)
            {
                if (resp.Fault != null)
                {
                    DictionaryOrchestrator.LogEvent($"Error in batch operation: {resp.Fault.Message}");
                }
            }
        }


        /// <summary>
        /// Saves each dependency of a web resource to a related table in Dataverse.
        /// </summary>
        /// <param name="webResource">The DataDictionaryWebResource instance.</param>
        /// <param name="webResourceRecordId">The Dataverse record ID of the parent web resource.</param>
        private void SaveWebResourceDependenciesToDataverse(DataDictionary.Models.DataDictionaryWebResource webResource, Guid webResourceRecordId)
        {
            if (webResource.ParsedDependencies == null || webResource.ParsedDependencies.Count == 0)
                return;

            foreach (var dependency in webResource.ParsedDependencies)
            {
                try
                {
                    var entity = new Entity("ljr_webresourcedependency");
                    entity["ljr_webresourceid"] = new EntityReference("ljr_webresource", webResourceRecordId);
                    if (dependency.AttributeId.HasValue)
                        entity["ljr_attributeid"] = dependency.AttributeId.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeName))
                        entity["ljr_attributename"] = dependency.AttributeName;
                    if (!string.IsNullOrWhiteSpace(dependency.EntityName))
                        entity["ljr_entityname"] = dependency.EntityName;
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeType))
                        entity["ljr_attributetype"] = dependency.AttributeType;
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeLogicalName))
                        entity["ljr_attributelogicalname"] = dependency.AttributeLogicalName;
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeType))
                        entity["ljr_componenttype"] = dependency.AttributeType;
                    if (!string.IsNullOrWhiteSpace(dependency.EntityName) && !string.IsNullOrWhiteSpace(dependency.AttributeLogicalName))
                    {
                        entity["ljr_webresourcedependencyname"] = dependency.EntityName + "-" + dependency.AttributeLogicalName;

                        // Query 1: Check if ljr_webresourcedependency exists (update or insert)
                        var dependencyQuery = new QueryExpression("ljr_webresourcedependency")
                        {
                            ColumnSet = new ColumnSet("ljr_webresourcedependencyid"),
                            Criteria = new FilterExpression
                            {
                                FilterOperator = LogicalOperator.And
                            }
                        };
                        dependencyQuery.Criteria.AddCondition("ljr_webresourceid", ConditionOperator.Equal, webResourceRecordId);
                        if (dependency.AttributeId.HasValue)
                            dependencyQuery.Criteria.AddCondition("ljr_attributeid", ConditionOperator.Equal, dependency.AttributeId.Value.ToString());
                        else if (!string.IsNullOrWhiteSpace(dependency.AttributeLogicalName) && !string.IsNullOrWhiteSpace(dependency.EntityName))
                        {
                            dependencyQuery.Criteria.AddCondition("ljr_attributelogicalname", ConditionOperator.Equal, dependency.AttributeLogicalName);
                            dependencyQuery.Criteria.AddCondition("ljr_entityname", ConditionOperator.Equal, dependency.EntityName);
                        }
                        var dependencyResult = _serviceClient.RetrieveMultiple(dependencyQuery);

                        // Query 2: Lookup ljr_datadictionaryattributemetadata by table and field name
                        Guid? attributeMetadataId = null;
                        if (!string.IsNullOrWhiteSpace(dependency.EntityName) && !string.IsNullOrWhiteSpace(dependency.AttributeLogicalName))
                        {
                            var attrMetaQuery = new QueryExpression("ljr_datadictionaryattributemetadata")
                            {
                                ColumnSet = new ColumnSet("ljr_datadictionaryattributemetadataid"),
                                Criteria = new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And
                                }
                            };
                            attrMetaQuery.Criteria.AddCondition("ljr_table", ConditionOperator.Equal, dependency.EntityName);
                            attrMetaQuery.Criteria.AddCondition("ljr_columnlogical", ConditionOperator.Equal, dependency.AttributeLogicalName);
                            var attrMetaResult = _serviceClient.RetrieveMultiple(attrMetaQuery);
                            if (attrMetaResult.Entities.Count > 0)
                            {
                                attributeMetadataId = attrMetaResult.Entities[0].GetAttributeValue<Guid>("ljr_datadictionaryattributemetadataid");
                                entity["ljr_attributemetadatalookup"] = new EntityReference("ljr_datadictionaryattributemetadata", attributeMetadataId.Value);
                            }
                        }

                        if (dependencyResult.Entities.Count > 0)
                        {
                            // Update existing record
                            entity.Id = dependencyResult.Entities[0].Id;
                            _serviceClient.Update(entity);
                        }
                        else
                        {
                            // Create new record
                            _serviceClient.Create(entity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DictionaryOrchestrator.LogEvent($"Error saving dependency for Web Resource '{webResource.DisplayName}': {ex.Message}");
                }
            }
        }

        private void SaveJavaScriptFieldModifications()
        {
            DictionaryOrchestrator.LogEvent("Saving JavaScript field modifications to Dataverse...");

            // Field modifications are stored as a child under the attribute metadata
            foreach (var entityPair in _ddModel.Entities)
            {
                foreach (var attr in entityPair.Value.Attributes)
                {
                    if (attr.Modifications == null || attr.Modifications.Count == 0)
                        continue;

                    foreach (var modification in attr.Modifications)
                    {
                        // Compose alternate key (fieldname + entityname + modificationtype)
                        var altKey = $"{modification.FieldName}|{attr.Metadata.Table}|{modification.ModificationType}";

                        // Query to check if record exists
                        var query = new QueryExpression("ljr_javascriptfieldmodification")
                        {
                            ColumnSet = new ColumnSet("ljr_name"),
                            Criteria = new FilterExpression
                            {
                                FilterOperator = LogicalOperator.And
                            }
                        };
                        query.Criteria.AddCondition("ljr_name", ConditionOperator.Equal, altKey);
                        
                        var result = _serviceClient.RetrieveMultiple(query);

                        var jsModEntity = new Entity("ljr_javascriptfieldmodification");
                        jsModEntity["ljr_fieldname"] = modification.FieldName;

                        jsModEntity["ljr_name"] = altKey;
                        jsModEntity["ljr_modificationtype"] = modification.ModificationType.ToString();
                        if (!string.IsNullOrWhiteSpace(modification.ModificationValue))
                            jsModEntity["ljr_modificationvalue"] = modification.ModificationValue;
                        if (!string.IsNullOrWhiteSpace(modification.JavaScriptCode))
                            jsModEntity["ljr_javascriptcode"] = modification.JavaScriptCode;
                        if (modification.LineNumber.HasValue)
                            jsModEntity["ljr_linenumber"] = modification.LineNumber.Value;
                        if (!string.IsNullOrWhiteSpace(modification.AttributeTable))
                            jsModEntity["ljr_attributetable"] = modification.AttributeTable;
                        if (!string.IsNullOrWhiteSpace(modification.AttributeLogicalName))
                            jsModEntity["ljr_attributelogicalname"] = modification.AttributeLogicalName;

                        // Optionally relate to parent attribute metadata if available
                        // (Assumes ljr_datadictionaryattributemetadata has alternate key: table + columnlogical)
                        if (!string.IsNullOrWhiteSpace(attr.Metadata.Table) && !string.IsNullOrWhiteSpace(attr.Metadata.ColumnLogical))
                        {
                            var attrMetaQuery = new QueryExpression("ljr_datadictionaryattributemetadata")
                            {
                                ColumnSet = new ColumnSet("ljr_datadictionaryattributemetadataid"),
                                Criteria = new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And
                                }
                            };
                            attrMetaQuery.Criteria.AddCondition("ljr_table", ConditionOperator.Equal, attr.Metadata.Table);
                            attrMetaQuery.Criteria.AddCondition("ljr_columnlogical", ConditionOperator.Equal, attr.Metadata.ColumnLogical);
                            var attrMetaResult = _serviceClient.RetrieveMultiple(attrMetaQuery);

                            //var attrMetaId = attrMetaResult.Entities[0].Id;
                            var webName = attr.Metadata.ModifyingWebResources;

                            // Query to find ljr_webresource by display name (webName), returning only the ID
                            var webResourceId = Guid.Empty;
                            if (!string.IsNullOrWhiteSpace(webName))
                            {
                                var webResourceQuery = new QueryExpression("ljr_webresource")
                                {
                                    ColumnSet = new ColumnSet("ljr_webresourceid"),
                                    Criteria = new FilterExpression
                                    {
                                        FilterOperator = LogicalOperator.And
                                    }
                                };
                                webResourceQuery.Criteria.AddCondition("ljr_displayname", ConditionOperator.Equal, webName);

                                var webResourceResult = _serviceClient.RetrieveMultiple(webResourceQuery);
                                if (webResourceResult.Entities.Count > 0)
                                {
                                    webResourceId = webResourceResult.Entities[0].Id;
                                }
                            }

                            jsModEntity["ljr_webresourcelookup"] = new EntityReference("ljr_webresource", webResourceId);

                        }

                        if (result.Entities.Count > 0)
                        {
                            // Update existing record
                            jsModEntity.Id = result.Entities[0].Id;
                            _serviceClient.Update(jsModEntity);
                            DictionaryOrchestrator.LogEvent($"Updated JavaScript field modification: {altKey}");
                        }
                        else
                        {
                            // Create new record
                            _serviceClient.Create(jsModEntity);
                            DictionaryOrchestrator.LogEvent($"Created JavaScript field modification: {altKey}");
                        }
                    }
                }
            }
        }

        private void SaveJavascriptToDataverse()
        {
            // Save all web resources to Dataverse (Upsert)

            foreach (var webResource in _ddModel.WebResources)
            {
                try
                {
                    // --- Upsert logic: use ljr_displayname as unique key (adjust if you have a better unique key) ---
                    var query = new QueryExpression("ljr_webresource")
                    {
                        ColumnSet = new ColumnSet("ljr_webresourceid", "ljr_name", "ljr_displayname"),
                        Criteria = new FilterExpression
                        {
                            FilterOperator = LogicalOperator.And
                        }
                    };
                    query.Criteria.AddCondition("ljr_displayname", ConditionOperator.Equal, webResource.Value.DisplayName);

                    var result = _serviceClient.RetrieveMultiple(query);

                    var entity = new Entity("ljr_webresource");
                    entity["ljr_displayname"] = webResource.Value.DisplayName;
                    entity["ljr_name"] = webResource.Value.DisplayName;
                    entity["ljr_javascript"] = DecodeBase64String(webResource.Value.Content);
                    entity["ljr_dependencyxml"] = webResource.Value.DependencyXml;
                    if (!string.IsNullOrWhiteSpace(webResource.Value.ParsedDependenciesJson))
                        entity["ljr_parseddependencies"] = webResource.Value.ParsedDependenciesJson;

                    Guid webResourceRecordId;
                    if (result.Entities.Count > 0)
                    {
                        // Update existing record
                        entity.Id = result.Entities[0].Id;
                        _serviceClient.Update(entity);
                        webResourceRecordId = entity.Id;
                        DictionaryOrchestrator.LogEvent($"Updated Web Resource: {webResource.Value.DisplayName} ({webResource.Value.WebResourceId})");
                    }
                    else
                    {
                        // Create new record
                        webResourceRecordId = _serviceClient.Create(entity);
                        DictionaryOrchestrator.LogEvent($"Created Web Resource: {webResource.Value.DisplayName} ({webResource.Value.WebResourceId})");
                    }
                    _ddModel.WebResources[webResource.Value.DisplayName].WebResourceId = webResourceRecordId;
                    // Save dependencies for this web resource
                    SaveWebResourceDependenciesToDataverse(webResource.Value, webResourceRecordId);
                }
                catch (Exception ex)
                {
                    DictionaryOrchestrator.LogEvent($"Error upserting Web Resource '{webResource.Value.DisplayName}': {ex.Message}");
                }
            }

            // Save JavaScript field modifications to Dataverse
            SaveJavaScriptFieldModifications();
        }
        // Converts a base64 string to plain text (UTF-8)
        private static string DecodeBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return string.Empty;
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // Not a valid base64 string, return as-is or handle as needed
                return base64;
            }
        }
        #endregion
    }
}

