using System;
using System.Collections.Generic;

namespace DataDictionary.Models
{
    public class DataDictionaryAttributeMetadata
    {
        public Guid? AttributeId { get; set; }
        public string Table { get; set; }
        public string ColumnDisplay { get; set; }
        public string ColumnLogical { get; set; }
        public string ColumnSchema { get; set; }
        public string DataType { get; set; }
        public string Description { get; set; }
        public bool IsCustom { get; set; }
        public bool AuditEnabled { get; set; }
        public int LangCode { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string AttributeOf { get; set; }
        public string AttributeType { get; set; }
        public string DisplayName { get; set; }
        public string EntityLogicalName { get; set; }
        public string ExternalName { get; set; }
        public bool? IsCustomAttribute { get; set; }
        public bool? IsCustomizable { get; set; }
        public bool? IsAuditEnabled { get; set; }
        public bool? HasChanged { get; set; }
        public string InheritsFrom { get; set; }
        public string IntroducedVersion { get; set; }
        public string FormulaDefinition { get; set; }
        public string LookupTo { get; set; }
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }
        public int? Precision { get; set; }
        public int? MaxLength { get; set; }
        public string OptionSet { get; set; }
        public string Value { get; set; }
        public int? ColumnNumber { get; set; }
        public DateTime CreatedOn { get; set; }
        public string DeprecatedVersion { get; set; }
        public string ExtensionData { get; set; }
        public string AttributeTypeName { get; set; }
        public string AutoNumberFormat { get; set; }
        public bool? CanBeSecuredForCreate { get; set; }
        public bool? CanBeSecuredForRead { get; set; }
        public bool? CanBeSecuredForUpdate { get; set; }
        public bool? CanModifiedAdditionalSettings { get; set; }
        public bool? IsCalculated { get; set; }
        public bool? IsFormula { get; set; }

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

        /// <summary>
        /// JavaScript field modifications related to this attribute
        /// </summary>
        public List<DataDictionaryJavaScriptFieldModification> JavaScriptFieldModifications { get; set; } = new List<DataDictionaryJavaScriptFieldModification>();
    }
}