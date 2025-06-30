using System;

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
        public string OptionSet { get; set; } // NOT SAVED TO DATAVERSE (unless mapped in SaveToDataverse)
        public string Value { get; set; } // NOT SAVED TO DATAVERSE (unless mapped in SaveToDataverse)
        public string Description { get; set; }
        public Int32? LangCode { get; set; }
        public string ExtensionData { get; set; } // UNUSED: Not set or saved, recommend removal
        public string AttributeTypeName { get; set; } // NOT SAVED TO DATAVERSE (unless mapped in SaveToDataverse)
        public string AutoNumberFormat { get; set; } // NOT SAVED TO DATAVERSE (unless mapped in SaveToDataverse)
        public bool? CanModifiedAdditionalSettings { get; set; } // NOT SAVED TO DATAVERSE (unless mapped in SaveToDataverse)
    }
}