using DataDictionary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDictionaryProcessor
{
    public class DvProcessor
    {
        //DataDictionary
        //│
        //├── Entity[1..*]
        //│   ├── EntityName: string
        //│   ├── EntityDescription: string
        //│   ├── EntityType: string
        //│   ├── Metadata: { key: value, ... }
        //│   │
        //│   └── Attribute[1..*]
        //│       ├── AttributeName: string
        //│       ├── DataType: string
        //│       ├── IsNullable: boolean
        //│       ├── DefaultValue: any
        //│       ├── Metadata: { key: value, ... }
        //│       └── ActionFunction: JavaScriptFunctionReference

        #region "Private Fields"

        Dictionary<string, DataDictionarySolution> _ddSolutions;
        DataDictionary.Models.DataDictionary _ddModel = new DataDictionary.Models.DataDictionary();

        private List<string> _allowedLogicalNames = new List<string>();
        private Dictionary<string, List<string>> _allowedtableAttributes = new Dictionary<string, List<string>>();
        List<DataDictionaryJavaScriptFieldModification> modifications;

        #endregion

        #region Public Properties

        public List<string> AllowedLogicalNames
        {
            get { return _allowedLogicalNames; }
            set { _allowedLogicalNames = value; }
        }

        public Dictionary<string, List<string>> AllowedTableAttributes
        {
            get { return _allowedtableAttributes; }
            set { _allowedtableAttributes = value; }
        }

        public List<DataDictionaryJavaScriptFieldModification> Modifications
        {
            get { return modifications; }
            set { modifications = value; }
        }


        #endregion


        public DataDictionary.Models.DataDictionary DdModel
        {
            get { return _ddModel; }
            set { _ddModel = value; }
        }

        public void ProcessData(Dictionary<string, DataDictionarySolution> ddSolutions)
        {
            _ddSolutions = ddSolutions;
            foreach (var solutionName in _ddSolutions.Keys)
            {
                Console.WriteLine($"Processing Solution: {solutionName}");
                if (solutionName == "Default")
                    return;

                _ddModel.Solutions.Add(solutionName);

                foreach (var entity in _ddSolutions[solutionName].Entities)
                {
                    Console.WriteLine($"Processing Entity: {entity.LogicalName}");
                    if (!_ddModel.Entities.ContainsKey(entity.LogicalName))
                    {
                        _ddModel.Entities[entity.LogicalName] = new DataDictionaryEntity
                        {
                            LogicalName = entity.LogicalName,
                            ObjectTypeCode = entity.ObjectTypeCode,
                            EntityId = entity.EntityId,
                            EntitySetName = entity.EntitySetName,
                            BaseTableName = entity.BaseTableName,
                            CollectionName = entity.CollectionName,
                            ComponentState = entity.ComponentState,
                            ComponentStateName = entity.ComponentStateName,
                            ExtensionTableName = entity.ExtensionTableName,
                            ExternalCollectionName = entity.ExternalCollectionName,
                            ExternalName = entity.ExternalName,
                            IsActivity = entity.IsActivity,

                        };
                    }


                    // Find all of the AttributeMetadata that has the same EntityLogicalName
                    // Filter AttributeMetadata for attributes where Table matches the entity's LogicalName
                    var matchingAttributes = _ddSolutions["Default"].AttributeMetadata
                        .Where(attr =>
                            string.Equals(attr.Table, entity.LogicalName, StringComparison.OrdinalIgnoreCase) &&
                            (attr.IsCustomizable == true || (attr.IsCustomizable.HasValue && attr.IsCustomizable.Value == true))
                        )
                        .ToList();


                    Console.WriteLine($"Found {matchingAttributes.Count} matching attributes for entity {entity.LogicalName}");

                    foreach (DataDictionaryAttributeMetadata attrMetadata in matchingAttributes)
                    {
                        DataDictionaryAttribute ddAttr = new DataDictionaryAttribute();
                        ddAttr.AttributeName = attrMetadata.ColumnLogical;
                        ddAttr.Metadata = attrMetadata;
                        ddAttr.ApplyValues();

                        _ddModel.Entities[entity.LogicalName].AddAttribute(ddAttr);
                    }
                    Console.WriteLine($"Processing Attributes for Entity: {entity.LogicalName}");
                }
            }
            Console.WriteLine("Data Dictionary Processing Complete.");
        }

        public void PrintDataDictionary()
        {
            Console.WriteLine("Data Dictionary Contents:");
            foreach (var solution in _ddModel.Solutions)
            {
                Console.WriteLine($"Solution: {solution}");
                if (_ddModel.Entities.Count == 0)
                {
                    Console.WriteLine("No entities found in this solution.");
                    continue;
                }
                Console.WriteLine($"Entities Count: {_ddModel.Entities.Count}");
                Console.WriteLine($"Attributes Count: {_ddModel.Entities.Values.Sum(e => e.Attributes.Count())}");
            }

            foreach (var entity in _ddModel.Entities.Values)
            {
                Console.WriteLine($"Entity Full Name: {entity.EntitySetName}.{entity.LogicalName}");
                Console.WriteLine($"Entity: {entity.LogicalName}, Type: {entity.ObjectTypeCode}");
                Console.WriteLine($"Attributes Count: {entity.Attributes.Count()}");
                foreach (var attribute in entity.Attributes)
                {
                    Console.WriteLine($"  Attribute Full Name: {entity.EntitySetName}.{attribute.AttributeName}");
                    // check if any field name is matching a modification from the javaScript parser
                    // Check if any modification matches the attribute name
                    //  Attribute: Fax, Type: String
                    var matchingModification = this.Modifications?
                        .FirstOrDefault(mod =>
                            string.Equals(mod.FieldName, attribute.AttributeName, StringComparison.OrdinalIgnoreCase));
                    if (matchingModification != null)
                    {
                        Console.WriteLine($"    [JS Modified] Field: {matchingModification.FieldName}, Modification: {matchingModification.ModificationType}, WebResource: {matchingModification.WebResourceName}");
                        attribute.Modifications.Add(matchingModification);
                    }

                    Console.WriteLine($"  Attribute: {attribute.AttributeName}, Type: {attribute.Metadata.DataType}");
                }
            }

        }
    }
}
