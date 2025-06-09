using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataDictionary
{
    /// <summary>
    /// Plug-in to generate a data dictionary for a specified Dataverse solution.
    /// Retrieves entities, fields, and JavaScript web resources, analyzes script references,
    /// and outputs the result as a JSON or CSV file attached to a Note.
    /// </summary>
    public class DataDictionaryPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            try
            {
                tracingService.Trace("DataDictionaryPlugin: Entered Execute method.");

                var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                tracingService.Trace("DataDictionaryPlugin: Retrieved IPluginExecutionContext.");

                var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                tracingService.Trace("DataDictionaryPlugin: Retrieved IOrganizationServiceFactory.");

                var service = serviceFactory.CreateOrganizationService(context.UserId);
                tracingService.Trace($"DataDictionaryPlugin: Created OrganizationService for UserId: {context.UserId}.");

                string[] solutionNames = null;
                if (context.InputParameters.Contains("SolutionNames"))
                {
                    var param = context.InputParameters["SolutionNames"];
                    tracingService.Trace($"DataDictionaryPlugin: SolutionNames parameter found, type: {param?.GetType().FullName}");
                    if (param is string[])
                    {
                        solutionNames = (string[])param;
                    }
                    else if (param is string)
                    {
                        solutionNames = ((string)param)
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToArray();
                    }
                }
                else
                {
                    tracingService.Trace("DataDictionaryPlugin: SolutionNames parameter not found in InputParameters.");
                }

                if (solutionNames == null || solutionNames.Length == 0)
                {
                    tracingService.Trace("DataDictionaryPlugin: No solution names provided. Exiting execution.");
                    return;
                }

                tracingService.Trace($"DataDictionaryPlugin: SolutionNames: {string.Join(", ", solutionNames)}");

                var allEntityMetadatas = new List<EntityMetadata>();
                var allFieldMetadatas = new List<FieldMetadata>();
                var allWebResources = new List<WebResourceInfo>();

                foreach (var solutionName in solutionNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    tracingService.Trace($"DataDictionaryPlugin: Processing solution '{solutionName}'.");
                    var currentSolutionId = GetSolutionId(service, solutionName, tracingService);
                    tracingService.Trace($"DataDictionaryPlugin: SolutionId for '{solutionName}': {currentSolutionId}");

                    var entityMetadatas = GetEntitiesInSolution(service, currentSolutionId, tracingService);
                    tracingService.Trace($"DataDictionaryPlugin: Entities found in '{solutionName}': {entityMetadatas.Count}");

                    foreach (var entity in entityMetadatas)
                    {
                        if (!allEntityMetadatas.Any(e => e.LogicalName == entity.LogicalName))
                            allEntityMetadatas.Add(entity);
                    }

                    var fieldMetadatas = GetFieldsInSolution(service, currentSolutionId, entityMetadatas, tracingService);
                    tracingService.Trace($"DataDictionaryPlugin: Fields found in '{solutionName}': {fieldMetadatas.Count}");

                    // After collecting fieldMetadatas in each solution, add the solution name to each field:
                    foreach (var field in fieldMetadatas)
                    {
                        if (field.SolutionNames == null)
                            field.SolutionNames = new List<string>();
                        if (!field.SolutionNames.Contains(solutionName))
                            field.SolutionNames.Add(solutionName);

                        // Merge with allFieldMetadatas if already present
                        var existing = allFieldMetadatas
                            .FirstOrDefault(f => f.EntityName == field.EntityName && f.SchemaName == field.SchemaName);
                        if (existing != null)
                        {
                            foreach (var sol in field.SolutionNames)
                                if (!existing.SolutionNames.Contains(sol))
                                    existing.SolutionNames.Add(sol);
                        }
                        else
                        {
                            allFieldMetadatas.Add(field);
                        }
                    }

                    var webResources = GetWebResourcesInSolution(service, currentSolutionId, tracingService);
                    tracingService.Trace($"DataDictionaryPlugin: Web resources found in '{solutionName}': {webResources.Count}");

                    foreach (var wr in webResources)
                    {
                        allWebResources.Add(new WebResourceInfo
                        {
                            Name = wr.Name,
                            DisplayName = wr.DisplayName,
                            Path = wr.Path
                        });
                    }
                }

                GetFormsForSolution(service, allEntityMetadatas, allFieldMetadatas, allWebResources, tracingService);


                tracingService.Trace($"DataDictionaryPlugin: Total unique entities: {allEntityMetadatas.Count}");
                tracingService.Trace($"DataDictionaryPlugin: Total unique fields: {allFieldMetadatas.Count}");
                tracingService.Trace($"DataDictionaryPlugin: Total unique web resources: {allWebResources.Count}");

                foreach (var entity in allEntityMetadatas)
                {
                    tracingService.Trace($"DataDictionaryPlugin: Inspecting forms for entity: {entity.LogicalName}");

                    // Get all form field locations for this entity
                    var allLocations = FormFieldInspector.GetAllFieldsWithVisibility(service, entity.LogicalName)
                        .Cast<FieldFormLocation>()
                        .ToList();

                    // Group locations by field name for efficient lookup
                    var locationsByField = allLocations
                        .GroupBy(loc => loc.FieldName)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var field in allFieldMetadatas.Where(f => f.EntityName == entity.LogicalName))
                    {
                        // Find all locations for this field on all forms
                        if (locationsByField.TryGetValue(field.SchemaName, out var fieldLocations))
                        {
                            // Ensure all associated forms are listed
                            field.FormLocations = fieldLocations
                                .Select(loc => new FieldFormLocation
                                {
                                    FormName = loc.FormName,
                                    TabName = loc.TabName,
                                    TabVisible = loc.TabVisible,
                                    SectionName = loc.SectionName,
                                    SectionVisible = loc.SectionVisible,
                                    FieldVisible = loc.FieldVisible,
                                    FieldName = loc.FieldName,
                                    FieldDescription = loc.FieldDescription,
                                    RequiredLevel = field.RequiredLevel,
                                    Permissions = field.Permissions,
                                    CanRead = loc.CanRead,
                                    CanWrite = loc.CanWrite,
                                    CanCreate = loc.CanCreate
                                })
                                .ToList();
                        }
                        else
                        {
                            field.FormLocations = new List<FieldFormLocation>();
                        }

                        if (!field.FormLocations.Any())
                        {
                            tracingService.Trace($"No form locations found for field {field.SchemaName} on entity {entity.LogicalName}");
                        }
                        else
                        {
                            tracingService.Trace($"Form locations for field {field.SchemaName} on entity {entity.LogicalName}: {string.Join(";", field.FormLocations.Select(f => f.FormName))}");
                        }
                    }
                }

                tracingService.Trace("DataDictionaryPlugin: Analyzing scripts.");
                var scriptReferences = AnalyzeScripts(allFieldMetadatas, allWebResources, tracingService);
                tracingService.Trace($"DataDictionaryPlugin: Script analysis complete. ScriptReferences count: {scriptReferences.Count}");

                tracingService.Trace("DataDictionaryPlugin: Generating JSON document.");
                var docBytes = GenerateJsonDocument(allFieldMetadatas, scriptReferences, solutionNames.ToList());
                tracingService.Trace($"DataDictionaryPlugin: JSON document generated. Size: {docBytes?.Length ?? 0} bytes.");

                tracingService.Trace("DataDictionaryPlugin: Generating CSV document.");
                var csvBytes = GenerateCsvDocument(allFieldMetadatas, scriptReferences);
                tracingService.Trace($"DataDictionaryPlugin: CSV document generated. Size: {csvBytes?.Length ?? 0} bytes.");

                tracingService.Trace("DataDictionaryPlugin: Storing JSON document as Note.");
                var noteId = StoreDocumentAsNote(service, docBytes, "DataDictionary.json", "Data Dictionary generated by plug-in.", tracingService);
                tracingService.Trace($"DataDictionaryPlugin: JSON NoteId: {noteId}");

                tracingService.Trace("DataDictionaryPlugin: Storing CSV document as Note.");
                var csvNoteId = StoreDocumentAsNote(service, csvBytes, "DataDictionary.csv", "Data Dictionary CSV generated by plug-in.", tracingService);
                tracingService.Trace($"DataDictionaryPlugin: CSV NoteId: {csvNoteId}");

                tracingService.Trace("DataDictionaryPlugin: Storing Data Dictionary in Dataverse tables.");
                StoreDataDictionaryInDataverse(service, allEntityMetadatas, allFieldMetadatas, allWebResources, scriptReferences, solutionNames.ToList(), tracingService);
                tracingService.Trace("DataDictionaryPlugin: Data Dictionary stored in Dataverse tables.");

                context.OutputParameters["NoteId"] = noteId;
                context.OutputParameters["CsvNoteId"] = csvNoteId;

                tracingService.Trace("DataDictionaryPlugin: Execution completed successfully.");
            }
            catch (Exception ex)
            {
                tracingService?.Trace($"DataDictionaryPlugin: Exception: {ex}");
                throw new InvalidPluginExecutionException("An error occurred in DataDictionaryPlugin: " + ex.Message, ex);
            }
        }

        private Guid GetSolutionId(IOrganizationService service, string solutionName, ITracingService tracingService)
        {
            tracingService.Trace($"Retrieving SolutionId for solution: {solutionName}");

            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, solutionName)
                    }
                }
            };

            var solutions = service.RetrieveMultiple(query);

            if (solutions.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException($"Solution '{solutionName}' not found.");
            }

            return solutions.Entities[0].Id;
        }

        private List<EntityMetadata> GetEntitiesInSolution(IOrganizationService service, Guid solutionId, ITracingService tracingService)
        {
            tracingService.Trace($"Retrieving entities for solutionId: {solutionId}");

            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 1), // 1 represents Entity
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId)
                    }
                }
            };

            var solutionComponents = service.RetrieveMultiple(query);

            var entityMetadatas = new List<EntityMetadata>();
            foreach (var component in solutionComponents.Entities)
            {
                var entityId = component.GetAttributeValue<Guid>("objectid");
                // Use MetadataId instead of LogicalName
                var retrieveEntityRequest = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Attributes,
                    MetadataId = entityId
                };

                var retrieveEntityResponse = (RetrieveEntityResponse)service.Execute(retrieveEntityRequest);
                entityMetadatas.Add(retrieveEntityResponse.EntityMetadata);
            }

            return entityMetadatas;
        }

        private List<FieldMetadata> GetFieldsInSolution(IOrganizationService service, Guid solutionId, List<EntityMetadata> entityMetadatas, ITracingService tracingService)
        {
            tracingService.Trace($"Retrieving fields for solutionId: {solutionId}");

            var fieldMetadatas = new List<FieldMetadata>();

            foreach (var entityMetadata in entityMetadatas)
            {
                tracingService.Trace($"Retrieving fields for entity: {entityMetadata.LogicalName}");

                var retrieveEntityRequest = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Attributes,
                    LogicalName = entityMetadata.LogicalName
                };

                var retrieveEntityResponse = (RetrieveEntityResponse)service.Execute(retrieveEntityRequest);

                foreach (var attribute in retrieveEntityResponse.EntityMetadata.Attributes)
                {
                    tracingService.Trace($"Processing field : {attribute.LogicalName}");
                    if (attribute.LogicalName == "exchangerate")
                        continue;

                    // Try to get additional metadata
                    int? maxLength = null;
                    int? precision = null;
                    int? minValue = null;
                    int? maxValue = null;

                    // String length
                    if (attribute is StringAttributeMetadata stringAttr)
                    {
                        maxLength = stringAttr.MaxLength;
                    }
                    // Decimal precision
                    if (attribute is DecimalAttributeMetadata decimalAttr)
                    {
                        precision = decimalAttr.Precision;
                        // Fix for CS0266: Explicitly cast 'long?' to 'int?' where necessary.
                        minValue = (int?)(decimalAttr.MinValue.HasValue ? (Int64?)(decimalAttr.MinValue.Value) : null);
                        maxValue = (int?)(decimalAttr.MaxValue.HasValue ? (Int64?)(decimalAttr.MaxValue.Value) : null);
                        // Fix for CS0266: Explicitly cast 'long?' to 'int?' where necessary.
                        if (decimalAttr.MinValue.HasValue)
                        {
                            minValue = (int?)decimalAttr.MinValue.Value;
                        }
                        else
                        {
                            minValue = null;
                        }

                        if (decimalAttr.MaxValue.HasValue)
                        {
                            maxValue = (int?)decimalAttr.MaxValue.Value;
                        }
                        else
                        {
                            maxValue = null;
                        }
                        minValue = (int?)(decimalAttr.MinValue.HasValue ? (Int64?)decimal.ToInt64(decimalAttr.MinValue.Value) : null);
                        maxValue = (int?)(decimalAttr.MaxValue.HasValue ? (Int64?)decimal.ToInt64(decimalAttr.MaxValue.Value) : null);
                    }
                    // Double precision
                    if (attribute is DoubleAttributeMetadata doubleAttr)
                    {
                        precision = doubleAttr.Precision;
                        minValue = doubleAttr.MinValue.HasValue ? (int?)doubleAttr.MinValue.Value : null;
                        maxValue = doubleAttr.MaxValue.HasValue ? (int?)doubleAttr.MaxValue.Value : null;
                    }
                    // Integer min/max
                    if (attribute is IntegerAttributeMetadata intAttr)
                    {
                        minValue = intAttr.MinValue;
                        maxValue = intAttr.MaxValue;
                    }
                    // Money precision
                    if (attribute is MoneyAttributeMetadata moneyAttr)
                    {
                        precision = moneyAttr.Precision;
                        minValue = (int?)(moneyAttr.MinValue.HasValue ? (Int64?)Convert.ToInt64(moneyAttr.MinValue.Value) : null);
                        maxValue = (int?)(moneyAttr.MaxValue.HasValue ? (Int64?)Convert.ToInt64(moneyAttr.MaxValue.Value) : null);
                    }

                    // Required level
                    string requiredLevel = null;
                    if (attribute.RequiredLevel != null)
                    {
                        requiredLevel = attribute.RequiredLevel.Value.ToString();
                    }

                    // Permissions (not available directly, placeholder)
                    string permissions = null;

                    var fieldMetadata = new FieldMetadata
                    {
                        EntityName = entityMetadata.LogicalName,
                        SchemaName = attribute.SchemaName,
                        DisplayName = attribute.DisplayName?.UserLocalizedLabel?.Label,
                        Description = attribute.Description?.UserLocalizedLabel?.Label,
                        Type = attribute.AttributeTypeName?.Value,
                        RequiredLevel = requiredLevel,
                        MaxLength = maxLength,
                        Precision = precision,
                        MinValue = minValue,
                        MaxValue = maxValue,
                        Permissions = permissions
                    };

                    fieldMetadatas.Add(fieldMetadata);
                }
            }

            return fieldMetadatas;
        }

        // Update the `GetWebResourcesInSolution` method to ensure the returned `WebResourceInfo` matches the expected type.

        private List<WebResourceInfoNew> GetWebResourcesInSolution(IOrganizationService service, Guid solutionId, ITracingService tracingService)
        {
            tracingService.Trace($"Retrieving web resources for solutionId: {solutionId}");

            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 61), // 61 represents Web Resource
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId)
                    }
                }
            };

            var solutionComponents = service.RetrieveMultiple(query);

            var webResources = new List<WebResourceInfoNew>();
            foreach (var component in solutionComponents.Entities)
            {
                var webResourceId = component.GetAttributeValue<Guid>("objectid");
                try
                {
                    var webResource = service.Retrieve("webresource", webResourceId, new ColumnSet("name", "displayname", "webresourcetype"));
                    webResources.Add(new WebResourceInfoNew
                    {
                        Path = webResourceId.ToString(),
                        Name = webResource.GetAttributeValue<string>("name"),
                        DisplayName = webResource.GetAttributeValue<string>("displayname"),
                        WebResourceType = webResource.GetAttributeValue<OptionSetValue>("webresourcetype")?.Value ?? 0,
                        Guid = webResourceId.ToString()
                    });
                }
                catch (Exception ex)
                {
                    tracingService.Trace($"Web resource with Id {webResourceId} could not be retrieved: {ex.Message}");
                    // Continue processing other web resources
                }
            }

            return webResources;
        }

        private List<string> AnalyzeScripts(List<FieldMetadata> fieldMetadatas, List<WebResourceInfo> webResources, ITracingService tracingService)
        {
            tracingService.Trace("Analyzing scripts for field metadata and web resources.");

            var scriptReferences = new List<string>();

            foreach (var field in fieldMetadatas)
            {
                if (field.ScriptReferences == null)
                {
                    field.ScriptReferences = new List<string>();
                }

                foreach (var webResource in webResources)
                {
                    if (webResource.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        if (field.SchemaName != null && webResource.Name.IndexOf(field.SchemaName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            field.ScriptReferences.Add(webResource.Name);
                            scriptReferences.Add(webResource.Name);
                        }
                    }
                }

                field.HiddenByScript = field.ScriptReferences.Any();
            }

            tracingService.Trace($"Script analysis completed. Total script references found: {scriptReferences.Count}");
            return scriptReferences;
        }

        private byte[] GenerateJsonDocument(List<FieldMetadata> fieldMetadatas, List<string> scriptReferences, List<string> solutionNames)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(DataDictionaryRoot));
                var document = new DataDictionaryRoot
                {
                    DataDictionary = new DataDictionaryDocument
                    {
                        Fields = fieldMetadatas,
                        ScriptReferences = scriptReferences,
                        SolutionNames = solutionNames
                    }
                };
                serializer.WriteObject(memoryStream, document);
                return memoryStream.ToArray();
            }
        }

        private byte[] GenerateCsvDocument(List<FieldMetadata> fieldMetadatas, List<string> scriptReferences)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream))
            {
                // Add FormNames and SolutionNames to the header
                writer.WriteLine("EntityName,SchemaName,DisplayName,Type,RequiredLevel,Description,MaxLength,Precision,MinValue,MaxValue,HiddenByScript,ScriptReferences,FormNames,SolutionNames");

                foreach (var field in fieldMetadatas)
                {
                    var scriptRefs = field.ScriptReferences != null ? string.Join(";", field.ScriptReferences) : string.Empty;
                    var formNames = field.FormLocations != null && field.FormLocations.Any()
                        ? string.Join(";", field.FormLocations.Select(f => f.FormName).Distinct())
                        : string.Empty;
                    var solutionNames = field.SolutionNames != null && field.SolutionNames.Any()
                        ? string.Join(";", field.SolutionNames.Distinct())
                        : string.Empty;

                    writer.WriteLine($"{field.EntityName},{field.SchemaName},{field.DisplayName},{field.Type},{field.RequiredLevel},{field.Description},{field.MaxLength},{field.Precision},{field.MinValue},{field.MaxValue},{field.HiddenByScript},{scriptRefs},{formNames},{solutionNames}");
                }

                writer.Flush();
                return memoryStream.ToArray();
            }
        }

        private Guid StoreDocumentAsNote(IOrganizationService service, byte[] documentBytes, string fileName, string description, ITracingService tracingService)
        {
            tracingService.Trace($"Storing document as Note. FileName: {fileName}, Description: {description}");

            var note = new Entity("annotation")
            {
                ["subject"] = fileName,
                ["notetext"] = description,
                ["documentbody"] = Convert.ToBase64String(documentBytes),
                ["filename"] = fileName,
                ["mimetype"] = "application/octet-stream"
            };

            var noteId = service.Create(note);
            tracingService.Trace($"Note created with ID: {noteId}");
            return noteId;
        }

        public void StoreDataDictionaryInDataverse(
            IOrganizationService service,
            List<EntityMetadata> allEntityMetadatas,
            List<FieldMetadata> allFieldMetadatas,
            List<WebResourceInfo> allWebResources,
            List<string> scriptReferences,
            List<string> solutionNames,
            ITracingService tracingService)
        {
            tracingService.Trace("Storing Data Dictionary data into Dataverse tables.");

            var helper = new DataverseDataHelper(service, tracingService);

            Dictionary<string, string[]> entityRecords = new Dictionary<string, string[]>();

            // Store Entities
            foreach (var entity in allEntityMetadatas)
            {
                helper.UpsertEntityRecord(
                    entity.LogicalName,
                    entity.DisplayName?.UserLocalizedLabel?.Label,
                    entity.Description?.UserLocalizedLabel?.Label
                );
                entityRecords.Add(entity.LogicalName, entity.PrimaryKey);
            }

            // Store Fields
            foreach (var field in allFieldMetadatas)
            {
                helper.UpsertFieldRecord(field,entityRecords);
                foreach (var formloc in field.FormLocations)
                {
                    helper.UpsertFieldFormLocation(formloc, formloc.FieldName, formloc.FormName);

                }
            }

            // Store Web Resources
            foreach (var wr in allWebResources)
            {
                helper.UpsertWebResourceRecord(wr);
            }

            // Store Script References
            foreach (var script in scriptReferences.Distinct())
            {
                helper.UpsertScriptReference(script);
            }

            



            tracingService.Trace("Data Dictionary data stored in Dataverse tables.");
        }

        public string GenerateJsonDocument(object data)
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, // Ignore nulls
                Formatting = Newtonsoft.Json.Formatting.Indented, // Pretty print
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() // camelCase
            };
            // Wrap the data in the DataDictionary parent
            var root = new { DataDictionary = data };
            return Newtonsoft.Json.JsonConvert.SerializeObject(root, settings);
        }
        private void GetFormsForSolution(
            IOrganizationService service,
            List<EntityMetadata> allEntityMetadatas,
            List<FieldMetadata> allFieldMetadatas,
            List<WebResourceInfo> allWebResources,
            ITracingService tracingService)
        {
            tracingService.Trace("Retrieving forms for solution.");

            foreach (var entity in allEntityMetadatas)
            {
                tracingService.Trace($"Processing forms for entity: {entity.LogicalName}");

                // Retrieve forms for the entity
                var query = new QueryExpression("systemform")
                {
                    ColumnSet = new ColumnSet("name", "formxml"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("objecttypecode", ConditionOperator.Equal, entity.LogicalName),
                            new ConditionExpression("type", ConditionOperator.In, new[] { 2, 7 }) // Main and Quick Create forms
                        }
                    }
                };

                var forms = service.RetrieveMultiple(query);

                foreach (var form in forms.Entities)
                {
                    var formName = form.GetAttributeValue<string>("name");
                    var formXml = form.GetAttributeValue<string>("formxml");

                    tracingService.Trace($"Processing form: {formName}");

                    // Parse form XML to extract field locations
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(formXml);

                    var tabs = xmlDoc.SelectNodes("//tab");
                    if (tabs != null)
                    {
                        foreach (XmlNode tab in tabs)
                        {
                            var tabName = tab.Attributes?["name"]?.Value;
                            var tabVisible = tab.Attributes?["visible"]?.Value == "true";

                            var sections = tab.SelectNodes("columns/column/sections/section");
                            if (sections != null)
                            {
                                foreach (XmlNode section in sections)
                                {
                                    var sectionName = section.Attributes?["name"]?.Value;
                                    var sectionVisible = section.Attributes?["visible"]?.Value == "true";

                                    var fields = section.SelectNodes("rows/row/cell/control");
                                    if (fields != null)
                                    {
                                        foreach (XmlNode field in fields)
                                        {
                                            var fieldName = field.Attributes?["datafieldname"]?.Value;
                                            if (!string.IsNullOrEmpty(fieldName))
                                            {
                                                var fieldMetadata = allFieldMetadatas.FirstOrDefault(f => f.EntityName == entity.LogicalName && f.SchemaName == fieldName);
                                                if (fieldMetadata != null)
                                                {
                                                    if (fieldMetadata.FormLocations == null)
                                                    {
                                                        fieldMetadata.FormLocations = new List<FieldFormLocation>();
                                                    }

                                                    fieldMetadata.FormLocations.Add(new FieldFormLocation
                                                    {
                                                        FormName = formName,
                                                        TabName = tabName,
                                                        TabVisible = tabVisible,
                                                        SectionName = sectionName,
                                                        SectionVisible = sectionVisible,
                                                        FieldVisible = true,
                                                        FieldName = fieldName
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            tracingService.Trace("Forms retrieval and processing completed.");
        }
        // Other methods remain unchanged...
    }

    public class DataDictionaryRoot
    {
        public DataDictionaryDocument DataDictionary { get; set; }
    }

    public class DataDictionaryDocument
    {
        public List<FieldMetadata> Fields { get; set; }
        public List<string> ScriptReferences { get; set; }
        public List<string> SolutionNames { get; set; } // Add this property
    }

    public class FieldFormLocation
    {
        public string FormName { get; set; }
        public string TabName { get; set; }
        public bool TabVisible { get; set; }
        public string SectionName { get; set; }
        public bool SectionVisible { get; set; }
        public bool FieldVisible { get; set; }
        public string FieldName { get; set; }
        public string FieldDescription { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanCreate { get; set; }
        public string RequiredLevel { get; set; }
        public string Permissions { get; set; }
    }

    public class FieldMetadata
    {
        public string EntityName { get; set; }
        public string SchemaName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string RequiredLevel { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public List<FieldFormLocation> FormLocations { get; set; }
        public List<string> ScriptReferences { get; set; }
        public bool HiddenByScript { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanCreate { get; set; }
        public string Permissions { get; set; }
        public List<string> SolutionNames { get; set; }

        public override string ToString()
        {
            var formLocations = FormLocations != null && FormLocations.Count > 0
                ? string.Join(" | ", FormLocations.Select(f =>
                    $"FormName: {f.FormName}, TabName: {f.TabName}, TabVisible: {f.TabVisible}, SectionName: {f.SectionName}, SectionVisible: {f.SectionVisible}, FieldVisible: {f.FieldVisible}, FieldName: {f.FieldName}, FieldDescription: {f.FieldDescription}, RequiredLevel: {f.RequiredLevel}, Permissions: {f.Permissions}, CanRead: {f.CanRead}, CanWrite: {f.CanWrite}, CanCreate: {f.CanCreate}"))
                : "None";

            var scriptRefs = ScriptReferences != null && ScriptReferences.Count > 0
                ? string.Join(";", ScriptReferences)
                : "None";

            var solutionNames = SolutionNames != null && SolutionNames.Count > 0
                ? string.Join(";", SolutionNames)
                : "None";

            return $"EntityName: {EntityName}, " +
                   $"SchemaName: {SchemaName}, " +
                   $"DisplayName: {DisplayName}, " +
                   $"Description: {Description}, " +
                   $"Type: {Type}, " +
                   $"RequiredLevel: {RequiredLevel}, " +
                   $"MaxLength: {MaxLength}, " +
                   $"Precision: {Precision}, " +
                   $"MinValue: {MinValue}, " +
                   $"MaxValue: {MaxValue}, " +
                   $"HiddenByScript: {HiddenByScript}, " +
                   $"CanRead: {CanRead}, " +
                   $"CanWrite: {CanWrite}, " +
                   $"CanCreate: {CanCreate}, " +
                   $"Permissions: {Permissions}, " +
                   $"ScriptReferences: [{scriptRefs}], " +
                   $"FormLocations: [{formLocations}], " +
                   $"SolutionNames: [{solutionNames}]";
        }
    }
    // Fix for CS0229: Ambiguity between 'WebResourceInfo.Name' and 'WebResourceInfo.Name'
    // The issue arises because the `WebResourceInfo` class has duplicate property definitions for `Name` and `DisplayName`.
    // To resolve this, remove the duplicate properties from the `WebResourceInfo` class.

    public class WebResourceInfoNew
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Path { get; set; }
        public int WebResourceType { get; set; }
        public string Guid { get; set; } // Add this property to uniquely identify the web resource
    }
}