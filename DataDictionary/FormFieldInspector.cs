using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

/// <summary>
/// Provides methods to inspect forms and extract field, tab, and section visibility information for Dataverse entities.
/// </summary>
public class FormFieldInspector
{
    /// <summary>
    /// Represents a field's location and visibility on a form, including tab and section context.
    /// </summary>
    public class FieldOnFormSection
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
        /// The schema name of the field.
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// Indicates whether the field is visible by default.
        /// </summary>
        public bool FieldVisible { get; set; }
    }

    /// <summary>
    /// Returns a mapping of form name to list of field schema names present on the form for a given entity.
    /// </summary>
    /// <param name="service">Organization service.</param>
    /// <param name="entityLogicalName">Logical name of the entity.</param>
    /// <returns>Dictionary mapping form names to lists of field schema names.</returns>
    public static Dictionary<string, List<string>> GetFieldsOnForms(IOrganizationService service, string entityLogicalName)
    {
        var result = new Dictionary<string, List<string>>();

        // Query all main forms for the entity
        var query = new QueryExpression("systemform")
        {
            ColumnSet = new ColumnSet("name", "formxml"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("objecttypecode", ConditionOperator.Equal, entityLogicalName),
                    new ConditionExpression("type", ConditionOperator.Equal, 2) // 2 = Main form
                }
            }
        };

        var forms = service.RetrieveMultiple(query).Entities;

        foreach (var form in forms)
        {
            var formName = form.GetAttributeValue<string>("name");
            var formXml = form.GetAttributeValue<string>("formxml");
            var fields = new List<string>();

            if (!string.IsNullOrEmpty(formXml))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(formXml);

                // Find all <control datafieldname="..."> nodes
                var controlNodes = xmlDoc.SelectNodes("//control[@datafieldname]");
                foreach (XmlNode node in controlNodes)
                {
                    var fieldName = node.Attributes["datafieldname"]?.Value;
                    if (!string.IsNullOrEmpty(fieldName) && !fields.Contains(fieldName))
                        fields.Add(fieldName);
                }
            }

            result[formName] = fields;
        }

        return result;
    }

    /// <summary>
    /// Returns a mapping of field schema name to a list of its appearances on forms/sections with visibility info.
    /// </summary>
    /// <param name="service">Organization service.</param>
    /// <param name="entityLogicalName">Logical name of the entity.</param>
    /// <returns>Dictionary mapping field schema names to lists of FieldOnFormSection objects.</returns>
    public static Dictionary<string, List<FieldOnFormSection>> GetFieldVisibilityOnForms(IOrganizationService service, string entityLogicalName)
    {
        var result = new Dictionary<string, List<FieldOnFormSection>>(StringComparer.OrdinalIgnoreCase);

        var query = new QueryExpression("systemform")
        {
            ColumnSet = new ColumnSet("name", "formxml"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("objecttypecode", ConditionOperator.Equal, entityLogicalName),
                    new ConditionExpression("type", ConditionOperator.Equal, 2) // Main form
                }
            }
        };

        var forms = service.RetrieveMultiple(query).Entities;

        foreach (var form in forms)
        {
            var formName = form.GetAttributeValue<string>("name");
            var formXml = form.GetAttributeValue<string>("formxml");
            if (string.IsNullOrEmpty(formXml)) continue;

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(formXml);

            var tabNodes = xmlDoc.SelectNodes("//tab");
            foreach (XmlNode tabNode in tabNodes)
            {
                var tabName = tabNode.Attributes["name"]?.Value ?? "";
                var tabVisible = tabNode.Attributes["visible"] == null || tabNode.Attributes["visible"].Value != "false";

                var sectionNodes = tabNode.SelectNodes(".//section");
                foreach (XmlNode sectionNode in sectionNodes)
                {
                    var sectionName = sectionNode.Attributes["name"]?.Value ?? "";
                    var sectionVisible = sectionNode.Attributes["visible"] == null || sectionNode.Attributes["visible"].Value != "false";

                    var controlNodes = sectionNode.SelectNodes(".//control[@datafieldname]");
                    foreach (XmlNode controlNode in controlNodes)
                    {
                        var fieldName = controlNode.Attributes["datafieldname"]?.Value;
                        if (string.IsNullOrEmpty(fieldName)) continue;

                        // Field visibility: if attribute not present, default is visible
                        var fieldVisible = controlNode.Attributes["visible"] == null || controlNode.Attributes["visible"].Value != "false";

                        if (!result.ContainsKey(fieldName))
                            result[fieldName] = new List<FieldOnFormSection>();

                        result[fieldName].Add(new FieldOnFormSection
                        {
                            FormName = formName,
                            TabName = tabName,
                            TabVisible = tabVisible,
                            SectionName = sectionName,
                            SectionVisible = sectionVisible,
                            FieldName = fieldName,
                            FieldVisible = fieldVisible
                        });
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Returns a list of all fields on all forms for an entity, including their tab/section and visibility.
    /// </summary>
    /// <param name="service">Organization service.</param>
    /// <param name="entityLogicalName">Logical name of the entity.</param>
    /// <returns>List of FieldOnFormSection objects with form, tab, section, and visibility info.</returns>
    public static List<FieldOnFormSection> GetAllFieldsWithVisibility(IOrganizationService service, string entityLogicalName)
    {
        var result = new List<FieldOnFormSection>();

        var query = new QueryExpression("systemform")
        {
            ColumnSet = new ColumnSet("name", "formxml"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("objecttypecode", ConditionOperator.Equal, entityLogicalName),
                    new ConditionExpression("type", ConditionOperator.Equal, 2) // Main form
                }
            }
        };

        var forms = service.RetrieveMultiple(query).Entities;

        foreach (var form in forms)
        {
            var formName = form.GetAttributeValue<string>("name");
            var formXml = form.GetAttributeValue<string>("formxml");
            if (string.IsNullOrEmpty(formXml)) continue;

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(formXml);

            var tabNodes = xmlDoc.SelectNodes("//tab");
            foreach (XmlNode tabNode in tabNodes)
            {
                var tabName = tabNode.Attributes["name"]?.Value ?? "";
                var tabVisible = tabNode.Attributes["visible"] == null || tabNode.Attributes["visible"].Value != "false";

                var sectionNodes = tabNode.SelectNodes(".//section");
                foreach (XmlNode sectionNode in sectionNodes)
                {
                    var sectionName = sectionNode.Attributes["name"]?.Value ?? "";
                    var sectionVisible = sectionNode.Attributes["visible"] == null || sectionNode.Attributes["visible"].Value != "false";

                    var controlNodes = sectionNode.SelectNodes(".//control[@datafieldname]");
                    foreach (XmlNode controlNode in controlNodes)
                    {
                        var fieldName = controlNode.Attributes["datafieldname"]?.Value;
                        if (string.IsNullOrEmpty(fieldName)) continue;

                        var fieldVisible = controlNode.Attributes["visible"] == null || controlNode.Attributes["visible"].Value != "false";

                        result.Add(new FieldOnFormSection
                        {
                            FormName = formName,
                            TabName = tabName,
                            TabVisible = tabVisible,
                            SectionName = sectionName,
                            SectionVisible = sectionVisible,
                            FieldName = fieldName,
                            FieldVisible = fieldVisible
                        });
                    }
                }
            }
        }
        return result;
    }
}