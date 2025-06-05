# DataDictionary Plugin for Dataverse

## Overview

The **DataDictionary** solution is a C# plugin for Microsoft Dataverse (Dynamics 365) that generates a data dictionary for specified solutions. It retrieves entities, fields, and JavaScript web resources, analyzes script references, and outputs the results as JSON and CSV files attached to Notes in Dataverse.

## How It Works

- The main entry point is `DataDictionaryPlugin` (implements `IPlugin`).
- The plugin expects an input parameter `SolutionNames` (string or string array) specifying the unique names of Dataverse solutions to process.
- For each solution, it:
  - Retrieves all entities and their metadata.
  - Retrieves all fields for those entities.
  - Retrieves all web resources (e.g., JavaScript files).
  - Analyzes which scripts reference which fields.
  - Generates a JSON and CSV data dictionary.
  - Attaches these files as Notes (annotation records) in Dataverse.

## Key Classes

- **DataDictionaryPlugin**: Main plugin logic.
- **WebResourceInfo**: Represents a Dataverse web resource.
- **FieldMetadata**: (Not shown) Represents metadata for a field.
- **FormFieldInspector**: (Not shown) Used to inspect form field visibility and locations.
- **FieldFormLocation**: (Not shown) Represents a field's location on a form.

## Requirements

- **.NET Framework 4.6.2**
- Microsoft Dataverse (Dynamics 365) environment
- Plugin Registration Tool or equivalent for deployment

## Dependencies

- Microsoft.Xrm.Sdk
- Microsoft.Xrm.Sdk.Query
- Microsoft.Xrm.Sdk.Metadata
- Microsoft.Xrm.Sdk.Messages
- Microsoft.Crm.Sdk.Messages

All dependencies are standard for Dataverse/Dynamics 365 plugin development.

## Setup & Usage

1. **Build the Solution**  
   Ensure you are targeting .NET Framework 4.6.2.

2. **Register the Plugin**  
   Use the Plugin Registration Tool to register `DataDictionaryPlugin` in your Dataverse environment.

3. **Configure the Step**  
   - Register the step on a message (e.g., custom action or workflow).
   - Pass the `SolutionNames` input parameter (comma-separated string or string array).

4. **Run the Plugin**  
   - The plugin will generate and attach the data dictionary files as Notes.
   - Check the Plugin Trace Log for detailed execution traces and troubleshooting.

## Troubleshooting

- Ensure the `SolutionNames` parameter is provided.
- Review the Plugin Trace Log for detailed step-by-step traces.
- If Notes are not created, check user permissions and that the plugin is running in the correct context.
- If you see errors about missing entities or web resources, ensure your solutions are published and all referenced components exist.

## Contributing

- Follow standard C# and Dataverse plugin development practices.
- Add new features by extending the plugin or supporting classes.

## License

This solution is provided as-is for internal use in Dataverse environments.