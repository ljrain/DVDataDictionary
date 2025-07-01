using System;
using System.Collections.Generic;

namespace DataDictionary.Models
{
    public class DataDictionaryAttribute
    {
        public string FullName { get; set; }
        //public Guid AttributeId { get; set; }
        public Guid AttributeOf { get; set; }
        public string AttributeName { get; set; }
        //public Guid AttributeTypeId { get; set; }
        //public string AttributeTypeName { get; set; }
        //public string Description { get; set; }
        //public bool IsPrimaryKey { get; set; }
        //public bool IsRequired { get; set; }
        //public bool IsValidForCreate { get; set; }
        //public bool IsValidForUpdate { get; set; }
        //public int MaxLength { get; set; }
        //public int MinValue { get; set; }
        //public int MaxValue { get; set; }
        //public string OptionSetName { get; set; }
        public string LogicalName { get; set; }
        //public string PhysicalName { get; set; }
        //public string ComponentStateName { get; set; }

        public DataDictionaryAttributeMetadata Metadata { get; set; } = new DataDictionaryAttributeMetadata();

        public List<DataDictionaryJavaScriptFieldModification> Modifications { get; set; } = new List<DataDictionaryJavaScriptFieldModification>();

        public void ApplyValues()
        {
            if (this.Metadata == null)
            {
                throw new Exception("Metadata cannot be null");
            }
            this.FullName = this.Metadata.Table + "." + this.Metadata.ColumnLogical;
            this.LogicalName = this.Metadata.ColumnLogical;
            this.AttributeName = this.Metadata.DisplayName;


        }
    }
}
