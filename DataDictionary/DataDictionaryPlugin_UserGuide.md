# DataDictionaryPlugin User Guide

## Overview
The DataDictionaryPlugin is a Dynamics 365/Dataverse plugin that generates a comprehensive data dictionary for a specified solution. It collects metadata about entities, fields, and JavaScript web resources, analyzes script references, and outputs the results as a JSON file attached to a Note record in Dataverse.

## How to Use
1. **Deploy the Plugin**: Register the DataDictionaryPlugin in your Dataverse environment using the Plugin Registration Tool or a deployment pipeline.
2. **Trigger the Plugin**: Execute the plugin by providing the required input parameter `SolutionName` (the unique name of your solution) via a custom workflow, plugin step, or direct execution.
3. **Output**: The plugin will generate a JSON file named `DataDictionary.json` and attach it to a Note (annotation) record. The Note's ID will be available in the plugin's output parameters as `NoteId`.

## What the Data Dictionary Contains
- **Entity and Field Metadata**: For each field in the solution, the dictionary includes entity name, schema name, display name, data type, requirement level, description, max length, precision, min/max values, and more.
- **Form Locations**: Lists all forms, tabs, and sections where each field appears, including visibility settings.
- **Script References**: Identifies JavaScript web resources that reference each field.

## Expected Output
- A JSON file with a list of field metadata objects, each containing:
  - EntityName, SchemaName, DisplayName, Type, RequiredLevel, Description
  - MaxLength, Precision, MinValue, MaxValue
  - FormLocations (form, tab, section, visibility)
  - ScriptReferences (web resource names)

## Prerequisites
- The plugin must be registered and have appropriate permissions to read solution, entity, attribute, form, and web resource metadata.
- The `SolutionName` parameter must match the unique name of an existing solution in your environment.

## Notes
- The plugin currently outputs the data dictionary as a JSON file. CSV output is available in the code but not used by default.
- The plugin does not modify any solution components; it only reads metadata and creates a Note record.

## Troubleshooting
- If the plugin fails, check the plugin trace logs for details.
- Ensure the solution name is correct and the plugin user has sufficient privileges.