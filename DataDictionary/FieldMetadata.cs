using System.Collections.Generic;

public class FieldMetadata
{
    public string SchemaName { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string EntityName { get; set; }
    public string Type { get; set; }
    public string RequiredLevel { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public List<FieldFormLocation> FormLocations { get; set; }
    public List<string> ScriptReferences { get; set; }

    // Add permissions
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanCreate { get; set; }
}
