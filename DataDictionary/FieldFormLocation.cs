using System.Collections.Generic;

/// <summary>
/// Represents the location of a field on a form, including tab, section, visibility information, and permissions.
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

    /// <summary>
    /// The name of the field.
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// The description of the field.
    /// </summary>
    public string FieldDescription { get; set; }

    /// <summary>
    /// Indicates whether the user has read permissions for the field.
    /// </summary>
    public bool CanRead { get; set; }

    /// <summary>
    /// Indicates whether the user has write permissions for the field.
    /// </summary>
    public bool CanWrite { get; set; }

    /// <summary>
    /// Indicates whether the user has create permissions for the field.
    /// </summary>
    public bool CanCreate { get; set; }
}