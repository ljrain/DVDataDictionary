using System.Collections.Generic;

/// <summary>
/// Represents the location of a field on a form, including tab, section, and visibility information.
/// </summary>
public class FieldFormLocation
{
    /// <summary>
    /// The name of the form where the field appears.
    /// </summary>
    public string FormName { get; set; }

    /// <summary>
    /// The name of the tab where the field appears.
    /// </summary>
    public string TabName { get; set; }

    /// <summary>
    /// Indicates whether the tab is visible by default.
    /// </summary>
    public bool TabVisible { get; set; }

    /// <summary>
    /// The name of the section where the field appears.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Indicates whether the section is visible by default.
    /// </summary>
    public bool SectionVisible { get; set; }

    /// <summary>
    /// Indicates whether the field is visible by default.
    /// </summary>
    public bool FieldVisible { get; set; }
}

/// <summary>
/// Represents metadata for a field, including schema, display, type, requirements, size, precision, form locations, and script references.
/// </summary>
public class FieldMetadata
{
    /// <summary>
    /// Logical name of the entity this field belongs to.
    /// </summary>
    public string EntityName { get; set; }

    /// <summary>
    /// Schema (logical) name of the field.
    /// </summary>
    public string SchemaName { get; set; }

    /// <summary>
    /// Display name of the field.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Data type of the field.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Requirement level of the field (e.g., None, ApplicationRequired).
    /// </summary>
    public string RequiredLevel { get; set; }

    /// <summary>
    /// Description of the field.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Maximum length for string or memo fields, if applicable.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Precision for numeric fields, if applicable.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Minimum value for numeric fields, if applicable.
    /// </summary>
    public int? MinValue { get; set; }

    /// <summary>
    /// Maximum value for numeric fields, if applicable.
    /// </summary>
    public int? MaxValue { get; set; }

    /// <summary>
    /// List of locations where this field appears on forms, including visibility information.
    /// </summary>
    public List<FieldFormLocation> FormLocations { get; set; }

    /// <summary>
    /// List of script (web resource) names that reference this field.
    /// </summary>
    public List<string> ScriptReferences { get; set; }

    /// <summary>
    /// Indicates if the field is hidden by any script (setVisible(false)).
    /// </summary>
    public bool HiddenByScript { get; set; }

    // Enhanced metadata properties based on comprehensive analysis

    /// <summary>
    /// Name of the field this attribute is an image of (for image fields)
    /// </summary>
    public string AttributeOf { get; set; }

    /// <summary>
    /// Type name of the attribute (provides more specific type information)
    /// </summary>
    public string AttributeTypeName { get; set; }

    /// <summary>
    /// Indicates whether the field can be secured for create operations
    /// </summary>
    public bool CanBeSecuredForCreate { get; set; }

    /// <summary>
    /// Indicates whether the field can be secured for read operations
    /// </summary>
    public bool CanBeSecuredForRead { get; set; }

    /// <summary>
    /// Indicates whether the field can be secured for update operations
    /// </summary>
    public bool CanBeSecuredForUpdate { get; set; }

    /// <summary>
    /// Column number in the database
    /// </summary>
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// Version in which the attribute was deprecated
    /// </summary>
    public string DeprecatedVersion { get; set; }

    /// <summary>
    /// Whether the field has changed from its default definition
    /// </summary>
    public bool HasChanged { get; set; }

    /// <summary>
    /// Field this attribute inherits from
    /// </summary>
    public string InheritsFrom { get; set; }

    /// <summary>
    /// Version in which the attribute was introduced
    /// </summary>
    public string IntroducedVersion { get; set; }

    /// <summary>
    /// Indicates whether this is a custom attribute
    /// </summary>
    public bool IsCustomAttribute { get; set; }

    /// <summary>
    /// Indicates whether this field contains sensitive data
    /// </summary>
    public bool IsDataSourceSecret { get; set; }

    /// <summary>
    /// Indicates whether the field can be filtered in views
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// Indicates whether the field is a logical attribute
    /// </summary>
    public bool IsLogical { get; set; }

    /// <summary>
    /// Indicates whether this is a managed attribute
    /// </summary>
    public bool IsManaged { get; set; }

    /// <summary>
    /// Indicates whether this is the primary ID field
    /// </summary>
    public bool IsPrimaryId { get; set; }

    /// <summary>
    /// Indicates whether this is the primary name field
    /// </summary>
    public bool IsPrimaryName { get; set; }

    /// <summary>
    /// Indicates whether the field is required for forms
    /// </summary>
    public bool IsRequiredForForm { get; set; }

    /// <summary>
    /// Indicates whether the field can be retrieved
    /// </summary>
    public bool IsRetrievable { get; set; }

    /// <summary>
    /// Indicates whether the field is searchable
    /// </summary>
    public bool IsSearchable { get; set; }

    /// <summary>
    /// Indicates whether field-level security is enabled
    /// </summary>
    public bool IsSecured { get; set; }

    /// <summary>
    /// Indicates whether the field is valid for create operations
    /// </summary>
    public bool IsValidForCreate { get; set; }

    /// <summary>
    /// Indicates whether the field is valid for forms
    /// </summary>
    public bool IsValidForForm { get; set; }

    /// <summary>
    /// Indicates whether the field is valid for grids
    /// </summary>
    public bool IsValidForGrid { get; set; }

    /// <summary>
    /// Indicates whether the field is valid for read operations
    /// </summary>
    public bool IsValidForRead { get; set; }

    /// <summary>
    /// Indicates whether the field is valid for update operations
    /// </summary>
    public bool IsValidForUpdate { get; set; }

    /// <summary>
    /// Indicates whether this is a valid OData attribute
    /// </summary>
    public bool IsValidODataAttribute { get; set; }

    /// <summary>
    /// ID of linked attribute (for calculated/rollup fields)
    /// </summary>
    public string LinkedAttributeId { get; set; }

    /// <summary>
    /// Unique identifier for this attribute metadata
    /// </summary>
    public string MetadataId { get; set; }

    /// <summary>
    /// Source type: Standard, Calculated, or Rollup
    /// </summary>
    public string SourceType { get; set; }

    /// <summary>
    /// Auto number format (for auto number fields)
    /// </summary>
    public string AutoNumberFormat { get; set; }

    /// <summary>
    /// Database length (different from MaxLength for some field types)
    /// </summary>
    public int? DatabaseLength { get; set; }

    /// <summary>
    /// Format for the field (affects display behavior)
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// Formula for calculated fields
    /// </summary>
    public string Formula { get; set; }

    /// <summary>
    /// Rollup state for rollup fields
    /// </summary>
    public string RollupState { get; set; }

    /// <summary>
    /// External name for virtual fields
    /// </summary>
    public string ExternalName { get; set; }

    /// <summary>
    /// Option set information for picklist fields
    /// </summary>
    public List<OptionMetadata> OptionSet { get; set; }

    /// <summary>
    /// Default form value for the field
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// Lookup targets for lookup fields
    /// </summary>
    public string[] Targets { get; set; }

    /// <summary>
    /// Relationship name for lookup fields
    /// </summary>
    public string RelationshipName { get; set; }
}

/// <summary>
/// Represents an option in an option set
/// </summary>
public class OptionMetadata
{
    /// <summary>
    /// Value of the option
    /// </summary>
    public int? Value { get; set; }

    /// <summary>
    /// Label of the option
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Description of the option
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Color associated with the option
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// Whether this is the default option
    /// </summary>
    public bool IsDefault { get; set; }
}