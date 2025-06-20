# DataIngestor

## Overview

**DataIngestor** is a .NET Framework 4.6.2 application designed to extract, analyze, and document Microsoft Dataverse (Dynamics 365) metadata, solution components, and web resources. It automates the retrieval of solution and entity metadata, decodes and analyzes JavaScript web resources for field-level modifications, and saves structured metadata to custom Dataverse tables for reporting, auditing, or documentation.

## Architecture

- **Language/Framework:** C# 7.3, .NET Framework 4.6.2
- **Core Libraries:** Microsoft.Xrm.Sdk, Microsoft.Xrm.Tooling.Connector
- **Main Entry Point:** `Program.cs` (console application)
- **Main Orchestrator:** `InjestorV2` class
- **Data Models:** Located in `DataIngestor/Models/`

## Key Components

### 1. Data Models

- **DataDictionarySolution**: Represents a Dataverse solution, containing collections of web resources, attribute metadata, entities, and solution components.
- **DataDictionaryEntity**: Represents a Dataverse entity (table), with a collection of attributes.
- **DataDictionaryAttributeMetadata**: Represents metadata for a Dataverse attribute (column), including type, display name, and JavaScript modification flags.
- **DataDictionaryWebResource**: Represents a web resource (typically JavaScript), including its content and parsed field modifications.
- **DataDictionaryJavaScriptFieldModification**: Represents a single JavaScript-driven field modification (e.g., setVisible, setRequiredLevel, setValue, setDisabled, setLabel).

### 2. Main Processing Flow

- **Initialization**: Connects to Dataverse using client credentials.
- **Solution Retrieval**: Loads solutions and their components (entities, attributes, web resources).
- **Entity & Attribute Metadata Extraction**: Retrieves and stores entity and attribute metadata.
- **Web Resource Download & Parsing**: Downloads JavaScript web resources, decodes content, and parses for Dataverse API usage and field modifications.
- **JavaScript Analysis**: Detects field-level modifications (visibility, required, default value, disabled, label changes) and correlates them with attribute metadata.
- **Data Storage**: Saves all extracted and correlated data to custom Dataverse tables (`ljr_datadictionaryattributemetadata`, `ljr_javascriptfieldmodification`, `ljr_webresource`).

### 3. Relationships

- **Solution → Entities → Attributes**: Hierarchical, with each solution containing entities, and each entity containing attributes.
- **AttributeMetadata ↔ JavaScriptFieldModification**: Bidirectional; each attribute metadata can have multiple related JavaScript field modifications, and each modification references its parent attribute.
- **WebResource → JavaScriptFieldModification**: Each web resource contains a list of parsed field modifications.

## Key Features

- **Automated Metadata Extraction**: Retrieves all relevant solution, entity, and attribute metadata from Dataverse.
- **JavaScript Parsing**: Analyzes JavaScript for field-level modifications using regex-based pattern matching.
- **Correlation of Metadata and JS Modifications**: Links JavaScript-driven field changes to their corresponding attribute metadata.
- **Upsert Logic**: Ensures that data is updated or inserted as needed in Dataverse, preventing duplicates.
- **Batch Operations**: Uses ExecuteMultiple for efficient bulk operations.
- **Testing Support**: Includes a test suite (`JavaScriptParsingTests`) for validating JavaScript parsing logic.

## Data Storage

- **ljr_datadictionaryattributemetadata**: Stores attribute metadata, including JavaScript modification flags.
- **ljr_javascriptfieldmodification**: Stores individual JavaScript field modifications.
- **ljr_webresource**: Stores web resource content and metadata.

## Usage

1. Configure connection settings in `Program.cs`.
2. Run the application, providing solution unique names.
3. The system will connect to Dataverse, extract and analyze metadata, and save results to custom tables.
4. Use the built-in test mode (`DataIngestor.exe test`) to validate JavaScript parsing logic without connecting to Dataverse.

## Extensibility

- **Add New Patterns**: Extend JavaScript parsing by adding new regex patterns in the parsing logic.
- **Custom Data Storage**: Modify or extend the data storage logic to support additional Dataverse tables or fields.
- **Reporting/Export**: Integrate with reporting tools or add export features for documentation.

## Error Handling & Logging

- All major operations include console logging for progress and error reporting.
- Batch operations report individual and aggregate errors.

## Security

- Uses secure client credentials for Dataverse access.
- Sensitive information (client secret) should be protected and not hard-coded in production.

## Limitations

- Only supports .NET Framework 4.6.2 and above.
- Assumes specific custom table schemas in Dataverse.
- JavaScript parsing is regex-based and may not handle all edge cases in complex scripts.

## Example
var injector = new InjestorV2(serviceClient);
string[] solutionNames = { "MySolution", "AnotherSolution" };
injector.ProcessSolutions(solutionNames);
## References

- [Microsoft Dataverse Documentation](https://docs.microsoft.com/en-us/power-apps/developer/data-platform/)
- [XrmTooling SDK](https://docs.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/client-programming)
