using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DataDictionary
{
    /// <summary>
    /// Helper class for inserting and updating Data Dictionary data into Dataverse tables.
    /// </summary>
    public class DataverseDataHelper
    {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracing;

        public DataverseDataHelper(IOrganizationService service, ITracingService tracing)
        {
            _service = service;
            _tracing = tracing;
        }

        public Entity UpsertEntityRecord(string logicalName, string displayName, string description)
        {
            var entity = new Entity("ljr_table");
            entity["ljr_name"] = logicalName;
            entity["ljr_schemaname"] = displayName;

            var query = new QueryExpression("ljr_table")
            {
                ColumnSet = new ColumnSet("ljr_tableid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("ljr_name", ConditionOperator.Equal, logicalName)
                    }
                }
            };
            var results = _service.RetrieveMultiple(query);
            if (results.Entities.Count > 0)
            {
                entity.Id = results.Entities[0].Id;
                _service.Update(entity);
                _tracing.Trace($"Updated entity record for {logicalName}");
            }
            else
            {
                _service.Create(entity);
                _tracing.Trace($"Created entity record for {logicalName}");
            }
            return (entity);
        }

        public void UpsertFieldRecord(FieldMetadata field, Dictionary<string, string[]> entityRecords)
        {
            try
            {
                var entity = new Entity("ljr_column");
                //entity["ljr_table"] = entityRecords[field.EntityName][0];
                entity["ljr_name"] = field.EntityName.ToLower() + "." + field.SchemaName.ToLower();
                entity["ljr_schemaname"] = field.SchemaName;
                entity["ljr_displayname"] = field.DisplayName;
                entity["ljr_description"] = field.Description;
                entity["ljr_type"] = field.Type;
                entity["ljr_requiredlevel"] = field.RequiredLevel;
                entity["ljr_maxlength"] = field.MaxLength;
                entity["ljr_precision"] = field.Precision;
                entity["ljr_minvalue"] = field.MinValue;
                entity["ljr_maxvalue"] = field.MaxValue;
                //entity["ljr_hiddenbyscript"] = field.HiddenByScript;
                //entity["ljr_permissions"] = field.Permissions;
                //entity["ljr_solutionnames"] = field.SolutionNames != null ? string.Join(";", field.SolutionNames) : null;
                //entity["ljr_scriptreferences"] = field.ScriptReferences != null ? string.Join(";", field.ScriptReferences) : null;

                var query = new QueryExpression("ljr_column")
                {
                    ColumnSet = new ColumnSet("ljr_columnid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            //new ConditionExpression("ljr_table", ConditionOperator.Equal, entityRecords[field.EntityName][0]),
                            new ConditionExpression("ljr_name", ConditionOperator.Equal, field.EntityName.ToLower() + "." + field.SchemaName.ToLower())
                        }
                    }
                };
                var results = _service.RetrieveMultiple(query);
                if (results.Entities.Count > 0)
                {
                    entity.Id = results.Entities[0].Id;
                    _service.Update(entity);
                    _tracing.Trace($"Updated field record for {field.EntityName}.{field.SchemaName}");
                }
                else
                {
                    _service.Create(entity);
                    _tracing.Trace($"Created field record for {field.EntityName}.{field.SchemaName}");
                }
            }
            catch (Exception ex)
            {
                _tracing.Trace($"Exception in UpsertFieldRecord for {field.EntityName}.{field.SchemaName}: {ex}");
            }
        }

        //public void UpsertWebResourceRecord(WebResourceInfo wr)
        //{
        //    var entity = new Entity("new_datadictionarywebresource");
        //    entity["new_name"] = wr.Name;
        //    entity["new_displayname"] = wr.DisplayName;
        //    entity["new_path"] = wr.Path;

        //    var query = new QueryExpression("new_datadictionarywebresource")
        //    {
        //        ColumnSet = new ColumnSet("new_datadictionarywebresourceid"),
        //        Criteria = new FilterExpression
        //        {
        //            Conditions =
        //            {
        //                new ConditionExpression("new_name", ConditionOperator.Equal, wr.Name)
        //            }
        //        }
        //    };
        //    var results = _service.RetrieveMultiple(query);
        //    if (results.Entities.Count > 0)
        //    {
        //        entity.Id = results.Entities[0].Id;
        //        _service.Update(entity);
        //        _tracing.Trace($"Updated web resource record for {wr.Name}");
        //    }
        //    else
        //    {
        //        _service.Create(entity);
        //        _tracing.Trace($"Created web resource record for {wr.Name}");
        //    }
        //}

        public void UpsertScriptReference(string scriptName)
        {
            var entity = new Entity("new_datadictionaryscriptreference");
            entity["new_name"] = scriptName;

            var query = new QueryExpression("new_datadictionaryscriptreference")
            {
                ColumnSet = new ColumnSet("new_datadictionaryscriptreferenceid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("new_name", ConditionOperator.Equal, scriptName)
                    }
                }
            };
            var results = _service.RetrieveMultiple(query);
            if (results.Entities.Count > 0)
            {
                entity.Id = results.Entities[0].Id;
                _service.Update(entity);
                _tracing.Trace($"Updated script reference record for {scriptName}");
            }
            else
            {
                _service.Create(entity);
                _tracing.Trace($"Created script reference record for {scriptName}");
            }
        }
    }
}