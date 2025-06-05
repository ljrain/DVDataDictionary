using System.Collections.Generic;

public class FieldFormLocation
{
    public string FormName { get; set; }
    public string TabName { get; set; }
    public bool TabVisible { get; set; }
    public string SectionName { get; set; }
    public bool SectionVisible { get; set; }
    public bool FieldVisible { get; set; }
}

public class FieldMetadata
{
    public string EntityName { get; set; }
    public string SchemaName { get; set; }
    public string DisplayName { get; set; }
    public string Type { get; set; }
    public string RequiredLevel { get; set; }
    public string Description { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public List<FieldFormLocation> FormLocations { get; set; }
    public List<string> ScriptReferences { get; set; }
}