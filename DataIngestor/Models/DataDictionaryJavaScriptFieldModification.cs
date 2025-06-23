using System;
using System.Collections.Generic;

namespace DataDictionary.Models
{
    /// <summary>
    /// Represents JavaScript modifications applied to a field/control in Dataverse forms
    /// </summary>
    public class DataDictionaryJavaScriptFieldModification
    {
        public string DependencyXml { get; set; }
        public string FieldName { get; set; }
        public Guid WebResourceId { get; set; }
        public string WebResourceName { get; set; }
        public JavaScriptModificationType ModificationType { get; set; }
        public string ModificationValue { get; set; }
        public string JavaScriptCode { get; set; }
        public int? LineNumber { get; set; }
        public string Notes { get; set; }
        public DateTime ParsedOn { get; set; } = DateTime.UtcNow;
        public string AttributeTable { get; set; }
        public string AttributeLogicalName { get; set; }
        public DataDictionaryAttributeMetadata ParentAttribute { get; set; }
    }

    /// <summary>
    /// Types of JavaScript modifications that can be applied to fields
    /// </summary>
    public enum JavaScriptModificationType
    {
        Visibility,
        RequiredLevel,
        DefaultValue,
        DisabledState,
        DisplayName,
        Other
    }
}