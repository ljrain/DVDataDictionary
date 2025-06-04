using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

public class FormFieldInspector
{
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
}