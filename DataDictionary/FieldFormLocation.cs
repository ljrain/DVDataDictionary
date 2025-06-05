using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataDictionary
{
    /// <summary>
    /// Represents the location of a field on a form, including tab, section, and visibility information.
    /// </summary>
    [DataContract]
    public class FieldFormLocation
{
    /// <summary>
    /// The name of the form where the field appears.
    /// </summary>
    [DataMember]
    public string FormName { get; set; }

    /// <summary>
    /// The name of the tab where the field appears.
    /// </summary>
    [DataMember]
    public string TabName { get; set; }

    /// <summary>
    /// Indicates whether the tab is visible by default.
    /// </summary>
    [DataMember]
    public bool TabVisible { get; set; }

    /// <summary>
    /// The name of the section where the field appears.
    /// </summary>
    [DataMember]
    public string SectionName { get; set; }

    /// <summary>
    /// Indicates whether the section is visible by default.
    /// </summary>
    [DataMember]
    public bool SectionVisible { get; set; }

    /// <summary>
    /// Indicates whether the field is visible by default.
    /// </summary>
    [DataMember]
    public bool FieldVisible { get; set; }

    /// <summary>
    /// The schema name of the field.
    /// </summary>
    [DataMember]
    public string FieldName { get; set; }
}

/// <summary>
/// Represents metadata for a field, including schema, display, type, requirements, size, precision, form locations, and script references.
/// </summary>
[DataContract]
public class FieldMetadata
{
    /// <summary>
    /// Logical name of the entity this field belongs to.
    /// </summary>
    [DataMember]
    public string EntityName { get; set; }

    /// <summary>
    /// Schema (logical) name of the field.
    /// </summary>
    [DataMember]
    public string SchemaName { get; set; }

    /// <summary>
    /// Display name of the field.
    /// </summary>
    [DataMember]
    public string DisplayName { get; set; }

    /// <summary>
    /// Data type of the field.
    /// </summary>
    [DataMember]
    public string Type { get; set; }

    /// <summary>
    /// Requirement level of the field (e.g., None, ApplicationRequired).
    /// </summary>
    [DataMember]
    public string RequiredLevel { get; set; }

    /// <summary>
    /// Description of the field.
    /// </summary>
    [DataMember]
    public string Description { get; set; }

    /// <summary>
    /// Maximum length for string or memo fields, if applicable.
    /// </summary>
    [DataMember]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Precision for numeric fields, if applicable.
    /// </summary>
    [DataMember]
    public int? Precision { get; set; }

    /// <summary>
    /// Minimum value for numeric fields, if applicable.
    /// </summary>
    [DataMember]
    public int? MinValue { get; set; }

    /// <summary>
    /// Maximum value for numeric fields, if applicable.
    /// </summary>
    [DataMember]
    public int? MaxValue { get; set; }

    /// <summary>
    /// List of locations where this field appears on forms, including visibility information.
    /// </summary>
    [DataMember]
    public List<FieldFormLocation> FormLocations { get; set; } = new List<FieldFormLocation>();

    /// <summary>
    /// List of script (web resource) names that reference this field.
    /// </summary>
    [DataMember]
    public List<string> ScriptReferences { get; set; } = new List<string>();

    /// <summary>
    /// Indicates if the field is hidden by any script (setVisible(false)).
    /// </summary>
    [DataMember]
    public bool HiddenByScript { get; set; }
}
}