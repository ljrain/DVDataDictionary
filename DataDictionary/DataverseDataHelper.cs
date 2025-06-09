using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DataDictionary
{
    /// <summary>
    /// Helper class for inserting and updating Data Dictionary data into Dataverse tables.
    /// Provides methods to upsert entity, field, web resource, script reference, and form location records.
    /// </summary>
    public class DataverseDataHelper
    {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracing;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseDataHelper"/> class.
        /// </summary>
        /// <param name="service">The organization service used to interact with Dataverse.</param>
        /// <param name="tracing">The tracing service for logging operations.</param>
        public DataverseDataHelper(IOrganizationService service, ITracingService tracing)
        {
            _service = service;
            _tracing = tracing;
        }

        /// <summary>
        /// Inserts or updates an entity record in the custom table.
        /// </summary>
        /// <param name="logicalName">The logical name of the entity.</param>
        /// <param name="displayName">The display name of the entity.</param>
        /// <param name="description">The description of the entity.</param>
        /// <returns>The upserted entity.</returns>
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

        /// <summary>
        /// Inserts or updates a field record in the custom table.
        /// </summary>
        /// <param name="field">The field metadata to upsert.</param>
        /// <param name="entityRecords">A dictionary of entity records for reference.</param>
        public void UpsertFieldRecord(FieldMetadata field, Dictionary<string, string[]> entityRecords)
        {
            try
            {
                var entity = new Entity("ljr_column");
                entity["ljr_name"] = field.EntityName.ToLower() + "." + field.SchemaName.ToLower();
                entity["ljr_schemaname"] = field.SchemaName;
                entity["ljr_displayname"] = field.DisplayName;
                entity["ljr_description"] = field.Description;
                entity["ljr_type"] = field.Type;

                entity["ljr_precision"] = field.Precision;

                // Only set min/max if the type is correct for the Dataverse column
                // Only set for supported types (IntegerType, DecimalType, DoubleType, MoneyType)
                if (!string.IsNullOrEmpty(field.Type) && (
                    field.Type.Contains("Integer") ||
                    field.Type.Contains("Decimal") ||
                    field.Type.Contains("Double") ||
                    field.Type.Contains("Money")))
                {
                    //if (field.MinValue.HasValue)
                    //    entity["ljr_minvalue"] = field.MinValue.Value;
                    //if (field.MaxValue.HasValue)
                    //    entity["ljr_maxvalue"] = field.MaxValue.Value;
                }

                entity["ljr_hiddenbyscript"] = field.HiddenByScript.ToString();
                entity["ljr_solutionname"] = field.SolutionNames != null ? string.Join(";", field.SolutionNames) : null;
                _tracing.Trace(field.ToString());

                var query = new QueryExpression("ljr_column")
                {
                    ColumnSet = new ColumnSet("ljr_columnid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
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

        /// <summary>
        /// Inserts or updates a web resource record in the custom table.
        /// </summary>
        /// <param name="wr">The web resource information to upsert.</param>
        public void UpsertWebResourceRecord(WebResourceInfo wr)
        {
            var entity = new Entity("ljr_webresource");
            entity["ljr_name"] = wr.Name;
            entity["ljr_displayname"] = wr.DisplayName;
            entity["ljr_path"] = wr.Path;

            var query = new QueryExpression("ljr_webresource")
            {
                ColumnSet = new ColumnSet("ljr_webresourceid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("ljr_name", ConditionOperator.Equal, wr.Name)
                    }
                }
            };
            var results = _service.RetrieveMultiple(query);
            if (results.Entities.Count > 0)
            {
                entity.Id = results.Entities[0].Id;
                _service.Update(entity);
                _tracing.Trace($"Updated web resource record for {wr.Name}");
            }
            else
            {
                _service.Create(entity);
                _tracing.Trace($"Created web resource record for {wr.Name}");
            }
        }

        /// <summary>
        /// Inserts or updates a script reference record in the custom table.
        /// </summary>
        /// <param name="scriptName">The name of the script reference.</param>
        public void UpsertScriptReference(string scriptName)
        {
            var entity = new Entity("ljr_webresource");
            entity["ljr_name"] = scriptName;

            var query = new QueryExpression("ljr_webresource")
            {
                ColumnSet = new ColumnSet("ljr_webresourceid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("ljr_name", ConditionOperator.Equal, scriptName)
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

        /// <summary>
        /// Retrieves all forms associated with a given solution.
        /// </summary>
        /// <param name="solutionId">The unique identifier of the solution.</param>
        /// <returns>A list of form entities associated with the solution.</returns>
        public List<Entity> GetFormsForSolution(Guid solutionId)
        {
            // 60 = System Form (main forms, quick view, quick create, etc.)
            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 60), // 60 = System Form
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId)
                    }
                }
            };

            var solutionComponents = _service.RetrieveMultiple(query);
            var forms = new List<Entity>();

            foreach (var component in solutionComponents.Entities)
            {
                var formId = component.GetAttributeValue<Guid>("objectid");
                var form = _service.Retrieve("systemform", formId, new ColumnSet(true));
                forms.Add(form);
            }

            _tracing.Trace($"Found {forms.Count} forms in solution {solutionId}");
            return forms;
        }

        /// <summary>
        /// Inserts or updates a field form location record in the custom table.
        /// </summary>
        /// <param name="location">The form location information to upsert.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="formId">The identifier of the form.</param>
        public void UpsertFieldFormLocation(FieldFormLocation location, string fieldName, string formId)
        {
            try
            {
                var entity = new Entity("ljr_fieldformlocation");
                entity["ljr_fieldname"] = fieldName;
                entity["ljr_formid"] = formId;
                entity["ljr_formname"] = location.FormName;
                entity["ljr_tabname"] = location.TabName;
                entity["ljr_tabvisible"] = location.TabVisible;
                entity["ljr_sectionname"] = location.SectionName;
                entity["ljr_sectionvisible"] = location.SectionVisible;
                entity["ljr_fieldvisible"] = location.FieldVisible;
                entity["ljr_fielddescription"] = location.FieldDescription;
                entity["ljr_requiredlevel"] = location.RequiredLevel;
                entity["ljr_permissions"] = location.Permissions;
                entity["ljr_canread"] = location.CanRead;
                entity["ljr_canwrite"] = location.CanWrite;
                entity["ljr_cancreate"] = location.CanCreate;

                var query = new QueryExpression("ljr_fieldformlocation")
                {
                    ColumnSet = new ColumnSet("ljr_fieldformlocationid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("ljr_fieldname", ConditionOperator.Equal, fieldName),
                            new ConditionExpression("ljr_formid", ConditionOperator.Equal, formId)
                        }
                    }
                };
                var results = _service.RetrieveMultiple(query);
                if (results.Entities.Count > 0)
                {
                    entity.Id = results.Entities[0].Id;
                    _service.Update(entity);
                    _tracing.Trace($"Updated FieldFormLocation for field {fieldName} on form {formId}");
                }
                else
                {
                    _service.Create(entity);
                    _tracing.Trace($"Created FieldFormLocation for field {fieldName} on form {formId}");
                }
            }
            catch (Exception ex)
            {
                _tracing.Trace($"Exception in UpsertFieldFormLocation for field {fieldName} on form {formId}: {ex}");
            }
        }
    }

    public class WebResourceInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Path { get; set; }
    }
}