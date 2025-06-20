using System;

namespace DataDictionary.Models
{
    public class DataDictionaryAttributeMetadata
    {
        public string Table { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ColumnDisplay { get; set; }
        public string ColumnLogical { get; set; }
        public string ColumnSchema { get; set; }
        public string DataType { get; set; }
        public string FormulaDefinition { get; set; }
        public bool IsCustom { get; set; }
        public bool AuditEnabled { get; set; }
        public bool IsCalculated { get; set; }
        public bool IsFormula { get; set; }
        public string LookupTo { get; set; }
        public Int64? MaxLength { get; set; }
        public Int64? MinValue { get; set; }
        public Int64? MaxValue { get; set; }
        public Int32? Precision { get; set; }
        public string OptionSet { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public Int32? LangCode { get; set; }
        public string AttributeOf { get; set; }
        public string AttributeType { get; set; }
        public string AttributeTypeName { get; set; }
        public string AutoNumberFormat { get; set; }
        public bool? CanBeSecuredForCreate { get; set; }
        public bool? CanBeSecuredForRead { get; set; }
        public bool? CanBeSecuredForUpdate { get; set; }
        public bool? CanModifiedAdditionalSettings { get; set; }
        public int? ColumnNumber { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string DeprecatedVersion { get; set; }
        public string DisplayName { get; set; }
        public string EntityLogicalName { get; set; }
        public string ExtensionData { get; set; }
        public string ExternalName { get; set; }
        public bool? HasChanged { get; set; }
        public string InheritsFrom { get; set; }
        public string IntroducedVersion { get; set; }
        public bool? IsAuditEnabled { get; set; }
        public bool? IsCustomAttribute { get; set; }
        public bool? IsCustomizable { get; set; }
        public bool? IsDataSourceSecret { get; set; }
        public bool? IsFilterable { get; set; }
        public bool? IsGlobalFilterEnabled { get; set; }
        public bool? IsLogical { get; set; }
        public bool? IsManaged { get; set; }
        public bool? IsPrimaryId { get; set; }
        public bool? IsPrimaryName { get; set; }
        public bool? IsRenamable { get; set; }
        public bool? IsRequiredForForm { get; set; }
        public bool? IsRetrievable { get; set; }
        public bool? IsSearchable { get; set; }
        public bool? IsSecured { get; set; }
        public bool? IsSortableEnabled { get; set; }
        public bool? IsValidForAdvancedFind { get; set; }
        public bool? IsValidForCreate { get; set; }
        public bool? IsValidForForm { get; set; }
        public bool? IsValidForGrid { get; set; }
        public bool? IsValidForRead { get; set; }
        public bool? IsValidForUpdate { get; set; }
        public bool? IsValidODataAttribute { get; set; }
        public string LikedAttributeId { get; set; }
        public string LogicalName { get; set; }
        public string MetadataId { get; set; }
        public string RequiredLevel { get; set; }
        public string SchemaName { get; set; }

        /// <summary>
        /// Indicates if this field is hidden by JavaScript
        /// </summary>
        public bool? IsHiddenByScript { get; set; }

        /// <summary>
        /// Indicates if this field is marked required by JavaScript
        /// </summary>
        public bool? IsRequiredByScript { get; set; }

        /// <summary>
        /// Indicates if this field has a default value set by JavaScript
        /// </summary>
        public bool? HasDefaultValueByScript { get; set; }

        /// <summary>
        /// The default value set by JavaScript (if any)
        /// </summary>
        public string ScriptDefaultValue { get; set; }

        /// <summary>
        /// List of web resources that modify this field
        /// </summary>
        public string ModifyingWebResources { get; set; }
    }
}