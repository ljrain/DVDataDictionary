using System;
using System.Collections.Generic;

namespace DataDictionary.Models
{
    public class DataDictionaryEntity
    {
        public Guid EntityId { get; set; }
        public string EntitySetName { get; set; }
        public string BaseTableName { get; set; }
        public string CollectionName { get; set; }
        public int ComponentState { get; set; }
        public string ComponentStateName { get; set; }
        public string ExtensionTableName { get; set; }
        public string ExternalCollectionName { get; set; }
        public string ExternalName { get; set; }
        public bool IsActivity { get; set; }
        public string IsActivityName { get; set; }
        public string LogicalCollectionName { get; set; }
        public string LogicalName { get; set; }
        public int ObjectTypeCode { get; set; }
        public string OriginalLocalizedCollectionName { get; set; }
        public string OriginalLocalizedName { get; set; }
        public DateTime? OverwriteTime { get; set; }
        public string ParentControllingAttributeName { get; set; }
        public string PhysicalName { get; set; }
        public string ReportViewName { get; set; }
        public string Name { get; set; }

        public IEnumerable<DataDictionaryAttribute> Attributes { get; set; } = new List<DataDictionaryAttribute>();

        public void AddAttribute(DataDictionaryAttribute attribute)
        {
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));
            ((List<DataDictionaryAttribute>)Attributes).Add(attribute);
        }

        public void RemoveAttribute(DataDictionaryAttribute attribute)
        {
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));
            ((List<DataDictionaryAttribute>)Attributes).Remove(attribute);
        }
    }
}
