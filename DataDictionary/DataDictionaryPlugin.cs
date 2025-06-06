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
                    var allLocations = FormFieldInspector.GetAllFieldsWithVisibility(service, entity.LogicalName);

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

                var scriptReferences = AnalyzeScripts(allFieldMetadatas, allWebResources, tracingService);

                // Create comprehensive entity metadata information
                var entityInfoList = CreateEntityMetadataInfoList(allEntityMetadatas, allFieldMetadatas);

                var docBytes = GenerateJsonDocument(allFieldMetadatas, scriptReferences, entityInfoList);
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
        private byte[] GenerateJsonDocument(List<FieldMetadata> fieldMetadatas, Dictionary<string, List<string>> scriptReferences, List<EntityMetadataInfo> entityInfoList)
        {
            try
            {
                var dataDictionary = new
                {
                    DVDictionary = new
                    {
                        Metadata = new
                        {
                            GeneratedOn = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            Version = "2.0",
                            Description = "Comprehensive data dictionary with enhanced metadata for Power FX compatibility"
                        },
                        Entities = entityInfoList.Select(entity => new
                        {
                            // Entity-level metadata
                            entity.LogicalName,
                            entity.SchemaName,
                            entity.DisplayName,
                            entity.DisplayCollectionName,
                            entity.Description,
                            entity.EntitySetName,
                            entity.LogicalCollectionName,
                            entity.PrimaryIdAttribute,
                            entity.PrimaryNameAttribute,
                            entity.PrimaryImageAttribute,
                            entity.ObjectTypeCode,
                            entity.MetadataId,
                            entity.OwnershipType,
                            entity.IsCustomEntity,
                            entity.IsCustomizable,
                            entity.IsManaged,
                            entity.IsActivity,
                            entity.IsActivityParty,
                            entity.IsAuditEnabled,
                            entity.CanBeInCustomEntityAssociation,
                            entity.CanBeInManyToMany,
                            entity.CanBePrimaryEntityInRelationship,
                            entity.CanBeRelatedEntityInRelationship,
                            entity.CanCreateAttributes,
                            entity.CanCreateCharts,
                            entity.CanCreateForms,
                            entity.CanCreateViews,
                            entity.CanModifyAdditionalSettings,
                            entity.CanTriggerWorkflow,
                            entity.HasActivities,
                            entity.HasChanged,
                            entity.HasFeedback,
                            entity.HasNotes,
                            entity.IsBusinessProcessEnabled,
                            entity.IsConnectionsEnabled,
                            entity.IsDocumentManagementEnabled,
                            entity.IsDuplicateDetectionEnabled,
                            entity.IsEnabledForCharts,
                            entity.IsEnabledForExternalChannels,
                            entity.IsEnabledForTrace,
                            entity.IsImportable,
                            entity.IsIntersect,
                            entity.IsKnowledgeManagementEnabled,
                            entity.IsMailMergeEnabled,
                            entity.IsMappable,
                            entity.IsOneNoteIntegrationEnabled,
                            entity.IsOptimisticConcurrencyEnabled,
                            entity.IsQuickCreateEnabled,
                            entity.IsReadingPaneEnabled,
                            entity.IsRenameable,
                            entity.IsSLAEnabled,
                            entity.IsSolutionAware,
                            entity.IsStateModelAware,
                            entity.IsValidForAdvancedFind,
                            entity.IsValidForQueue,
                            entity.IsVisibleInMobile,
                            entity.IsVisibleInMobileClient,
                            entity.IconLargeName,
                            entity.IconMediumName,
                            entity.IconSmallName,
                            entity.IconVectorName,
                            entity.EntityColor,
                            entity.EntityHelpUrl,
                            entity.EntityHelpUrlEnabled,
                            entity.IntroducedVersion,
                            entity.ExternalCollectionName,
                            entity.ExternalName,
                            entity.DataProviderId,
                            entity.DataSourceId,
                            entity.DaysSinceRecordLastModified,
                            entity.ChangeTrackingEnabled,
                            entity.ActivityTypeMask,
                            entity.AutoRouteToOwnerQueue,
                            entity.AutoCreateAccessTeams,
                            entity.EnforceStateTransitions,
                            entity.UsesBusinessDataLabelTable,
                            entity.SyncToExternalSearchIndex,
                            entity.RecurrenceBaseEntityLogicalName,
                            entity.ReportViewName,

                            // Relationships
                            OneToManyRelationships = entity.OneToManyRelationships?.Select(r => new
                            {
                                r.SchemaName,
                                r.RelationshipType,
                                r.ReferencedEntity,
                                r.ReferencedAttribute,
                                r.ReferencingEntity,
                                r.ReferencingAttribute,
                                r.IsCustomizable,
                                r.IsManaged,
                                r.CascadeConfiguration
                            }),
                            ManyToOneRelationships = entity.ManyToOneRelationships?.Select(r => new
                            {
                                r.SchemaName,
                                r.RelationshipType,
                                r.ReferencedEntity,
                                r.ReferencedAttribute,
                                r.ReferencingEntity,
                                r.ReferencingAttribute,
                                r.IsCustomizable,
                                r.IsManaged,
                                r.CascadeConfiguration
                            }),
                            ManyToManyRelationships = entity.ManyToManyRelationships?.Select(r => new
                            {
                                r.SchemaName,
                                r.RelationshipType,
                                r.ReferencedEntity,
                                r.ReferencedAttribute,
                                r.ReferencingEntity,
                                r.ReferencingAttribute,
                                r.IsCustomizable,
                                r.IsManaged
                            }),

                            // Keys and Privileges
                            Keys = entity.Keys?.Select(k => new
                            {
                                k.SchemaName,
                                k.DisplayName,
                                k.KeyAttributes,
                                k.IsManaged
                            }),
                            Privileges = entity.Privileges?.Select(p => new
                            {
                                p.Name,
                                p.PrivilegeType,
                                p.CanBeBasic,
                                p.CanBeDeep,
                                p.CanBeGlobal,
                                p.CanBeLocal
                            }),

                            // Field metadata
                            Fields = entity.Fields.Select(f => new
                            {
                                // Basic Properties
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

                                // Enhanced Properties
                                f.AttributeOf,
                                f.AttributeTypeName,
                                f.CanBeSecuredForCreate,
                                f.CanBeSecuredForRead,
                                f.CanBeSecuredForUpdate,
                                f.ColumnNumber,
                                f.DeprecatedVersion,
                                f.HasChanged,
                                f.InheritsFrom,
                                f.IntroducedVersion,
                                f.IsCustomAttribute,
                                f.IsDataSourceSecret,
                                f.IsFilterable,
                                f.IsLogical,
                                f.IsManaged,
                                f.IsPrimaryId,
                                f.IsPrimaryName,
                                f.IsRequiredForForm,
                                f.IsRetrievable,
                                f.IsSearchable,
                                f.IsSecured,
                                f.IsValidForCreate,
                                f.IsValidForForm,
                                f.IsValidForGrid,
                                f.IsValidForRead,
                                f.IsValidForUpdate,
                                f.IsValidODataAttribute,
                                f.LinkedAttributeId,
                                f.MetadataId,
                                f.SourceType,
                                f.AutoNumberFormat,
                                f.DatabaseLength,
                                f.Format,
                                f.Formula,
                                f.RollupState,
                                f.ExternalName,
                                f.DefaultValue,
                                f.Targets,
                                f.RelationshipName,

                                // Option Set Information
                                OptionSet = f.OptionSet?.Select(o => new
                                {
                                    o.Value,
                                    o.Label,
                                    o.Description,
                                    o.Color,
                                    o.IsDefault
                                }),

                                // Form and Script Information
                                FormLocations = f.FormLocations?.Select(fl => new
                                {
                                    fl.FormName,
                                    fl.TabName,
                                    fl.TabVisible,
                                    fl.SectionName,
                                    fl.SectionVisible,
                                    fl.FieldVisible,
                                    fl.FieldName,
                                    fl.FieldDescription
                                }),
                                f.ScriptReferences,
                                f.HiddenByScript
                            }).ToArray()
                        }).ToArray(),
                        ScriptReferences = scriptReferences,
                        Summary = new
                        {
                            TotalEntities = entityInfoList.Count,
                            TotalFields = fieldMetadatas.Count,
                            CustomEntities = entityInfoList.Count(e => e.IsCustomEntity),
                            ManagedEntities = entityInfoList.Count(e => e.IsManaged),
                            CustomFields = fieldMetadatas.Count(f => f.IsCustomAttribute),
                            ManagedFields = fieldMetadatas.Count(f => f.IsManaged),
                            CalculatedFields = fieldMetadatas.Count(f => f.SourceType == "Calculated"),
                            RollupFields = fieldMetadatas.Count(f => f.SourceType == "Rollup"),
                            SecuredFields = fieldMetadatas.Count(f => f.IsSecured),
                            FieldsWithScripts = fieldMetadatas.Count(f => f.ScriptReferences?.Any() == true),
                            TotalRelationships = entityInfoList.Sum(e => 
                                (e.OneToManyRelationships?.Count ?? 0) +
                                (e.ManyToOneRelationships?.Count ?? 0) +
                                (e.ManyToManyRelationships?.Count ?? 0)),
                            TotalKeys = entityInfoList.Sum(e => e.Keys?.Count ?? 0)
                        }
                    }
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
                        EntityFilters = EntityFilters.All, // Get all metadata including relationships, keys, etc.
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

        private List<EntityMetadataInfo> CreateEntityMetadataInfoList(List<EntityMetadata> entityMetadatas, List<FieldMetadata> fieldMetadatas)
        {
            var entityInfoList = new List<EntityMetadataInfo>();

            foreach (var emd in entityMetadatas)
            {
                var entityInfo = new EntityMetadataInfo
                {
                    LogicalName = emd.LogicalName,
                    SchemaName = emd.SchemaName,
                    DisplayName = emd.DisplayName?.UserLocalizedLabel?.Label,
                    DisplayCollectionName = emd.DisplayCollectionName?.UserLocalizedLabel?.Label,
                    Description = emd.Description?.UserLocalizedLabel?.Label,
                    EntitySetName = emd.EntitySetName,
                    LogicalCollectionName = emd.LogicalCollectionName,
                    PrimaryIdAttribute = emd.PrimaryIdAttribute,
                    PrimaryNameAttribute = emd.PrimaryNameAttribute,
                    PrimaryImageAttribute = emd.PrimaryImageAttribute,
                    ObjectTypeCode = emd.ObjectTypeCode?.Value ?? 0,
                    MetadataId = emd.MetadataId?.Value ?? Guid.Empty,
                    OwnershipType = emd.OwnershipType?.ToString(),
                    IsCustomEntity = emd.IsCustomEntity?.Value ?? false,
                    IsCustomizable = emd.IsCustomizable?.Value ?? false,
                    IsManaged = emd.IsManaged?.Value ?? false,
                    IsActivity = emd.IsActivity?.Value ?? false,
                    IsActivityParty = emd.IsActivityParty?.Value ?? false,
                    IsAuditEnabled = emd.IsAuditEnabled?.Value ?? false,
                    CanBeInCustomEntityAssociation = emd.CanBeInCustomEntityAssociation?.Value ?? false,
                    CanBeInManyToMany = emd.CanBeInManyToMany?.Value ?? false,
                    CanBePrimaryEntityInRelationship = emd.CanBePrimaryEntityInRelationship?.Value ?? false,
                    CanBeRelatedEntityInRelationship = emd.CanBeRelatedEntityInRelationship?.Value ?? false,
                    CanCreateAttributes = emd.CanCreateAttributes?.Value ?? false,
                    CanCreateCharts = emd.CanCreateCharts?.Value ?? false,
                    CanCreateForms = emd.CanCreateForms?.Value ?? false,
                    CanCreateViews = emd.CanCreateViews?.Value ?? false,
                    CanModifyAdditionalSettings = emd.CanModifyAdditionalSettings?.Value ?? false,
                    CanTriggerWorkflow = emd.CanTriggerWorkflow?.Value ?? false,
                    HasActivities = emd.HasActivities?.Value ?? false,
                    HasChanged = emd.HasChanged?.Value ?? false,
                    HasFeedback = emd.HasFeedback?.Value ?? false,
                    HasNotes = emd.HasNotes?.Value ?? false,
                    IsBusinessProcessEnabled = emd.IsBusinessProcessEnabled?.Value ?? false,
                    IsConnectionsEnabled = emd.IsConnectionsEnabled?.Value ?? false,
                    IsDocumentManagementEnabled = emd.IsDocumentManagementEnabled?.Value ?? false,
                    IsDuplicateDetectionEnabled = emd.IsDuplicateDetectionEnabled?.Value ?? false,
                    IsEnabledForCharts = emd.IsEnabledForCharts?.Value ?? false,
                    IsEnabledForExternalChannels = emd.IsEnabledForExternalChannels?.Value ?? false,
                    IsEnabledForTrace = emd.IsEnabledForTrace?.Value ?? false,
                    IsImportable = emd.IsImportable?.Value ?? false,
                    IsIntersect = emd.IsIntersect?.Value ?? false,
                    IsKnowledgeManagementEnabled = emd.IsKnowledgeManagementEnabled?.Value ?? false,
                    IsMailMergeEnabled = emd.IsMailMergeEnabled?.Value ?? false,
                    IsMappable = emd.IsMappable?.Value ?? false,
                    IsOneNoteIntegrationEnabled = emd.IsOneNoteIntegrationEnabled?.Value ?? false,
                    IsOptimisticConcurrencyEnabled = emd.IsOptimisticConcurrencyEnabled?.Value ?? false,
                    IsQuickCreateEnabled = emd.IsQuickCreateEnabled?.Value ?? false,
                    IsReadingPaneEnabled = emd.IsReadingPaneEnabled?.Value ?? false,
                    IsRenameable = emd.IsRenameable?.Value ?? false,
                    IsSLAEnabled = emd.IsSLAEnabled?.Value ?? false,
                    IsSolutionAware = emd.IsSolutionAware ?? false,
                    IsStateModelAware = emd.IsStateModelAware?.Value ?? false,
                    IsValidForAdvancedFind = emd.IsValidForAdvancedFind?.Value ?? false,
                    IsValidForQueue = emd.IsValidForQueue?.Value ?? false,
                    IsVisibleInMobile = emd.IsVisibleInMobile?.Value ?? false,
                    IsVisibleInMobileClient = emd.IsVisibleInMobileClient?.Value ?? false,
                    IconLargeName = emd.IconLargeName,
                    IconMediumName = emd.IconMediumName,
                    IconSmallName = emd.IconSmallName,
                    IconVectorName = emd.IconVectorName,
                    EntityColor = emd.EntityColor,
                    EntityHelpUrl = emd.EntityHelpUrl,
                    EntityHelpUrlEnabled = emd.EntityHelpUrlEnabled?.Value ?? false,
                    IntroducedVersion = emd.IntroducedVersion,
                    ExternalCollectionName = emd.ExternalCollectionName,
                    ExternalName = emd.ExternalName,
                    DataProviderId = emd.DataProviderId,
                    DataSourceId = emd.DataSourceId,
                    DaysSinceRecordLastModified = emd.DaysSinceRecordLastModified ?? -1,
                    ChangeTrackingEnabled = emd.ChangeTrackingEnabled?.Value ?? false,
                    ActivityTypeMask = emd.ActivityTypeMask?.Value ?? 0,
                    AutoRouteToOwnerQueue = emd.AutoRouteToOwnerQueue?.Value ?? false,
                    AutoCreateAccessTeams = emd.AutoCreateAccessTeams?.Value ?? false,
                    EnforceStateTransitions = emd.EnforceStateTransitions?.Value ?? false,
                    UsesBusinessDataLabelTable = emd.UsesBusinessDataLabelTable?.Value ?? false,
                    SyncToExternalSearchIndex = emd.SyncToExternalSearchIndex?.Value ?? false,
                    RecurrenceBaseEntityLogicalName = emd.RecurrenceBaseEntityLogicalName,
                    ReportViewName = emd.ReportViewName,

                    // Set fields for this entity
                    Fields = fieldMetadatas.Where(f => f.EntityName == emd.LogicalName).ToList(),

                    // Convert relationships (basic info only to avoid circular references)
                    OneToManyRelationships = ConvertOneToManyRelationships(emd.OneToManyRelationships),
                    ManyToOneRelationships = ConvertManyToOneRelationships(emd.ManyToOneRelationships),
                    ManyToManyRelationships = ConvertManyToManyRelationships(emd.ManyToManyRelationships),
                    Keys = ConvertEntityKeys(emd.Keys),
                    Privileges = ConvertSecurityPrivileges(emd.Privileges)
                };

                entityInfoList.Add(entityInfo);
            }

            return entityInfoList;
        }

        private List<RelationshipMetadata> ConvertOneToManyRelationships(OneToManyRelationshipMetadata[] relationships)
        {
            if (relationships == null) return new List<RelationshipMetadata>();

            return relationships.Select(r => new RelationshipMetadata
            {
                SchemaName = r.SchemaName,
                RelationshipType = "OneToMany",
                ReferencedEntity = r.ReferencedEntity,
                ReferencedAttribute = r.ReferencedAttribute,
                ReferencingEntity = r.ReferencingEntity,
                ReferencingAttribute = r.ReferencingAttribute,
                IsCustomizable = r.IsCustomizable?.Value ?? false,
                IsManaged = r.IsManaged?.Value ?? false,
                CascadeConfiguration = r.CascadeConfiguration?.ToString() ?? ""
            }).ToList();
        }

        private List<RelationshipMetadata> ConvertManyToOneRelationships(OneToManyRelationshipMetadata[] relationships)
        {
            if (relationships == null) return new List<RelationshipMetadata>();

            return relationships.Select(r => new RelationshipMetadata
            {
                SchemaName = r.SchemaName,
                RelationshipType = "ManyToOne",
                ReferencedEntity = r.ReferencedEntity,
                ReferencedAttribute = r.ReferencedAttribute,
                ReferencingEntity = r.ReferencingEntity,
                ReferencingAttribute = r.ReferencingAttribute,
                IsCustomizable = r.IsCustomizable?.Value ?? false,
                IsManaged = r.IsManaged?.Value ?? false,
                CascadeConfiguration = r.CascadeConfiguration?.ToString() ?? ""
            }).ToList();
        }

        private List<RelationshipMetadata> ConvertManyToManyRelationships(ManyToManyRelationshipMetadata[] relationships)
        {
            if (relationships == null) return new List<RelationshipMetadata>();

            return relationships.Select(r => new RelationshipMetadata
            {
                SchemaName = r.SchemaName,
                RelationshipType = "ManyToMany",
                ReferencedEntity = r.Entity1LogicalName,
                ReferencedAttribute = r.Entity1IntersectAttribute,
                ReferencingEntity = r.Entity2LogicalName,
                ReferencingAttribute = r.Entity2IntersectAttribute,
                IsCustomizable = r.IsCustomizable?.Value ?? false,
                IsManaged = r.IsManaged?.Value ?? false,
                CascadeConfiguration = ""
            }).ToList();
        }

        private List<EntityKeyMetadata> ConvertEntityKeys(EntityKeyMetadata[] keys)
        {
            if (keys == null) return new List<EntityKeyMetadata>();

            return keys.Select(k => new DataDictionary.EntityKeyMetadata
            {
                SchemaName = k.SchemaName,
                DisplayName = k.DisplayName?.UserLocalizedLabel?.Label ?? "",
                KeyAttributes = k.KeyAttributes,
                IsManaged = k.IsManaged?.Value ?? false
            }).ToList();
        }

        private List<SecurityPrivilegeMetadata> ConvertSecurityPrivileges(SecurityPrivilegeMetadata[] privileges)
        {
            if (privileges == null) return new List<SecurityPrivilegeMetadata>();

            return privileges.Select(p => new DataDictionary.SecurityPrivilegeMetadata
            {
                Name = p.Name,
                PrivilegeType = p.PrivilegeType?.ToString() ?? "",
                CanBeBasic = p.CanBeBasic,
                CanBeDeep = p.CanBeDeep,
                CanBeGlobal = p.CanBeGlobal,
                CanBeLocal = p.CanBeLocal
            }).ToList();
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
                            MaxValue = attribute.MaxValue,

                            // Enhanced metadata properties
                            AttributeOf = attribute.AttributeOf,
                            AttributeTypeName = attribute.AttributeTypeName?.Value,
                            CanBeSecuredForCreate = attribute.CanBeSecuredForCreate?.Value ?? false,
                            CanBeSecuredForRead = attribute.CanBeSecuredForRead?.Value ?? false,
                            CanBeSecuredForUpdate = attribute.CanBeSecuredForUpdate?.Value ?? false,
                            ColumnNumber = attribute.ColumnNumber?.Value,
                            DeprecatedVersion = attribute.DeprecatedVersion,
                            HasChanged = attribute.HasChanged?.Value ?? false,
                            InheritsFrom = attribute.InheritsFrom,
                            IntroducedVersion = attribute.IntroducedVersion,
                            IsCustomAttribute = attribute.IsCustomAttribute?.Value ?? false,
                            IsDataSourceSecret = attribute.IsDataSourceSecret?.Value ?? false,
                            IsFilterable = attribute.IsFilterable?.Value ?? false,
                            IsLogical = attribute.IsLogical?.Value ?? false,
                            IsManaged = attribute.IsManaged?.Value ?? false,
                            IsPrimaryId = attribute.IsPrimaryId?.Value ?? false,
                            IsPrimaryName = attribute.IsPrimaryName?.Value ?? false,
                            IsRequiredForForm = attribute.IsRequiredForForm?.Value ?? false,
                            IsRetrievable = attribute.IsRetrievable?.Value ?? false,
                            IsSearchable = attribute.IsSearchable?.Value ?? false,
                            IsSecured = attribute.IsSecured?.Value ?? false,
                            IsValidForCreate = attribute.IsValidForCreate?.Value ?? false,
                            IsValidForForm = attribute.IsValidForForm?.Value ?? false,
                            IsValidForGrid = attribute.IsValidForGrid?.Value ?? false,
                            IsValidForRead = attribute.IsValidForRead?.Value ?? false,
                            IsValidForUpdate = attribute.IsValidForUpdate?.Value ?? false,
                            IsValidODataAttribute = attribute.IsValidODataAttribute,
                            LinkedAttributeId = attribute.LinkedAttributeId?.ToString(),
                            MetadataId = attribute.MetadataId?.ToString(),
                            SourceType = GetSourceType(attribute.SourceType),
                            AutoNumberFormat = GetAutoNumberFormat(attribute),
                            DatabaseLength = GetDatabaseLength(attribute),
                            Format = GetAttributeFormat(attribute),
                            Formula = GetFormula(attribute),
                            RollupState = GetRollupState(attribute),
                            ExternalName = GetExternalName(attribute),
                            OptionSet = GetOptionSetMetadata(attribute),
                            DefaultValue = GetDefaultValue(attribute),
                            Targets = GetTargets(attribute),
                            RelationshipName = GetRelationshipName(attribute)
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

        private string GetSourceType(int? sourceType)
        {
            switch (sourceType ?? 0)
            {
                case 1:
                    return "Calculated";
                case 2:
                    return "Rollup";
                default:
                    return "Standard";
            }
        }

        private string GetAutoNumberFormat(AttributeMetadata attribute)
        {
            try
            {
                var stringAttribute = attribute as StringAttributeMetadata;
                return stringAttribute?.AutoNumberFormat ?? "";
            }
            catch
            {
                return "";
            }
        }

        private int? GetDatabaseLength(AttributeMetadata attribute)
        {
            try
            {
                var stringAttribute = attribute as StringAttributeMetadata;
                return stringAttribute?.DatabaseLength;
            }
            catch
            {
                return null;
            }
        }

        private string GetAttributeFormat(AttributeMetadata attribute)
        {
            try
            {
                // Different attribute types have different format properties
                if (attribute is StringAttributeMetadata stringAttr)
                    return stringAttr.Format?.ToString() ?? "";
                if (attribute is DateTimeAttributeMetadata dateAttr)
                    return dateAttr.Format?.ToString() ?? "";
                if (attribute is IntegerAttributeMetadata intAttr)
                    return intAttr.Format?.ToString() ?? "";
                if (attribute is MoneyAttributeMetadata moneyAttr)
                    return moneyAttr.PrecisionSource?.ToString() ?? "";
                return "";
            }
            catch
            {
                return "";
            }
        }

        private string GetFormula(AttributeMetadata attribute)
        {
            try
            {
                var property = attribute.GetType().GetProperty("FormulaDefinition");
                return property?.GetValue(attribute)?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string GetRollupState(AttributeMetadata attribute)
        {
            try
            {
                var property = attribute.GetType().GetProperty("RollupState");
                return property?.GetValue(attribute)?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string GetExternalName(AttributeMetadata attribute)
        {
            try
            {
                var property = attribute.GetType().GetProperty("ExternalName");
                return property?.GetValue(attribute)?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private List<OptionMetadata> GetOptionSetMetadata(AttributeMetadata attribute)
        {
            try
            {
                var options = new List<OptionMetadata>();

                if (attribute is PicklistAttributeMetadata picklistAttr && picklistAttr.OptionSet?.Options != null)
                {
                    foreach (var option in picklistAttr.OptionSet.Options)
                    {
                        options.Add(new OptionMetadata
                        {
                            Value = option.Value,
                            Label = option.Label?.UserLocalizedLabel?.Label ?? "",
                            Description = option.Description?.UserLocalizedLabel?.Label ?? "",
                            Color = option.Color ?? "",
                            IsDefault = false // Would need additional logic to determine default
                        });
                    }
                }
                else if (attribute is StateAttributeMetadata stateAttr && stateAttr.OptionSet?.Options != null)
                {
                    foreach (var option in stateAttr.OptionSet.Options)
                    {
                        options.Add(new OptionMetadata
                        {
                            Value = option.Value,
                            Label = option.Label?.UserLocalizedLabel?.Label ?? "",
                            Description = option.Description?.UserLocalizedLabel?.Label ?? "",
                            Color = option.Color ?? "",
                            IsDefault = false
                        });
                    }
                }
                else if (attribute is StatusAttributeMetadata statusAttr && statusAttr.OptionSet?.Options != null)
                {
                    foreach (var option in statusAttr.OptionSet.Options)
                    {
                        options.Add(new OptionMetadata
                        {
                            Value = option.Value,
                            Label = option.Label?.UserLocalizedLabel?.Label ?? "",
                            Description = option.Description?.UserLocalizedLabel?.Label ?? "",
                            Color = option.Color ?? "",
                            IsDefault = false
                        });
                    }
                }

                return options;
            }
            catch
            {
                return new List<OptionMetadata>();
            }
        }

        private object GetDefaultValue(AttributeMetadata attribute)
        {
            try
            {
                if (attribute is BooleanAttributeMetadata boolAttr)
                    return boolAttr.DefaultValue;
                if (attribute is IntegerAttributeMetadata intAttr)
                    return intAttr.DefaultValue;
                if (attribute is DecimalAttributeMetadata decAttr)
                    return decAttr.DefaultValue;
                if (attribute is DoubleAttributeMetadata dblAttr)
                    return dblAttr.DefaultValue;
                if (attribute is MoneyAttributeMetadata moneyAttr)
                    return moneyAttr.DefaultValue;
                if (attribute is PicklistAttributeMetadata pickAttr)
                    return pickAttr.DefaultFormValue;
                if (attribute is StringAttributeMetadata strAttr)
                    return strAttr.DefaultValue;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string[] GetTargets(AttributeMetadata attribute)
        {
            try
            {
                if (attribute is LookupAttributeMetadata lookupAttr && lookupAttr.Targets != null)
                    return lookupAttr.Targets;
                return new string[0];
            }
            catch
            {
                return new string[0];
            }
        }

        private string GetRelationshipName(AttributeMetadata attribute)
        {
            try
            {
                if (attribute is LookupAttributeMetadata lookupAttr)
                    return lookupAttr.RelationshipName ?? "";
                return "";
            }
            catch
            {
                return "";
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

        private Dictionary<string, List<string>> AnalyzeScripts(List<FieldMetadata> fieldMetadatas, List<WebResourceInfo> webResources, ITracingService tracingService)
        {
            try
            {
                tracingService.Trace("Starting script analysis...");
                var scriptReferences = new Dictionary<string, List<string>>();

                foreach (var webResource in webResources.Where(wr => wr.Name.EndsWith(".js")))
                {
                    tracingService.Trace($"Analyzing script: {webResource.Name}");
                    
                    if (string.IsNullOrEmpty(webResource.Content))
                        continue;

                    // Decode base64 content
                    var content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(webResource.Content));
                    
                    // Analyze each field to see if it's referenced in this script
                    foreach (var field in fieldMetadatas)
                    {
                        var fieldKey = $"{field.EntityName}.{field.SchemaName}";
                        
                        // Check for various patterns that might reference the field
                        var patterns = new[]
                        {
                            field.SchemaName,
                            $"'{field.SchemaName}'",
                            $"\"{field.SchemaName}\"",
                            $"getAttribute(\"{field.SchemaName}\")",
                            $"getAttribute('{field.SchemaName}')",
                            $"getControl(\"{field.SchemaName}\")",
                            $"getControl('{field.SchemaName}')"
                        };

                        if (patterns.Any(pattern => content.Contains(pattern)))
                        {
                            if (!scriptReferences.ContainsKey(fieldKey))
                                scriptReferences[fieldKey] = new List<string>();
                            
                            scriptReferences[fieldKey].Add(webResource.Name);

                            // Check if field is being hidden by script
                            if (content.Contains($"{field.SchemaName}\").setVisible(false)") ||
                                content.Contains($"{field.SchemaName}').setVisible(false)"))
                            {
                                field.HiddenByScript = true;
                            }
                        }
                    }
                }

                // Update field script references
                foreach (var field in fieldMetadatas)
                {
                    var fieldKey = $"{field.EntityName}.{field.SchemaName}";
                    if (scriptReferences.ContainsKey(fieldKey))
                    {
                        field.ScriptReferences = scriptReferences[fieldKey];
                    }
                    else
                    {
                        field.ScriptReferences = new List<string>();
                    }
                }

                tracingService.Trace($"Script analysis completed. Found references for {scriptReferences.Count} fields.");
                return scriptReferences;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"AnalyzeScripts failed: {ex}");
                throw;
            }
        }

        private byte[] GenerateCsvDocument(List<FieldMetadata> fieldMetadatas, Dictionary<string, List<string>> scriptReferences)
        {
            try
            {
                var csv = new StringBuilder();
                
                // Enhanced CSV Headers with comprehensive metadata
                csv.AppendLine("EntityName,SchemaName,DisplayName,Type,AttributeTypeName,RequiredLevel,Description," +
                              "MaxLength,Precision,MinValue,MaxValue,ColumnNumber,SourceType,IntroducedVersion," +
                              "DeprecatedVersion,IsCustomAttribute,IsManaged,IsPrimaryId,IsPrimaryName," +
                              "IsFilterable,IsSearchable,IsRetrievable,IsSecured,IsValidForCreate,IsValidForRead," +
                              "IsValidForUpdate,IsValidForForm,IsValidForGrid,CanBeSecuredForCreate," +
                              "CanBeSecuredForRead,CanBeSecuredForUpdate,IsLogical,IsDataSourceSecret," +
                              "IsRequiredForForm,IsValidODataAttribute,DefaultValue,Format,AutoNumberFormat," +
                              "DatabaseLength,Formula,RollupState,ExternalName,RelationshipName,Targets," +
                              "OptionSetValues,FormLocations,ScriptReferences,HiddenByScript,LinkedAttributeId," +
                              "MetadataId,AttributeOf,InheritsFrom,HasChanged");
                
                foreach (var field in fieldMetadatas)
                {
                    var formLocations = field.FormLocations?.Any() == true 
                        ? string.Join("; ", field.FormLocations.Select(fl => $"{fl.FormName}:{fl.TabName}:{fl.SectionName}:{fl.FieldVisible}"))
                        : "";
                    
                    var scriptRefs = field.ScriptReferences?.Any() == true 
                        ? string.Join("; ", field.ScriptReferences)
                        : "";

                    var optionSetValues = field.OptionSet?.Any() == true
                        ? string.Join("; ", field.OptionSet.Select(o => $"{o.Value}:{o.Label}"))
                        : "";

                    var targets = field.Targets?.Any() == true
                        ? string.Join("; ", field.Targets)
                        : "";

                    // Escape CSV values
                    csv.AppendLine($"\"{EscapeCsvValue(field.EntityName)}\"," +
                                  $"\"{EscapeCsvValue(field.SchemaName)}\"," +
                                  $"\"{EscapeCsvValue(field.DisplayName)}\"," +
                                  $"\"{EscapeCsvValue(field.Type)}\"," +
                                  $"\"{EscapeCsvValue(field.AttributeTypeName)}\"," +
                                  $"\"{EscapeCsvValue(field.RequiredLevel)}\"," +
                                  $"\"{EscapeCsvValue(field.Description)}\"," +
                                  $"\"{field.MaxLength}\"," +
                                  $"\"{field.Precision}\"," +
                                  $"\"{field.MinValue}\"," +
                                  $"\"{field.MaxValue}\"," +
                                  $"\"{field.ColumnNumber}\"," +
                                  $"\"{EscapeCsvValue(field.SourceType)}\"," +
                                  $"\"{EscapeCsvValue(field.IntroducedVersion)}\"," +
                                  $"\"{EscapeCsvValue(field.DeprecatedVersion)}\"," +
                                  $"\"{field.IsCustomAttribute}\"," +
                                  $"\"{field.IsManaged}\"," +
                                  $"\"{field.IsPrimaryId}\"," +
                                  $"\"{field.IsPrimaryName}\"," +
                                  $"\"{field.IsFilterable}\"," +
                                  $"\"{field.IsSearchable}\"," +
                                  $"\"{field.IsRetrievable}\"," +
                                  $"\"{field.IsSecured}\"," +
                                  $"\"{field.IsValidForCreate}\"," +
                                  $"\"{field.IsValidForRead}\"," +
                                  $"\"{field.IsValidForUpdate}\"," +
                                  $"\"{field.IsValidForForm}\"," +
                                  $"\"{field.IsValidForGrid}\"," +
                                  $"\"{field.CanBeSecuredForCreate}\"," +
                                  $"\"{field.CanBeSecuredForRead}\"," +
                                  $"\"{field.CanBeSecuredForUpdate}\"," +
                                  $"\"{field.IsLogical}\"," +
                                  $"\"{field.IsDataSourceSecret}\"," +
                                  $"\"{field.IsRequiredForForm}\"," +
                                  $"\"{field.IsValidODataAttribute}\"," +
                                  $"\"{EscapeCsvValue(field.DefaultValue?.ToString())}\"," +
                                  $"\"{EscapeCsvValue(field.Format)}\"," +
                                  $"\"{EscapeCsvValue(field.AutoNumberFormat)}\"," +
                                  $"\"{field.DatabaseLength}\"," +
                                  $"\"{EscapeCsvValue(field.Formula)}\"," +
                                  $"\"{EscapeCsvValue(field.RollupState)}\"," +
                                  $"\"{EscapeCsvValue(field.ExternalName)}\"," +
                                  $"\"{EscapeCsvValue(field.RelationshipName)}\"," +
                                  $"\"{EscapeCsvValue(targets)}\"," +
                                  $"\"{EscapeCsvValue(optionSetValues)}\"," +
                                  $"\"{EscapeCsvValue(formLocations)}\"," +
                                  $"\"{EscapeCsvValue(scriptRefs)}\"," +
                                  $"\"{field.HiddenByScript}\"," +
                                  $"\"{EscapeCsvValue(field.LinkedAttributeId)}\"," +
                                  $"\"{EscapeCsvValue(field.MetadataId)}\"," +
                                  $"\"{EscapeCsvValue(field.AttributeOf)}\"," +
                                  $"\"{EscapeCsvValue(field.InheritsFrom)}\"," +
                                  $"\"{field.HasChanged}\"");
                }
                
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error generating CSV document: " + ex.Message, ex);
            }
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
                
            return value.Replace("\"", "\"\"");
        }

        private Guid StoreDocumentAsNote(IOrganizationService service, byte[] documentBytes, string fileName, string description, ITracingService tracingService)
        {
            try
            {
                tracingService.Trace($"Storing document as note: {fileName}");

                // Create the annotation (note) record
                var note = new Entity("annotation");
                note["subject"] = fileName;
                note["notetext"] = description;
                note["filename"] = fileName;
                note["documentbody"] = Convert.ToBase64String(documentBytes);
                note["mimetype"] = fileName.EndsWith(".json") ? "application/json" : "text/csv";

                var noteId = service.Create(note);
                tracingService.Trace($"Document stored successfully. Note ID: {noteId}");
                
                return noteId;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"StoreDocumentAsNote failed: {ex}");
                throw;
            }
        }
    }

    /// <summary>
    /// Helper class for extracting field, tab, and section visibility from form XML.
    /// </summary>
    public static class FormFieldInspector
    {
        internal static IEnumerable<FieldFormLocation> GetAllFieldsWithVisibility(IOrganizationService service, string logicalName)
        {
            var fieldLocations = new List<FieldFormLocation>();
            
            try
            {
                // Retrieve all forms for the entity
                var query = new QueryExpression("systemform")
                {
                    ColumnSet = new ColumnSet("name", "formxml", "type"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("objecttypecode", ConditionOperator.Equal, logicalName),
                            new ConditionExpression("type", ConditionOperator.In, new object[] { 2, 7 }) // Main forms and Quick forms
                        }
                    }
                };

                var forms = service.RetrieveMultiple(query).Entities;

                foreach (var form in forms)
                {
                    var formName = form.GetAttributeValue<string>("name");
                    var formXml = form.GetAttributeValue<string>("formxml");
                    var formType = form.GetAttributeValue<OptionSetValue>("type")?.Value;

                    if (!string.IsNullOrEmpty(formXml))
                    {
                        var locations = ParseFormXmlForFields(formXml, formName, formType);
                        fieldLocations.AddRange(locations);
                    }
                }
            }
            catch (Exception)
            {
                // If form parsing fails, return empty list rather than throw
                // This allows the plugin to continue processing other metadata
            }

            return fieldLocations;
        }

        private static List<FieldFormLocation> ParseFormXmlForFields(string formXml, string formName, int? formType)
        {
            var fieldLocations = new List<FieldFormLocation>();
            
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(formXml);

                // Navigate through tabs and sections to find fields
                var tabs = doc.SelectNodes("//tab");
                if (tabs != null)
                {
                    foreach (XmlNode tab in tabs)
                    {
                        var tabName = GetAttributeValue(tab, "name") ?? GetAttributeValue(tab, "id") ?? "Unknown Tab";
                        var tabVisible = GetBooleanAttribute(tab, "visible", true);

                        var sections = tab.SelectNodes(".//section");
                        if (sections != null)
                        {
                            foreach (XmlNode section in sections)
                            {
                                var sectionName = GetAttributeValue(section, "name") ?? GetAttributeValue(section, "id") ?? "Unknown Section";
                                var sectionVisible = GetBooleanAttribute(section, "visible", true);

                                var controls = section.SelectNodes(".//control");
                                if (controls != null)
                                {
                                    foreach (XmlNode control in controls)
                                    {
                                        var fieldName = GetAttributeValue(control, "datafieldname") ?? GetAttributeValue(control, "id");
                                        var controlClassId = GetAttributeValue(control, "classid");
                                        var fieldVisible = GetBooleanAttribute(control, "visible", true);

                                        // Only include controls that represent actual fields
                                        if (!string.IsNullOrEmpty(fieldName) && IsFieldControl(controlClassId))
                                        {
                                            fieldLocations.Add(new FieldFormLocation
                                            {
                                                FormName = formName,
                                                TabName = tabName,
                                                TabVisible = tabVisible,
                                                SectionName = sectionName,
                                                SectionVisible = sectionVisible,
                                                FieldVisible = fieldVisible,
                                                FieldName = fieldName,
                                                FieldDescription = GetAttributeValue(control, "description") ?? ""
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If XML parsing fails for this form, continue with other forms
            }

            return fieldLocations;
        }

        private static string GetAttributeValue(XmlNode node, string attributeName)
        {
            return node?.Attributes?[attributeName]?.Value;
        }

        private static bool GetBooleanAttribute(XmlNode node, string attributeName, bool defaultValue)
        {
            var value = GetAttributeValue(node, attributeName);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        private static bool IsFieldControl(string classId)
        {
            if (string.IsNullOrEmpty(classId))
                return true; // Assume it's a field control if no classId specified

            // Common field control class IDs
            var fieldControlIds = new[]
            {
                "{4273EDBD-AC1D-40d3-9FB2-095C621B552D}", // Text
                "{5B773807-9FB2-42db-97C3-7A91EFF8ADFF}", // DateTime
                "{B0C6723A-8503-4fd7-BB28-C6D04A569235}", // Picklist
                "{3EF39988-22BB-4f0b-BBBE-64B5A3748AEE}", // Lookup
                "{5546E58C-8EF1-4d63-B0B8-59B10FC67FC5}", // Integer
                "{0D2C745A-E5A8-4c8f-BA63-C6D3BB604660}", // Decimal
                "{533B9E00-756B-4312-95A0-DC888637AC78}", // Money
                "{67FAC785-CD58-4f9f-ABB3-4B7DDC6ED5ED}", // Boolean
                "{F3015350-44A2-4aa0-97B5-6B953E174671}", // Memo
                "{71716B6C-711E-476c-8AB8-5D11542BFB47}"  // Image
            };

            return fieldControlIds.Any(id => id.Equals(classId, StringComparison.OrdinalIgnoreCase));
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
        }
    }
}