using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

public class FormFieldInspector
{
    public class FieldOnFormSection
    {
        public string FormName { get; set; }
        public string TabName { get; set; }
        public bool TabVisible { get; set; }
        public string SectionName { get; set; }
        public bool SectionVisible { get; set; }
        public string FieldName { get; set; }
        public bool FieldVisible { get; set; }
    }

    /// <summary>
    /// Returns a mapping of form name to list of field schema names present on the form for a given entity.
    /// </summary>
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