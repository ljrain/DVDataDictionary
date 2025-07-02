# DataDictionary - Core Library and Plugin

## Overview

**DataDictionary** is the core .NET Framework 4.6.2 library that provides models, plugins, and utilities for extracting and managing Microsoft Dataverse (Dynamics 365) metadata. It serves as the foundation for automated data dictionary generation and metadata analysis.

## Features

- **Dataverse Plugin Architecture**: Implements `IPlugin` for in-environment execution
- **Comprehensive Data Models**: Classes for solutions, entities, attributes, and web resources
- **Metadata Extraction**: Utilities for retrieving and organizing Dataverse metadata
- **JavaScript Analysis**: Core functionality for parsing and analyzing form scripts
- **Multiple Integration Points**: Supports both plugin and external application scenarios

## Architecture

The library is structured around several key components:

### Core Models
- **DataDictionarySolution**: Represents a Dataverse solution and its components
- **DataDictionaryEntity**: Represents an entity (table) with attributes and metadata
- **DataDictionaryAttribute**: Represents field metadata and JavaScript modifications
- **DataDictionaryWebResource**: Represents JavaScript files and their analysis results

### Plugin Implementation
- **DataDictionaryPlugin**: Main plugin entry point implementing `IPlugin`
- **DataDictionaryIngestorPlugin**: Alternative plugin approach for different use cases
- **WebResourceInfo**: Handles web resource metadata and content analysis

### Analysis Components
- **FormFieldInspector**: Analyzes form field visibility and locations
- **FieldMetadata**: Manages field-level metadata and relationships
- **FieldFormLocation**: Tracks field positioning and form relationships

## Plugin Deployment

### Prerequisites
- .NET Framework 4.6.2 or higher
- Microsoft Dataverse environment with plugin registration capabilities
- Plugin Registration Tool or equivalent deployment mechanism

### Installation Steps

1. **Build the Solution**
   ```bash
   # Ensure targeting .NET Framework 4.6.2
   dotnet build DataDictionary.csproj
   ```

2. **Register the Plugin**
   - Use Plugin Registration Tool to register `DataDictionaryPlugin`
   - Configure on appropriate message or custom action
   - Set required input parameters

3. **Configure Execution**
   - Pass `SolutionNames` parameter (comma-separated or array)
   - Ensure proper security context and permissions
   - Configure output handling (Notes attachment)

### Usage

The plugin processes specified solutions and:
- Extracts complete entity and attribute metadata
- Analyzes JavaScript web resources for field modifications
- Correlates script behaviors with field definitions
- Generates JSON and CSV data dictionary files
- Attaches results as Notes in Dataverse

## Integration with DataDictionaryProcessor

This library serves as the foundation for the **DataDictionaryProcessor** console application, providing:
- Shared data models and analysis logic
- Common metadata extraction utilities
- JavaScript parsing and correlation capabilities
- Standardized output formatting

## Dependencies

Core Microsoft Dataverse SDK packages:
- `Microsoft.Xrm.Sdk`
- `Microsoft.Xrm.Sdk.Query`
- `Microsoft.Xrm.Sdk.Metadata`
- `Microsoft.Xrm.Sdk.Messages`
- `Microsoft.Crm.Sdk.Messages`

## Development

### Building from Source
1. Clone the repository
2. Open in Visual Studio 2017 or later
3. Restore NuGet packages
4. Build targeting .NET Framework 4.6.2
5. Reference from consuming applications

### Extending Functionality
- Implement new metadata analyzers by extending base classes
- Add custom JavaScript parsing patterns for specialized use cases
- Create new output formatters for different consumption needs
- Extend plugin functionality with additional message types

## Troubleshooting

### Common Plugin Issues
- **Parameter Validation**: Ensure `SolutionNames` parameter is provided and valid
- **Permissions**: Verify sufficient privileges for metadata access and Notes creation
- **Trace Logs**: Review Plugin Trace Log for detailed execution information
- **Solution Publishing**: Ensure target solutions are published and accessible

### Performance Considerations
- Large solutions may require increased timeout values
- JavaScript analysis can be resource-intensive for complex scripts
- Batch processing may be necessary for solutions with many web resources

## Support

For technical details and advanced configuration:
- [DataDictionaryProcessor README](../DataDictionaryProcessor/README.md) - Console application usage
- [Developer Guide](../DataDictionaryProcessor/developer_guide.md) - Comprehensive technical documentation
- [Architecture Documentation](../docs/) - Detailed design and implementation guidance

---

*This library provides the foundation for automated Dataverse documentation and metadata analysis across the DVDataDictionary solution.*
