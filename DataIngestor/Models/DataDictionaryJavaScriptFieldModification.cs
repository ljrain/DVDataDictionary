using System;
using System.Collections.Generic;

namespace DataDictionary.Models
{
    /// <summary>
    /// Represents JavaScript modifications applied to a field/control in Dataverse forms
    /// </summary>
    public class DataDictionaryJavaScriptFieldModification
    {
        /// <summary>
        /// The logical name of the field being modified
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The web resource ID where this modification was found
        /// </summary>
        public Guid WebResourceId { get; set; }

        /// <summary>
        /// The web resource display name where this modification was found
        /// </summary>
        public string WebResourceName { get; set; }

        /// <summary>
        /// Type of modification (Hidden, Required, DefaultValue, etc.)
        /// </summary>
        public JavaScriptModificationType ModificationType { get; set; }

        /// <summary>
        /// The specific value set (for default values) or condition (for visibility/required)
        /// </summary>
        public string ModificationValue { get; set; }

        /// <summary>
        /// The actual JavaScript code line that performs the modification
        /// </summary>
        public string JavaScriptCode { get; set; }

        /// <summary>
        /// Line number in the JavaScript where this modification was found
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Additional context or notes about the modification
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// When this modification was detected/parsed
        /// </summary>
        public DateTime ParsedOn { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Types of JavaScript modifications that can be applied to fields
    /// </summary>
    public enum JavaScriptModificationType
    {
        /// <summary>
        /// Field visibility modification (setVisible)
        /// </summary>
        Visibility,
        
        /// <summary>
        /// Field required level modification (setRequiredLevel)
        /// </summary>
        RequiredLevel,
        
        /// <summary>
        /// Field default value assignment (setValue)
        /// </summary>
        DefaultValue,
        
        /// <summary>
        /// Field disabled/enabled state (setDisabled)
        /// </summary>
        DisabledState,
        
        /// <summary>
        /// Field display name modification (setLabel)
        /// </summary>
        DisplayName,
        
        /// <summary>
        /// Other field modifications not covered by specific types
        /// </summary>
        Other
    }
}