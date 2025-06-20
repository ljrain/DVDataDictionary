# DataIngestor

## Overview

**DataIngestor** is a .NET Framework 4.6.2 project for ingesting Microsoft Dataverse (Dynamics 365) metadata, solution components, and web resources. It retrieves solution and entity metadata, decodes and analyzes JavaScript web resources, and saves structured metadata to Dataverse custom tables for reporting or documentation.

## Features

- Retrieve solutions and their components from Dataverse.
- Extract entity and attribute metadata.
- Download and decode JavaScript web resources.
- **Enhanced JavaScript Analysis** - Analyze JavaScript for Dataverse API usage and field modifications including:
  - Fields being hidden (`setVisible(false)`)
  - Fields being marked required (`setRequiredLevel("required")`)
  - Fields having default values set (`setValue(value)`)
  - Fields being disabled (`setDisabled(true)`)
  - Display name changes (`setLabel("text")`)
- Save metadata and web resource content to custom Dataverse tables.
- Correlate JavaScript modifications with field metadata.

## Prerequisites

- .NET Framework 4.6.2
- Visual Studio 2017 or later
- Access to a Microsoft Dataverse (Dynamics 365) environment
- Valid credentials and permissions for Dataverse API

## Build & Usage

1. Clone the repository and open the solution in Visual Studio.
2. Restore NuGet packages.
3. Update connection settings as needed.
4. Build the solution.
5. Run the application, providing the required solution unique names.

### Testing Mode

To run the built-in JavaScript parsing tests:

```bash
DataIngestor.exe test
```

This will validate the JavaScript parsing functionality without connecting to Dataverse.

## Enhanced JavaScript Parsing

The enhanced JavaScript parsing system can detect the following field modifications:

### Supported Patterns

1. **Visibility Control**
   ```javascript
   formContext.getControl('fieldname').setVisible(false);
   Xrm.Page.getControl("fieldname").setVisible(true);
   ```

2. **Required Level Control**
   ```javascript
   formContext.getAttribute('fieldname').setRequiredLevel('required');
   formContext.getAttribute('fieldname').setRequiredLevel('recommended');
   formContext.getAttribute('fieldname').setRequiredLevel('none');
   ```

3. **Default Value Assignment**
   ```javascript
   formContext.getAttribute('fieldname').setValue('some value');
   formContext.getAttribute('fieldname').setValue(123);
   formContext.getAttribute('fieldname').setValue(variable);
   ```

4. **Disabled State Control**
   ```javascript
   formContext.getControl('fieldname').setDisabled(true);
   formContext.getControl('fieldname').setDisabled(false);
   ```

5. **Display Name Changes**
   ```javascript
   formContext.getControl('fieldname').setLabel('New Label');
   ```

### Output

The system generates the following data structures:

- **DataDictionaryJavaScriptFieldModification**: Individual field modifications found in JavaScript
- **Enhanced DataDictionaryAttributeMetadata**: Attribute metadata enriched with JavaScript modification flags
- **Enhanced DataDictionaryWebResource**: Web resources with parsed field modifications

## Data Storage

The enhanced system stores data in the following Dataverse tables:

- `ljr_datadictionaryattributemetadata` - Enhanced with JavaScript modification flags
- `ljr_javascriptfieldmodification` - Individual JavaScript field modifications
- `ljr_webresource` - Web resource content and metadata

## Main Classes

- `InjestorV2`: Main orchestrator for retrieving, processing, and saving metadata and web resources.
- `DataDictionarySolution`, `DataDictionaryWebResource`: Models for solution and web resource data.
- `DataDictionaryJavaScriptFieldModification`: Model for JavaScript field modifications.
- `JavaScriptParsingTests`: Test suite for validating JavaScript parsing functionality.

## Example

```csharp
// Create injector with Dataverse connection
var injector = new InjestorV2(serviceClient);

// Process solutions and analyze JavaScript
string[] solutionNames = { "MySolution", "AnotherSolution" };
injector.ProcessSolutions(solutionNames);

// Results are automatically saved to Dataverse with enhanced JavaScript analysis
```
