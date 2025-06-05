using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

/// <summary>  
/// Helper class for extracting field, tab, and section visibility from form XML.  
/// </summary>  
public static class FormFieldInspector
{
    /// <summary>  
    /// Retrieves all fields with visibility information for a given entity.  
    /// </summary>  
    /// <param name="service">The organization service.</param>  
    /// <param name="entityLogicalName">The logical name of the entity.</param>  
    /// <returns>A list of field form locations.</returns>  
    public static IEnumerable<FieldFormLocation> GetAllFieldsWithVisibility(IOrganizationService service, string entityLogicalName)
    {
        // Placeholder implementation for retrieving field visibility information.  
        // Replace this with actual logic to parse form XML and extract field visibility details.  
        return new List<FieldFormLocation>();
    }

    public class FieldFormLocation
    {
        public string FormName { get; set; }
        public string TabName { get; set; }
        public bool TabVisible { get; set; }
        public string SectionName { get; set; }
        public bool SectionVisible { get; set; }
        public bool FieldVisible { get; set; }
        public string FieldName { get; set; }
        public string FieldDescription { get; set; }
    }
}
