using System;

namespace DataDictionary.Models
{
    public class DataDictionaryAttributeMetadata
    {
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
        public Int64? Precision { get; set; }
        public string OptionSet { get; set; }
        public Int64? Value { get; set; }
        public string Description { get; set; }
        public Int64? LangCode { get; set; }
    }
}