# DataDictionary Plugin User Guide

## Overview

The DataDictionary Plugin is a C#/.NET Framework 4.6.2 Dataverse plugin that automates the extraction, analysis, and documentation of solution metadata directly within your Dataverse environment. It analyzes entities, attributes, and JavaScript web resources to generate comprehensive data dictionaries that help organizations understand and maintain their Dataverse implementations.

---

## How the DataDictionary Plugin Works

### 1. Solution Processing

- **Input**: One or more solution unique names provided as a parameter
- **Retrieval**: Queries Dataverse for complete solution metadata, including all components (entities, attributes, web resources, etc.)
- **Component Analysis**: Classifies and processes each component according to its type

### 2. Entity and Attribute Metadata Extraction

- **Entities**: Retrieves logical names, schema names, display names, and entity-level metadata
- **Attributes**: Collects field details including data types, constraints, precision, and configuration settings
- **Metadata Correlation**: Builds a comprehensive model of all entities and attributes for documentation

### 3. Web Resource and JavaScript Analysis

- **Web Resource Discovery**: Identifies all JavaScript web resources in the specified solutions
- **Content Analysis**: Decodes and parses JavaScript content from web resources
- **Dependency Mapping**: Analyzes web resource dependencies to understand entity and attribute relationships
- **JavaScript Pattern Detection**: Uses pattern matching to detect Dataverse API usage (`formContext`, `setVisible`, `setRequiredLevel`, etc.)
- **Field Modification Discovery**: Identifies and documents field-level modifications made by JavaScript code

### 4. Correlation and Enrichment

- **Attribute-Web Resource Correlation**: Links JavaScript field modifications and dependencies to the corresponding attribute metadata.
- **Impact Tracking**: Tracks which web resources modify or reference which fields, and annotates attribute metadata with this information.

### 5. Data Persistence

- **Dataverse Upsert**: All extracted and correlated metadata is upserted (created or updated) into custom Dataverse tables:
  - Attribute metadata (`ljr_datadictionaryattributemetadata`)
  - Web resources (`ljr_webresource`)
  - Web resource dependencies (`ljr_webresourcedependency`)
  - JavaScript field modifications (`ljr_javascriptfieldmodification`)
- **Batch Operations**: Uses batch requests for efficient data transfer and to avoid duplicates.

---

## End Results

- **Comprehensive Data Dictionary**: All solution metadata, including entities, attributes, and their relationships to scripts, is stored in Dataverse for reporting or further analysis.
- **JavaScript Impact Analysis**: Field-level changes made by JavaScript are explicitly tracked and linked to both the field and the web resource.
- **Dependency Mapping**: All dependencies between web resources and schema components are recorded.
- **Extensible Model**: The codebase is modular and can be extended to support additional metadata types, output formats, or custom business logic.

---

## Usage Instructions

1. **Configure Connection**: Instantiate `InjestorV2` with a valid `IOrganizationService` connection to your Dataverse environment.
2. **Run Processing**: Call `ProcessSolutions(string[] solutionUniqueNames)` with the unique names of the solutions you wish to analyze.
3. **Review Output**: All results are saved to Dataverse custom tables. Use Advanced Find, Power BI, or custom reports to review the data dictionary, script impacts, and dependencies.

---

## Developer Notes

- **Extensibility**: The code is structured for easy extension. Add new metadata extraction or correlation logic as needed.
- **Regex Patterns**: JavaScript analysis uses regex for pattern matching. Update or extend these patterns to support new API usages.
- **Error Handling**: All Dataverse operations are wrapped in try/catch blocks with console logging for troubleshooting.
- **Batch Size**: Batch upsert size is configurable for performance tuning.

---

## Prerequisites

- .NET Framework 4.6.2
- Microsoft.CrmSdk.XrmTooling.CoreAssembly
- Newtonsoft.Json
- Sufficient Dataverse permissions to read solution, entity, attribute, and web resource metadata, and to write to custom tables.

---

## Troubleshooting

- **No Data Saved**: Ensure the connection is valid and the user has write permissions to the custom tables.
- **Missing Metadata**: Confirm the solution unique names are correct and the solution is published.
- **Script Parsing Issues**: Regex-based JavaScript parsing may need adjustment for custom or advanced script patterns.

---

## Customization Guidance

- **Add Output Formats**: Extend the code to export to JSON, CSV, or Excel as needed.
- **Integrate with CI/CD**: Automate execution as part of your build or deployment pipeline.
- **Enhance Analysis**: Add logic for form layout analysis, business rule extraction, or plugin/workflow impact.

---

## Summary

The DataIngestor provides a robust foundation for solution documentation, impact analysis, and governance in Dataverse environments. It is intended as a starting point for client-specific extensions and can be adapted to meet evolving requirements.