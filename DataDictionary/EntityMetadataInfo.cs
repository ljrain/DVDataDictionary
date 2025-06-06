using System;
using System.Collections.Generic;

namespace DataDictionary
{
    /// <summary>
    /// Represents comprehensive metadata for a Dataverse entity
    /// </summary>
    public class EntityMetadataInfo
    {
        /// <summary>
        /// Logical name of the entity
        /// </summary>
        public string LogicalName { get; set; }

        /// <summary>
        /// Schema name of the entity
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Display name of the entity
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Plural display name of the entity
        /// </summary>
        public string DisplayCollectionName { get; set; }

        /// <summary>
        /// Description of the entity
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Entity set name for OData operations
        /// </summary>
        public string EntitySetName { get; set; }

        /// <summary>
        /// Logical collection name
        /// </summary>
        public string LogicalCollectionName { get; set; }

        /// <summary>
        /// Primary ID attribute name
        /// </summary>
        public string PrimaryIdAttribute { get; set; }

        /// <summary>
        /// Primary name attribute name
        /// </summary>
        public string PrimaryNameAttribute { get; set; }

        /// <summary>
        /// Primary image attribute name
        /// </summary>
        public string PrimaryImageAttribute { get; set; }

        /// <summary>
        /// Object type code
        /// </summary>
        public int ObjectTypeCode { get; set; }

        /// <summary>
        /// Entity metadata ID
        /// </summary>
        public Guid MetadataId { get; set; }

        /// <summary>
        /// Ownership type (User, Organization, etc.)
        /// </summary>
        public string OwnershipType { get; set; }

        /// <summary>
        /// Whether the entity is custom
        /// </summary>
        public bool IsCustomEntity { get; set; }

        /// <summary>
        /// Whether the entity is customizable
        /// </summary>
        public bool IsCustomizable { get; set; }

        /// <summary>
        /// Whether the entity is managed
        /// </summary>
        public bool IsManaged { get; set; }

        /// <summary>
        /// Whether the entity is an activity
        /// </summary>
        public bool IsActivity { get; set; }

        /// <summary>
        /// Whether the entity is an activity party
        /// </summary>
        public bool IsActivityParty { get; set; }

        /// <summary>
        /// Whether audit is enabled
        /// </summary>
        public bool IsAuditEnabled { get; set; }

        /// <summary>
        /// Whether the entity can be in custom entity associations
        /// </summary>
        public bool CanBeInCustomEntityAssociation { get; set; }

        /// <summary>
        /// Whether the entity can be in many-to-many relationships
        /// </summary>
        public bool CanBeInManyToMany { get; set; }

        /// <summary>
        /// Whether the entity can be the primary entity in relationships
        /// </summary>
        public bool CanBePrimaryEntityInRelationship { get; set; }

        /// <summary>
        /// Whether the entity can be the related entity in relationships
        /// </summary>
        public bool CanBeRelatedEntityInRelationship { get; set; }

        /// <summary>
        /// Whether the entity can create attributes
        /// </summary>
        public bool CanCreateAttributes { get; set; }

        /// <summary>
        /// Whether the entity can create charts
        /// </summary>
        public bool CanCreateCharts { get; set; }

        /// <summary>
        /// Whether the entity can create forms
        /// </summary>
        public bool CanCreateForms { get; set; }

        /// <summary>
        /// Whether the entity can create views
        /// </summary>
        public bool CanCreateViews { get; set; }

        /// <summary>
        /// Whether the entity can modify additional settings
        /// </summary>
        public bool CanModifyAdditionalSettings { get; set; }

        /// <summary>
        /// Whether the entity can trigger workflows
        /// </summary>
        public bool CanTriggerWorkflow { get; set; }

        /// <summary>
        /// Whether the entity has activities
        /// </summary>
        public bool HasActivities { get; set; }

        /// <summary>
        /// Whether the entity has changed
        /// </summary>
        public bool HasChanged { get; set; }

        /// <summary>
        /// Whether the entity has feedback
        /// </summary>
        public bool HasFeedback { get; set; }

        /// <summary>
        /// Whether the entity has notes
        /// </summary>
        public bool HasNotes { get; set; }

        /// <summary>
        /// Whether business process flows are enabled
        /// </summary>
        public bool IsBusinessProcessEnabled { get; set; }

        /// <summary>
        /// Whether connections are enabled
        /// </summary>
        public bool IsConnectionsEnabled { get; set; }

        /// <summary>
        /// Whether document management is enabled
        /// </summary>
        public bool IsDocumentManagementEnabled { get; set; }

        /// <summary>
        /// Whether duplicate detection is enabled
        /// </summary>
        public bool IsDuplicateDetectionEnabled { get; set; }

        /// <summary>
        /// Whether the entity is enabled for charts
        /// </summary>
        public bool IsEnabledForCharts { get; set; }

        /// <summary>
        /// Whether the entity is enabled for external channels
        /// </summary>
        public bool IsEnabledForExternalChannels { get; set; }

        /// <summary>
        /// Whether the entity is enabled for tracing
        /// </summary>
        public bool IsEnabledForTrace { get; set; }

        /// <summary>
        /// Whether the entity is importable
        /// </summary>
        public bool IsImportable { get; set; }

        /// <summary>
        /// Whether the entity is an intersection entity
        /// </summary>
        public bool IsIntersect { get; set; }

        /// <summary>
        /// Whether knowledge management is enabled
        /// </summary>
        public bool IsKnowledgeManagementEnabled { get; set; }

        /// <summary>
        /// Whether mail merge is enabled
        /// </summary>
        public bool IsMailMergeEnabled { get; set; }

        /// <summary>
        /// Whether the entity is mappable
        /// </summary>
        public bool IsMappable { get; set; }

        /// <summary>
        /// Whether OneNote integration is enabled
        /// </summary>
        public bool IsOneNoteIntegrationEnabled { get; set; }

        /// <summary>
        /// Whether optimistic concurrency is enabled
        /// </summary>
        public bool IsOptimisticConcurrencyEnabled { get; set; }

        /// <summary>
        /// Whether quick create is enabled
        /// </summary>
        public bool IsQuickCreateEnabled { get; set; }

        /// <summary>
        /// Whether the reading pane is enabled
        /// </summary>
        public bool IsReadingPaneEnabled { get; set; }

        /// <summary>
        /// Whether the entity is renameable
        /// </summary>
        public bool IsRenameable { get; set; }

        /// <summary>
        /// Whether SLA is enabled
        /// </summary>
        public bool IsSLAEnabled { get; set; }

        /// <summary>
        /// Whether the entity is solution aware
        /// </summary>
        public bool IsSolutionAware { get; set; }

        /// <summary>
        /// Whether state model is aware
        /// </summary>
        public bool IsStateModelAware { get; set; }

        /// <summary>
        /// Whether the entity is valid for advanced find
        /// </summary>
        public bool IsValidForAdvancedFind { get; set; }

        /// <summary>
        /// Whether the entity is valid for queue
        /// </summary>
        public bool IsValidForQueue { get; set; }

        /// <summary>
        /// Whether the entity is visible in mobile
        /// </summary>
        public bool IsVisibleInMobile { get; set; }

        /// <summary>
        /// Whether the entity is visible in mobile client
        /// </summary>
        public bool IsVisibleInMobileClient { get; set; }

        /// <summary>
        /// Icon name for large size
        /// </summary>
        public string IconLargeName { get; set; }

        /// <summary>
        /// Icon name for medium size
        /// </summary>
        public string IconMediumName { get; set; }

        /// <summary>
        /// Icon name for small size
        /// </summary>
        public string IconSmallName { get; set; }

        /// <summary>
        /// Icon name for vector
        /// </summary>
        public string IconVectorName { get; set; }

        /// <summary>
        /// Entity color
        /// </summary>
        public string EntityColor { get; set; }

        /// <summary>
        /// Entity help URL
        /// </summary>
        public string EntityHelpUrl { get; set; }

        /// <summary>
        /// Whether entity help URL is enabled
        /// </summary>
        public bool EntityHelpUrlEnabled { get; set; }

        /// <summary>
        /// Introduced version
        /// </summary>
        public string IntroducedVersion { get; set; }

        /// <summary>
        /// External collection name
        /// </summary>
        public string ExternalCollectionName { get; set; }

        /// <summary>
        /// External name
        /// </summary>
        public string ExternalName { get; set; }

        /// <summary>
        /// Data provider ID
        /// </summary>
        public Guid? DataProviderId { get; set; }

        /// <summary>
        /// Data source ID
        /// </summary>
        public Guid? DataSourceId { get; set; }

        /// <summary>
        /// Days since record last modified
        /// </summary>
        public int DaysSinceRecordLastModified { get; set; }

        /// <summary>
        /// Whether change tracking is enabled
        /// </summary>
        public bool ChangeTrackingEnabled { get; set; }

        /// <summary>
        /// Activity type mask for activity entities
        /// </summary>
        public int ActivityTypeMask { get; set; }

        /// <summary>
        /// Auto route to owner queue
        /// </summary>
        public bool AutoRouteToOwnerQueue { get; set; }

        /// <summary>
        /// Auto create access teams
        /// </summary>
        public bool AutoCreateAccessTeams { get; set; }

        /// <summary>
        /// Enforce state transitions
        /// </summary>
        public bool EnforceStateTransitions { get; set; }

        /// <summary>
        /// Uses business data label table
        /// </summary>
        public bool UsesBusinessDataLabelTable { get; set; }

        /// <summary>
        /// Sync to external search index
        /// </summary>
        public bool SyncToExternalSearchIndex { get; set; }

        /// <summary>
        /// Recurrence base entity logical name
        /// </summary>
        public string RecurrenceBaseEntityLogicalName { get; set; }

        /// <summary>
        /// Report view name
        /// </summary>
        public string ReportViewName { get; set; }

        /// <summary>
        /// Collection of field metadata for this entity
        /// </summary>
        public List<FieldMetadata> Fields { get; set; }

        /// <summary>
        /// Collection of one-to-many relationships
        /// </summary>
        public List<RelationshipMetadata> OneToManyRelationships { get; set; }

        /// <summary>
        /// Collection of many-to-one relationships
        /// </summary>
        public List<RelationshipMetadata> ManyToOneRelationships { get; set; }

        /// <summary>
        /// Collection of many-to-many relationships
        /// </summary>
        public List<RelationshipMetadata> ManyToManyRelationships { get; set; }

        /// <summary>
        /// Collection of entity keys
        /// </summary>
        public List<EntityKeyMetadata> Keys { get; set; }

        /// <summary>
        /// Collection of security privileges
        /// </summary>
        public List<SecurityPrivilegeMetadata> Privileges { get; set; }
    }

    /// <summary>
    /// Represents relationship metadata
    /// </summary>
    public class RelationshipMetadata
    {
        /// <summary>
        /// Schema name of the relationship
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Type of relationship
        /// </summary>
        public string RelationshipType { get; set; }

        /// <summary>
        /// Referenced entity
        /// </summary>
        public string ReferencedEntity { get; set; }

        /// <summary>
        /// Referenced attribute
        /// </summary>
        public string ReferencedAttribute { get; set; }

        /// <summary>
        /// Referencing entity
        /// </summary>
        public string ReferencingEntity { get; set; }

        /// <summary>
        /// Referencing attribute
        /// </summary>
        public string ReferencingAttribute { get; set; }

        /// <summary>
        /// Whether the relationship is customizable
        /// </summary>
        public bool IsCustomizable { get; set; }

        /// <summary>
        /// Whether the relationship is managed
        /// </summary>
        public bool IsManaged { get; set; }

        /// <summary>
        /// Cascade configuration
        /// </summary>
        public string CascadeConfiguration { get; set; }
    }

    /// <summary>
    /// Represents entity key metadata
    /// </summary>
    public class EntityKeyMetadata
    {
        /// <summary>
        /// Schema name of the key
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Display name of the key
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Key attributes
        /// </summary>
        public string[] KeyAttributes { get; set; }

        /// <summary>
        /// Whether the key is managed
        /// </summary>
        public bool IsManaged { get; set; }
    }

    /// <summary>
    /// Represents security privilege metadata
    /// </summary>
    public class SecurityPrivilegeMetadata
    {
        /// <summary>
        /// Name of the privilege
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of privilege
        /// </summary>
        public string PrivilegeType { get; set; }

        /// <summary>
        /// Whether privilege can be basic
        /// </summary>
        public bool CanBeBasic { get; set; }

        /// <summary>
        /// Whether privilege can be deep
        /// </summary>
        public bool CanBeDeep { get; set; }

        /// <summary>
        /// Whether privilege can be global
        /// </summary>
        public bool CanBeGlobal { get; set; }

        /// <summary>
        /// Whether privilege can be local
        /// </summary>
        public bool CanBeLocal { get; set; }
    }
}