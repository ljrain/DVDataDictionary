using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.ServiceModel;
using System.Runtime.Serialization;
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
                var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                var service = serviceFactory.CreateOrganizationService(context.UserId);

                tracingService.Trace("Starting DataDictionaryPlugin execution.");

                var solutionNames = context.InputParameters.Contains("SolutionNames")
                    ? context.InputParameters["SolutionNames"] as string[]
                    : null;
                if (solutionNames == null || solutionNames.Length == 0 || solutionNames.All(string.IsNullOrWhiteSpace))
                    throw new InvalidPluginExecutionException("SolutionNames parameter is required and must be a non-empty array.");

                tracingService.Trace($"SolutionNames: {string.Join(", ", solutionNames)}");

                var allEntityMetadatas = new List<EntityMetadata>();
                var allFieldMetadatas = new List<FieldMetadata>();
                var allWebResources = new List<WebResourceInfo>();

                foreach (var solutionName in solutionNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    var currentSolutionId = GetSolutionId(service, solutionName, tracingService); // Renamed variable to avoid conflict
                    tracingService.Trace($"SolutionId for '{solutionName}': {currentSolutionId}");

                    var entityMetadatas = GetEntitiesInSolution(service, currentSolutionId, tracingService);
                    tracingService.Trace($"Entities found in '{solutionName}': {entityMetadatas.Count}");

                    foreach (var entity in entityMetadatas)
                    {
                        if (!allEntityMetadatas.Any(e => e.LogicalName == entity.LogicalName))
                            allEntityMetadatas.Add(entity);
                    }

                    var fieldMetadatas = GetFieldsInSolution(service, currentSolutionId, entityMetadatas, tracingService);
                    tracingService.Trace($"Fields found in '{solutionName}': {fieldMetadatas.Count}");

                    foreach (var field in fieldMetadatas)
                    {
                        if (!allFieldMetadatas.Any(f => f.EntityName == field.EntityName && f.SchemaName == field.SchemaName))
                            allFieldMetadatas.Add(field);
                    }

                    var webResources = GetWebResourcesInSolution(service, currentSolutionId, tracingService);
                    tracingService.Trace($"Web resources found in '{solutionName}': {webResources.Count}");

                    foreach (var wr in webResources)
                    {
                        if (!allWebResources.Any(w => w.Name == wr.Name))
                            allWebResources.Add(wr);
                    }
                }

                foreach (var entity in allEntityMetadatas)
                {
                    var allLocations = FormFieldInspector.GetAllFieldsWithVisibility(service, entity.LogicalName)
                        .Cast<FormFieldInspector.FieldFormLocation>(); // Explicitly cast objects to FieldFormLocation

                    foreach (var field in allFieldMetadatas.Where(f => f.EntityName == entity.LogicalName))
                    {
                        field.FormLocations = allLocations
                            .Where(loc => loc.FieldName == field.SchemaName) // FieldName is now accessible after casting
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

                var scriptReferences = AnalyzeScripts(allFieldMetadatas, allWebResources, tracingService);

                var docBytes = GenerateJsonDocument(allFieldMetadatas, scriptReferences);
                var csvBytes = GenerateCsvDocument(allFieldMetadatas, scriptReferences);

                var noteId = StoreDocumentAsNote(service, docBytes, "DataDictionary.json", "Data Dictionary generated by plug-in.", tracingService);
                var csvNoteId = StoreDocumentAsNote(service, csvBytes, "DataDictionary.csv", "Data Dictionary CSV generated by plug-in.", tracingService);

                context.OutputParameters["NoteId"] = noteId;
                context.OutputParameters["CsvNoteId"] = csvNoteId;

                tracingService.Trace("DataDictionaryPlugin execution completed successfully.");
            }
            catch (Exception ex)
            {
                tracingService?.Trace($"Exception: {ex}");
                throw new InvalidPluginExecutionException("An error occurred in DataDictionaryPlugin: " + ex.Message, ex);
            }
        }

        private Guid GetSolutionId(IOrganizationService service, string solutionName, ITracingService tracingService)
        {
            try
            {
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid"),
                    Criteria = { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, solutionName) } }
                };
                var result = service.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (result == null)
                    throw new InvalidPluginExecutionException($"Solution '{solutionName}' not found.");
                return result.Id;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"GetSolutionId failed: {ex}");
                throw;
            }
        }
        private byte[] GenerateJsonDocument(List<FieldMetadata> fieldMetadatas, Dictionary<string, List<string>> scriptReferences)
        {
            try
            {
                var dataDictionary = new
                {
                    Fields = fieldMetadatas.Select(f => new
                    {
                        f.EntityName,
                        f.SchemaName,
                        f.DisplayName,
                        f.Type,
                        f.RequiredLevel,
                        f.Description,
                        f.MaxLength,
                        f.Precision,
                        f.MinValue,
                        f.MaxValue,
                        FormLocations = f.FormLocations?.Select(fl => new
                        {
                            fl.FormName,
                            fl.TabName,
                            fl.TabVisible,
                            fl.SectionName,
                            fl.SectionVisible,
                            fl.FieldVisible,
                            fl.FieldName
                        }),
                        f.ScriptReferences,
                        f.HiddenByScript
                    }),
                    ScriptReferences = scriptReferences
                };

                using (var memoryStream = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(dataDictionary.GetType());
                    serializer.WriteObject(memoryStream, dataDictionary);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error generating JSON document: " + ex.Message, ex);
            }
        }
        private List<EntityMetadata> GetEntitiesInSolution(IOrganizationService service, Guid solutionId, ITracingService tracingService)
        {
            try
            {
                tracingService.Trace($"Retrieving entities for solution ID: {solutionId}");

                var request = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    LogicalName = "solutioncomponent",
                    ColumnSet = new ColumnSet("objectid", "componenttype")
                };

                var query = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid", "componenttype"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                            new ConditionExpression("componenttype", ConditionOperator.Equal, 1) // Entity type
                        }
                    }
                };

                var solutionComponents = service.RetrieveMultiple(query).Entities;

                var entityMetadataList = new List<EntityMetadata>();
                foreach (var component in solutionComponents)
                {
                    var entityId = component.GetAttributeValue<Guid>("objectid");
                    var entityMetadataRequest = new RetrieveEntityRequest
                    {
                        EntityFilters = EntityFilters.All,
                        MetadataId = entityId
                    };

                    var response = (RetrieveEntityResponse)service.Execute(entityMetadataRequest);
                    entityMetadataList.Add(response.EntityMetadata);
                }

                tracingService.Trace($"Retrieved {entityMetadataList.Count} entities for solution ID: {solutionId}");
                return entityMetadataList;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"GetEntitiesInSolution failed: {ex}");
                throw;
            }
        }
        private List<FieldMetadata> GetFieldsInSolution(IOrganizationService service, Guid solutionId, List<EntityMetadata> entityMetadatas, ITracingService tracingService)
        {
            try
            {
                tracingService.Trace($"Retrieving fields for solution ID: {solutionId}");

                var fieldMetadataList = new List<FieldMetadata>();

                foreach (var entityMetadata in entityMetadatas)
                {
                    tracingService.Trace($"Retrieving fields for entity: {entityMetadata.LogicalName}");

                    foreach (var attribute in entityMetadata.Attributes)
                    {
                        var fieldMetadata = new FieldMetadata
                        {
                            EntityName = entityMetadata.LogicalName,
                            SchemaName = attribute.LogicalName,
                            DisplayName = attribute.DisplayName?.UserLocalizedLabel?.Label,
                            Type = attribute.AttributeTypeName?.Value,
                            RequiredLevel = attribute.RequiredLevel?.Value.ToString(),
                            Description = attribute.Description?.UserLocalizedLabel?.Label,
                            MaxLength = attribute.MaxLength,
                            Precision = attribute.Precision,
                            MinValue = attribute.MinValue,
                            MaxValue = attribute.MaxValue
                        };

                        fieldMetadataList.Add(fieldMetadata);
                    }
                }

                tracingService.Trace($"Retrieved {fieldMetadataList.Count} fields for solution ID: {solutionId}");
                return fieldMetadataList;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"GetFieldsInSolution failed: {ex}");
                throw;
            }
        }
        private List<WebResourceInfo> GetWebResourcesInSolution(IOrganizationService service, Guid solutionId, ITracingService tracingService)
        {
            try
            {
                tracingService.Trace($"Retrieving web resources for solution ID: {solutionId}");

                var query = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid", "componenttype"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                            new ConditionExpression("componenttype", ConditionOperator.Equal, 61) // Web resource type
                        }
                    }
                };

                var solutionComponents = service.RetrieveMultiple(query).Entities;

                var webResourceList = new List<WebResourceInfo>();
                foreach (var component in solutionComponents)
                {
                    var webResourceId = component.GetAttributeValue<Guid>("objectid");
                    var webResource = service.Retrieve("webresource", webResourceId, new ColumnSet("name", "displayname", "content"));

                    webResourceList.Add(new WebResourceInfo
                    {
                        Id = webResourceId,
                        Name = webResource.GetAttributeValue<string>("name"),
                        DisplayName = webResource.GetAttributeValue<string>("displayname"),
                        Content = webResource.GetAttributeValue<string>("content")
                    });
                }

                tracingService.Trace($"Retrieved {webResourceList.Count} web resources for solution ID: {solutionId}");
                return webResourceList;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"GetWebResourcesInSolution failed: {ex}");
                throw;
            }
        }
    }

    /// <summary>
    /// Helper class for extracting field, tab, and section visibility from form XML.
    /// </summary>
    public static class FormFieldInspector
    {
        internal static IEnumerable<object> GetAllFieldsWithVisibility(IOrganizationService service, string logicalName)
        {
            throw new NotImplementedException();
        }

        public class FieldFormLocation
        {
            public string FormName { get; set; }
            public string TabName { get; set; }
            public bool TabVisible { get; set; }
            public string SectionName { get; set; }
            public bool SectionVisible { get; set; }
            public bool FieldVisible { get; set; }
            public string FieldName { get; set; } // Added this property to fix CS0117
            public string FieldDescription { get; set; }
        }
    }
}