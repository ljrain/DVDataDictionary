# DataDictionaryPlugin User Guide

## Overview
The DataDictionaryPlugin is a comprehensive Dynamics 365/Dataverse plugin that generates an enterprise-grade data dictionary for specified solutions. It collects extensive metadata about entities, fields, relationships, forms, and JavaScript web resources, analyzes script references, and outputs the results as JSON and CSV files attached to Note records in Dataverse.

## Key Features

### Comprehensive Metadata Collection
- **Entity Metadata**: 70+ properties including ownership, capabilities, icons, colors, audit settings
- **Field Metadata**: 50+ properties covering security, customization, behavior, format, and validation
- **Relationship Metadata**: OneToMany, ManyToOne, and ManyToMany relationships with cascade configurations
- **Form Analysis**: Real XML parsing of system forms to extract field locations and visibility
- **Script Analysis**: JavaScript web resource analysis for field references and visibility changes
- **Option Set Values**: Complete option set metadata with labels, descriptions, and colors
- **Security Metadata**: Field-level security settings and entity privileges

### Power FX Compatibility
- **DVDictionary Structure**: JSON output structured with DVDictionary parent for Power FX consumption
- **Hierarchical Organization**: Entities contain fields, relationships, keys, and privileges
- **Summary Analytics**: Built-in statistics and counts for quick analysis

### Advanced Analysis Features
- **Script Reference Detection**: Identifies JavaScript files that reference each field
- **Visibility Analysis**: Detects fields hidden by scripts (setVisible(false))
- **Calculated/Rollup Field Detection**: Identifies computed fields with formulas
- **Custom vs. Standard Classification**: Distinguishes custom from out-of-box components
- **Managed vs. Unmanaged Tracking**: Identifies solution packaging status

## How to Use
1. **Deploy the Plugin**: Register the DataDictionaryPlugin in your Dataverse environment using the Plugin Registration Tool or a deployment pipeline.
2. **Trigger the Plugin**: Execute the plugin by providing the required input parameter `SolutionNames` (array of solution unique names) via a custom workflow, plugin step, or direct execution.
3. **Output**: The plugin will generate both JSON and CSV files named `DataDictionary.json` and `DataDictionary.csv` and attach them to Note (annotation) records. The Note IDs will be available in the plugin's output parameters as `NoteId` and `CsvNoteId`.

## Input Parameters
- **SolutionNames**: Array of solution unique names to analyze (Required)

## Output Parameters
- **NoteId**: GUID of the Note record containing the JSON file
- **CsvNoteId**: GUID of the Note record containing the CSV file

## What the Data Dictionary Contains

### Entity-Level Information
- Basic properties (LogicalName, SchemaName, DisplayName, etc.)
- Ownership and access control settings
- Customization capabilities (can create forms, views, charts, etc.)
- Integration settings (audit, duplicate detection, mobile, etc.)
- Icons and branding (colors, help URLs)
- Relationships (OneToMany, ManyToOne, ManyToMany)
- Entity keys and security privileges

### Field-Level Information
- **Core Properties**: SchemaName, DisplayName, Type, RequiredLevel, Description
- **Data Properties**: MaxLength, Precision, MinValue, MaxValue, DefaultValue
- **Behavior Properties**: IsFilterable, IsSearchable, IsRetrievable, IsSecured
- **Security Properties**: CanBeSecuredForCreate/Read/Update, field-level security
- **Customization Properties**: IsCustomAttribute, IsManaged, IsCustomizable
- **Advanced Properties**: SourceType (Standard/Calculated/Rollup), Formula, Format
- **Option Set Values**: Complete picklist options with labels and colors
- **Lookup Properties**: Target entities and relationship names
- **Form Locations**: All forms, tabs, and sections where the field appears
- **Script References**: JavaScript files that reference the field

### Analytics and Summary
- Total counts of entities, fields, relationships, keys
- Custom vs. standard component analysis
- Managed vs. unmanaged component tracking
- Security and script usage statistics

## Expected Output

### JSON Structure (Power FX Compatible)
```json
{
  "DVDictionary": {
    "Metadata": {
      "GeneratedOn": "2024-01-01T12:00:00Z",
      "Version": "2.0",
      "Description": "Comprehensive data dictionary with enhanced metadata for Power FX compatibility"
    },
    "Entities": [
      {
        "LogicalName": "account",
        "SchemaName": "Account",
        "DisplayName": "Account",
        "IsCustomEntity": false,
        "IsManaged": false,
        "Fields": [
          {
            "SchemaName": "name",
            "DisplayName": "Account Name",
            "Type": "String",
            "RequiredLevel": "ApplicationRequired",
            "MaxLength": 160,
            "IsCustomAttribute": false,
            "IsPrimaryName": true,
            "FormLocations": [...],
            "OptionSet": [...],
            "ScriptReferences": [...]
          }
        ],
        "OneToManyRelationships": [...],
        "Keys": [...],
        "Privileges": [...]
      }
    ],
    "Summary": {
      "TotalEntities": 5,
      "TotalFields": 250,
      "CustomFields": 15,
      "CalculatedFields": 3,
      "SecuredFields": 8
    }
  }
}
```

### CSV Output
Enhanced CSV with 45+ columns including all metadata properties for detailed analysis in Excel or other tools.

## Prerequisites
- The plugin must be registered and have appropriate permissions to read solution, entity, attribute, form, and web resource metadata.
- The `SolutionNames` parameter must match the unique names of existing solutions in your environment.
- The plugin user must have sufficient privileges to:
  - Read metadata (solution, entity, attribute, form, web resource records)
  - Create annotation (note) records
  - Access system forms for parsing

## Advanced Features

### Form Analysis
The plugin parses systemform XML to extract:
- Form hierarchy (Form → Tab → Section → Field)
- Visibility settings at each level
- Field descriptions and labels
- Control types and configurations

### Script Analysis
JavaScript web resources are analyzed to:
- Identify field references using various patterns
- Detect visibility manipulation (setVisible calls)
- Map script dependencies to fields
- Provide comprehensive script usage reports

### Relationship Mapping
Complete relationship metadata including:
- Cascade configuration details
- Referenced and referencing entities/attributes
- Relationship types and customization status
- Managed vs. unmanaged relationship tracking

## Performance Considerations
- The plugin processes all metadata for specified solutions, which can be substantial for large solutions
- Form parsing and script analysis add processing time but provide valuable insights
- Consider running during off-peak hours for large solutions
- The plugin includes comprehensive error handling to continue processing if individual components fail

## Troubleshooting
- If the plugin fails, check the plugin trace logs for details.
- Ensure the solution names are correct and the plugin user has sufficient privileges.
- For large solutions, verify timeout settings allow adequate processing time.
- Check that system forms are accessible and not corrupted.
- Verify JavaScript web resources are properly encoded (base64).

## Version History
- **v1.0**: Basic field metadata extraction
- **v2.0**: Comprehensive entity metadata, form parsing, script analysis, Power FX compatibility