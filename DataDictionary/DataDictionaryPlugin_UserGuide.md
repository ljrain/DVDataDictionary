# DataIngestor Proof of Concept Guide

## Overview

The DataIngestor (InjestorV2) is a C#/.NET Framework 4.6.2 proof-of-concept tool for Dynamics 365/Dataverse environments. It automates the extraction, analysis, and correlation of solution metadata—including entities, attributes, and JavaScript web resources—directly from Dataverse. The tool is designed for both technical users and developers, and is intended to be extended or customized by clients for their own data governance, documentation, or impact analysis needs.

---

## How DataIngestor Works

### 1. Solution Processing

- **Input**: One or more solution unique names (string array).
- **Retrieval**: For each solution, the tool queries Dataverse for solution metadata, including all components (entities, attributes, web resources, etc.).
- **Component Analysis**: Each component is classified and processed according to its type (e.g., entity, attribute, web resource).

### 2. Entity and Attribute Metadata Extraction

- **Entities**: For each entity in the solution, the tool retrieves logical names, schema names, display names, data types, and other metadata.
- **Attributes**: For each attribute, it collects details such as logical name, type, min/max values, precision, and audit settings.
- **Metadata Correlation**: The tool builds a comprehensive in-memory model of all entities and attributes, which is later saved to Dataverse.

### 3. Web Resource and JavaScript Analysis

- **Web Resource Discovery**: Identifies all JavaScript web resources in the solution.
- **Content Decoding**: Decodes and parses the JavaScript content from base64.
- **Dependency Parsing**: Analyzes the `DependencyXml` of each web resource to extract referenced entities and attributes.
- **JavaScript Pattern Detection**: Uses regex to detect Dataverse API usage (e.g., `Xrm.Page`, `formContext`, `setVisible`, `setRequiredLevel`, etc.).
- **Field Modification Extraction**: Identifies and records field-level modifications (visibility, required level, default values, etc.) made by JavaScript.

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