using System;
using System.Collections.Generic;

namespace DataDictionary.Models
{
    /// <summary>
    /// Comprehensive metadata model for Dataverse attributes.
    /// Enhanced to match the completeness of the XRM Toolbox MetadataBrowser reference implementation.
    /// </summary>
    public class DataDictionaryAttributeMetadata
    {
        #region Core Properties
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
        #endregion

        #region Extended Metadata Properties (from XRM reference)
        public string AttributeOf { get; set; }
        public string AttributeType { get; set; }
        public string AttributeTypeName { get; set; }
        public string AutoNumberFormat { get; set; }
        
        // Security Properties
        public bool? CanBeSecuredForCreate { get; set; }
        public bool? CanBeSecuredForRead { get; set; }
        public bool? CanBeSecuredForUpdate { get; set; }
        
        // Customization Properties
        public bool? CanModifiedAdditionalSettings { get; set; }
        public bool? IsCustomizable { get; set; }
        public bool? IsRenamable { get; set; }
        
        // System Properties
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
        
        // Validation Properties
        public bool? IsAuditEnabled { get; set; }
        public bool? IsCustomAttribute { get; set; }
        public bool? IsDataSourceSecret { get; set; }
        public bool? IsFilterable { get; set; }
        public bool? IsGlobalFilterEnabled { get; set; }
        public bool? IsLogical { get; set; }
        public bool? IsManaged { get; set; }
        public bool? IsPrimaryId { get; set; }
        public bool? IsPrimaryName { get; set; }
        public bool? IsRequiredForForm { get; set; }
        public bool? IsRetrievable { get; set; }
        public bool? IsSearchable { get; set; }
        public bool? IsSecured { get; set; }
        public bool? IsSortableEnabled { get; set; }
        
        // Usage Properties
        public bool? IsValidForAdvancedFind { get; set; }
        public bool? IsValidForCreate { get; set; }
        public bool? IsValidForForm { get; set; }
        public bool? IsValidForGrid { get; set; }
        public bool? IsValidForRead { get; set; }
        public bool? IsValidForUpdate { get; set; }
        public bool? IsValidODataAttribute { get; set; }
        
        // Reference Properties
        public string LikedAttributeId { get; set; }
        public string LogicalName { get; set; }
        public string MetadataId { get; set; }
        public string RequiredLevel { get; set; }
        public string SchemaName { get; set; }
        #endregion

        #region New Enhanced Properties
        /// <summary>
        /// Source type: Standard, Calculated, or Rollup
        /// </summary>
        public string SourceType { get; set; }
        
        /// <summary>
        /// Collection of option set values for picklist/multi-select attributes
        /// </summary>
        public List<OptionSetMetadata> OptionSetValues { get; set; }
        
        /// <summary>
        /// Target entities for lookup attributes
        /// </summary>
        public List<string> LookupTargets { get; set; }
        
        /// <summary>
        /// Form locations where this attribute appears
        /// </summary>
        public List<FieldFormLocation> FormLocations { get; set; }
        
        /// <summary>
        /// JavaScript references that use this field
        /// </summary>
        public List<string> ScriptReferences { get; set; }
        
        /// <summary>
        /// Solutions containing this attribute
        /// </summary>
        public List<string> SolutionNames { get; set; }
        #endregion

        #region Constructor
        public DataDictionaryAttributeMetadata()
        {
            OptionSetValues = new List<OptionSetMetadata>();
            LookupTargets = new List<string>();
            FormLocations = new List<FieldFormLocation>();
            ScriptReferences = new List<string>();
            SolutionNames = new List<string>();
        }
        #endregion
    }

    /// <summary>
    /// Metadata for option set values
    /// </summary>
    public class OptionSetMetadata
    {
        public int Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public bool IsManaged { get; set; }
        public string ExternalValue { get; set; }
    }

    /// <summary>
    /// Enhanced field form location information
    /// </summary>
    public class FieldFormLocation
    {
        public string FormName { get; set; }
        public string FormId { get; set; }
        public string TabName { get; set; }
        public string SectionName { get; set; }
        public bool IsVisible { get; set; }
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public int? Row { get; set; }
        public int? Column { get; set; }
    }
}