using System;

namespace DataDictionary.Models
{
    /// <summary>
    /// Represents JavaScript modifications applied to a field/control in Dataverse forms
    /// </summary>
    public class DataDictionaryJavaScriptFieldModification
    {
        // DependencyXml is not used in save logic, consider removing if not needed elsewhere
        public string DependencyXml { get; set; } // UNUSED: Not saved to Dataverse

        public string FieldName { get; set; }
        public Guid WebResourceId { get; set; }
        public string WebResourceName { get; set; }
        public JavaScriptModificationType ModificationType { get; set; }
        public string ModificationValue { get; set; }
        public string JavaScriptCode { get; set; }
        public int? LineNumber { get; set; }
        public string Notes { get; set; }
        public DateTime ParsedOn { get; set; } = DateTime.UtcNow;

        // These are now mapped in save logic if not null/empty
        public string AttributeTable { get; set; }
        public string AttributeLogicalName { get; set; }

        // ParentAttribute is for in-memory correlation only, not persisted
        public DataDictionaryAttributeMetadata ParentAttribute { get; set; } // NOT SAVED TO DATAVERSE
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