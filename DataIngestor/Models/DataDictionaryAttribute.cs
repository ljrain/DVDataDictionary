using System;

namespace DataDictionary.Models
{
    public class DataDictionaryAttribute
    {
        public Guid AttributeId { get; set; }
        public Guid AttributeOf { get; set; }
        public string AttributeName { get; set; }
        public Guid AttributeTypeId { get; set; }
        public string AttributeTypeName { get; set; }
        public string Description { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsRequired { get; set; }
        public bool IsValidForCreate { get; set; }
        public bool IsValidForUpdate { get; set; }
        public int MaxLength { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public string OptionSetName { get; set; }
        public string LogicalName { get; set; }
        public string PhysicalName { get; set; }
        public string ComponentStateName { get; set; }
    }
}
