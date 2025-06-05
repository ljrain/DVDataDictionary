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

                    foreach (var field in fieldMetadatas)
                    {
                        if (!allFieldMetadatas.Any(f => f.EntityName == field.EntityName && f.SchemaName == field.SchemaName))
                            allFieldMetadatas.Add(field);
                    }

                    var webResources = GetWebResourcesInSolution(service, currentSolutionId, tracingService);
                    tracingService.Trace($"DataDictionaryPlugin: Web resources found in '{solutionName}': {webResources.Count}");

                    foreach (var wr in webResources)
                    {
                        if (!allWebResources.Any(w => w.Name == wr.Name))
                            allWebResources.Add(wr);
                    }
                }

                tracingService.Trace($"DataDictionaryPlugin: Total unique entities: {allEntityMetadatas.Count}");
                tracingService.Trace($"DataDictionaryPlugin: Total unique fields: {allFieldMetadatas.Count}");
                tracingService.Trace($"DataDictionaryPlugin: Total unique web resources: {allWebResources.Count}");

                foreach (var entity in allEntityMetadatas)
                {
                    tracingService.Trace($"DataDictionaryPlugin: Inspecting form fields for entity: {entity.LogicalName}");
                    var allLocations = FormFieldInspector.GetAllFieldsWithVisibility(service, entity.LogicalName)
                        .Cast<FieldFormLocation>();

                    foreach (var field in allFieldMetadatas.Where(f => f.EntityName == entity.LogicalName))
                    {
                        field.FormLocations = allLocations
                            .Where(loc => loc.FieldName == field.SchemaName)
                            .Select(loc => new FieldFormLocation
                            {
                                FormName = loc.FormName,
                                TabName = loc.TabName,
                                TabVisible = loc.TabVisible,
                                SectionName = loc.SectionName,
                                SectionVisible = loc.SectionVisible,
                                FieldVisible = loc.FieldVisible,
                                FieldName = loc.FieldName,
                                FieldDescription = loc.FieldDescription
                            })
                            .ToList();
                    }
                }

                tracingService.Trace("DataDictionaryPlugin: Analyzing scripts.");
                var scriptReferences = AnalyzeScripts(allFieldMetadatas, allWebResources, tracingService);
                tracingService.Trace($"DataDictionaryPlugin: Script analysis complete. ScriptReferences count: {scriptReferences.Count}");

                tracingService.Trace("DataDictionaryPlugin: Generating JSON document.");
                var docBytes = GenerateJsonDocument(allFieldMetadatas, scriptReferences);
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
                    var fieldMetadata = new FieldMetadata
                    {
                        EntityName = entityMetadata.LogicalName,
                        SchemaName = attribute.SchemaName,
                        DisplayName = attribute.DisplayName?.UserLocalizedLabel?.Label,
                        Description = attribute.Description?.UserLocalizedLabel?.Label
                    };

                    fieldMetadatas.Add(fieldMetadata);
                }
            }

            return fieldMetadatas;
        }

        private List<WebResourceInfo> GetWebResourcesInSolution(IOrganizationService service, Guid solutionId, ITracingService tracingService)
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

            var webResources = new List<WebResourceInfo>();
            foreach (var component in solutionComponents.Entities)
            {
                var webResourceId = component.GetAttributeValue<Guid>("objectid");
                try
                {
                    var webResource = service.Retrieve("webresource", webResourceId, new ColumnSet("name", "displayname"));
                    webResources.Add(new WebResourceInfo
                    {
                        Path = webResourceId.ToString(),
                        Name = webResource.GetAttributeValue<string>("name"),
                        DisplayName = webResource.GetAttributeValue<string>("displayname")
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

        private byte[] GenerateJsonDocument(List<FieldMetadata> fieldMetadatas, List<string> scriptReferences)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(DataDictionaryDocument));
                var document = new DataDictionaryDocument
                {
                    Fields = fieldMetadatas,
                    ScriptReferences = scriptReferences
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
                // Write CSV header
                writer.WriteLine("EntityName,SchemaName,DisplayName,Type,RequiredLevel,Description,MaxLength,Precision,MinValue,MaxValue,HiddenByScript,ScriptReferences");

                // Write CSV rows for each field metadata
                foreach (var field in fieldMetadatas)
                {
                    var scriptRefs = field.ScriptReferences != null ? string.Join(";", field.ScriptReferences) : string.Empty;
                    writer.WriteLine($"{field.EntityName},{field.SchemaName},{field.DisplayName},{field.Type},{field.RequiredLevel},{field.Description},{field.MaxLength},{field.Precision},{field.MinValue},{field.MaxValue},{field.HiddenByScript},{scriptRefs}");
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
        // Other methods remain unchanged...
    }

    public class DataDictionaryDocument
    {
        public List<FieldMetadata> Fields { get; set; }
        public List<string> ScriptReferences { get; set; }
        public string AdditionalProperty { get; set; }
    }
}