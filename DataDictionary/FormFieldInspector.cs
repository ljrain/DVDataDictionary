using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DataDictionary; // Use the shared FieldFormLocation class

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
        var results = new List<FieldFormLocation>();

        // Query all main forms for the entity
        var query = new QueryExpression("systemform")
        {
            ColumnSet = new ColumnSet("formid", "name", "formxml"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("objecttypecode", ConditionOperator.Equal, entityLogicalName),
                    new ConditionExpression("type", ConditionOperator.Equal, 2) // 2 = Main form
                }
            }
        };

        var forms = service.RetrieveMultiple(query);

        foreach (var form in forms.Entities)
        {
            var formName = form.GetAttributeValue<string>("name");
            var formXml = form.GetAttributeValue<string>("formxml");
            if (string.IsNullOrEmpty(formXml))
                continue;

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(formXml);

            // Parse tabs
            var tabNodes = xmlDoc.SelectNodes("//tabs/tab");
            if (tabNodes == null) continue;

            foreach (XmlNode tabNode in tabNodes)
            {
                var tabName = tabNode.Attributes?["name"]?.Value ?? "";
                var tabVisible = tabNode.Attributes?["visible"]?.Value != "false";

                // Parse sections
                var sectionNodes = tabNode.SelectNodes("columns/column/sections/section");
                if (sectionNodes == null) continue;

                foreach (XmlNode sectionNode in sectionNodes)
                {
                    var sectionName = sectionNode.Attributes?["name"]?.Value ?? "";
                    var sectionVisible = sectionNode.Attributes?["visible"]?.Value != "false";

                    // Parse fields
                    var cellNodes = sectionNode.SelectNodes("rows/row/cell");
                    if (cellNodes == null) continue;

                    foreach (XmlNode cellNode in cellNodes)
                    {
                        var fieldName = cellNode.Attributes?["id"]?.Value ?? "";
                        var fieldVisible = cellNode.Attributes?["visible"]?.Value != "false";
                        var fieldDescription = ""; // Not available in form XML

                        if (!string.IsNullOrEmpty(fieldName))
                        {
                            results.Add(new FieldFormLocation
                            {
                                FormName = formName,
                                TabName = tabName,
                                TabVisible = tabVisible,
                                SectionName = sectionName,
                                SectionVisible = sectionVisible,
                                FieldVisible = fieldVisible,
                                FieldName = fieldName,
                                FieldDescription = fieldDescription
                            });
                        }
                    }
                }
            }
        }

        return results;
    }
}
