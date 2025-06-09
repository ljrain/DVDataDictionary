using System.Collections.Generic;

// Ensure the 'FieldFormLocation' type is defined or imported
// If 'FieldFormLocation' is part of your project but in a different namespace, add the appropriate using directive here.
// Example: using YourNamespaceName;

public class FieldMetadata
{
    public string EntityFieldName { get; set; } // Logical name of the field in the entity

    public string EntityName { get; set; }
    public string SchemaName { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string RequiredLevel { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public List<DataDictionary.FieldFormLocation> FormLocations { get; set; } // Ensure 'FieldFormLocation' is defined
    public List<string> ScriptReferences { get; set; }
    public bool HiddenByScript { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanCreate { get; set; }
    public string Permissions { get; set; }
    public List<string> SolutionNames { get; set; }
}
