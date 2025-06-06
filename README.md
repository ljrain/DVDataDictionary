# DVDataDictionary

A comprehensive Dynamics 365/Dataverse plugin that generates enterprise-grade data dictionaries with extensive metadata analysis for Power Platform solutions.

## Features

### 🔍 Comprehensive Metadata Analysis
- **Entity-Level Metadata**: 70+ properties including ownership, capabilities, audit settings, and security
- **Field-Level Metadata**: 50+ properties covering behavior, validation, customization, and relationships
- **Relationship Mapping**: Complete OneToMany, ManyToOne, and ManyToMany relationship metadata
- **Form Analysis**: Real-time parsing of system forms to extract field locations and visibility
- **Script Analysis**: JavaScript web resource analysis for field references and dynamic behavior
- **Option Set Values**: Complete picklist metadata with labels, descriptions, and colors

### 🚀 Power FX Compatibility
- **DVDictionary JSON Structure**: Optimized for Power Platform consumption
- **Hierarchical Organization**: Logical entity and field grouping
- **Summary Analytics**: Built-in statistics and insights

### 🔧 Advanced Analysis
- **Security Tracking**: Field-level security and entity privilege mapping
- **Customization Detection**: Custom vs. out-of-box component identification
- **Formula Analysis**: Calculated and rollup field detection with formulas
- **Script Impact Analysis**: Identifies fields hidden or modified by JavaScript

## Quick Start

1. **Deploy**: Register the plugin in your Dataverse environment
2. **Execute**: Provide solution names as input parameter
3. **Analyze**: Access comprehensive JSON and CSV outputs via Note records

### Input Parameters
- `SolutionNames`: Array of solution unique names to analyze

### Output
- **JSON File**: Power FX compatible DVDictionary structure
- **CSV File**: Detailed spreadsheet with 45+ metadata columns
- **Note Records**: Files attached to Dataverse annotations for easy access

## Sample Output Structure

```json
{
  "DVDictionary": {
    "Metadata": {
      "GeneratedOn": "2024-01-01T12:00:00Z",
      "Version": "2.0"
    },
    "Entities": [
      {
        "LogicalName": "account",
        "DisplayName": "Account",
        "IsCustomEntity": false,
        "Fields": [
          {
            "SchemaName": "name",
            "DisplayName": "Account Name",
            "Type": "String",
            "RequiredLevel": "ApplicationRequired",
            "IsCustomAttribute": false,
            "FormLocations": [...],
            "ScriptReferences": [...],
            "OptionSet": [...]
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
      "CalculatedFields": 3
    }
  }
}
```

## What's Included

### Entity Metadata
- Core properties (names, descriptions, ownership)
- Capabilities (forms, views, charts, workflows)
- Integration settings (audit, mobile, external channels)
- Visual elements (icons, colors, help URLs)
- Security and access control settings

### Field Metadata
- Data properties (type, length, precision, validation)
- Behavior settings (filterable, searchable, required)
- Security configuration (field-level security)
- Customization status (custom, managed, customizable)
- Advanced features (calculated formulas, rollup definitions)
- Form presence and visibility across all forms
- JavaScript references and dynamic behavior

### Relationships & Structure
- Complete relationship metadata with cascade rules
- Entity keys and alternate keys
- Security privileges and access levels
- Form hierarchy and field placement
- Script dependencies and field interactions

## Documentation

See [DataDictionaryPlugin_UserGuide.md](DataDictionary/DataDictionaryPlugin_UserGuide.md) for detailed usage instructions, prerequisites, and troubleshooting.

## Architecture

Built with enterprise patterns:
- Comprehensive error handling and logging
- Extensible metadata model architecture
- Performance optimized for large solutions
- Power Platform integration ready

## Version 2.0 Highlights

- 🆕 **Complete Entity Metadata**: 70+ entity properties
- 🆕 **Enhanced Field Analysis**: 50+ field properties  
- 🆕 **Real Form Parsing**: XML analysis of system forms
- 🆕 **Script Impact Analysis**: JavaScript field reference detection
- 🆕 **Power FX Structure**: DVDictionary JSON format
- 🆕 **Relationship Mapping**: Complete relationship metadata
- 🆕 **Security Analysis**: Field and entity security settings
- 🆕 **Option Set Details**: Complete picklist metadata
- 🆕 **Summary Analytics**: Built-in statistics and insights

## Contributing

This project provides comprehensive metadata analysis for Dynamics 365/Dataverse solutions. Contributions for additional metadata properties, analysis features, or output formats are welcome.