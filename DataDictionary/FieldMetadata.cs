using System.Collections.Generic;

public class FieldMetadata
{
    public string EntityName { get; set; }
    public string SchemaName { get; set; }
    public string DisplayName { get; set; }
    public string Type { get; set; }
    public string RequiredLevel { get; set; }
    public string Description { get; set; }
    public List<string> Forms { get; set; } // <-- Add this line
}