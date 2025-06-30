# DataDictionaryProcessor Developer Guide

> **Disclaimer:**  
> This software and its documentation are provided as a proof of concept for demonstration and reference purposes only. No warranty, express or implied, is provided. Use at your own risk.

## Overview

This repository contains three related projects—**DataDictionary**, **DataDictionaryProcessor**, and **DataIngestor**—which are all works in progress toward building a comprehensive data dictionary solution for Microsoft Dataverse (Dynamics 365). The current implementation uses a console application (**DataDictionaryProcessor**) to automate data dictionary generation. However, the long-term goal is to enable this functionality to be imported as a solution and executed directly from within a Power App, providing a more integrated and user-friendly experience.

The **DataDictionaryProcessor** is a console application within the DVDataDictionary solution that generates comprehensive data dictionaries for Microsoft Dataverse (Dynamics 365) solutions. It extracts metadata about entities, attributes, and web resources, analyzes JavaScript code for field modifications, and correlates this information to create a unified data dictionary.

### Context within DVDataDictionary Solution

The DataDictionaryProcessor serves as one of the key components in the DVDataDictionary ecosystem:

- **DataDictionary**: Core library with models and plugin functionality
- **DataDictionaryProcessor**: Console application for automated data dictionary generation
- **DataIngestor**: Alternative approach for data ingestion (legacy)

The processor is designed to be run as a standalone console application or integrated into automated workflows for documentation generation.

## Main Responsibilities

The DataDictionaryProcessor has four primary responsibilities:

1. **Metadata Collection**: Retrieves comprehensive metadata from Dataverse including solutions, entities, attributes, and web resources
2. **JavaScript Analysis**: Parses JavaScript web resources to identify field modifications, visibility changes, and business logic
3. **Data Correlation**: Links JavaScript modifications with their corresponding entity attributes and metadata
4. **Data Dictionary Generation**: Processes and outputs the correlated data into a structured data dictionary format

## Typical Workflow

The processor follows a well-defined workflow:
graph TD
    A[Program.cs] --> B[Load Configuration]
    B --> C[Create DictionaryOrchestrator]
    C --> D[BuildDataDictionary]
    D --> E[DvCollector.CollectData]
    E --> F[DvProcessor.ProcessData]
    F --> G[Output Results]
    
    E --> E1[Get Solutions]
    E --> E2[Get Components]
    E --> E3[Get Entities]
    E --> E4[Get Attributes]
    E --> E5[Get Web Resources]
    
    F --> F1[Process Metadata]
    F --> F2[Parse JavaScript]
    F --> F3[Correlate Data]
    F --> F4[Build Dictionary]
### Detailed Workflow Steps

1. **Initialization**
   - Load connection configuration from `appsettings.json`
   - Establish Dataverse connection
   - Create orchestrator instance

2. **Data Collection** (DvCollector)
   - Retrieve solution information
   - Get solution components
   - Extract entity metadata
   - Collect attribute details
   - Gather web resource content

3. **Data Processing** (DvProcessor)
   - Parse JavaScript web resources for field modifications
   - Correlate modifications with attribute metadata
   - Build unified data dictionary model
   - Generate output

## Key Components

### Program.cs

**Purpose**: Application entry point and configuration management

**Key Responsibilities**:
- Loads configuration from `appsettings.json`
- Constructs Dataverse connection string
- Initializes and executes the DictionaryOrchestrator
- Handles top-level exception management

**Configuration Structure**:{
  "CRMURL": "https://your-environment.crm.dynamics.com",
  "CLIENTID": "your-client-id",
  "CLIENTSECRET": "your-client-secret",
  "TENANTID": "your-tenant-id"
}
**Key Methods**:
- `Main(string[] args)`: Application entry point

### DictionaryOrchestrator.cs

**Purpose**: Main workflow coordinator that orchestrates the entire data dictionary building process

**Key Responsibilities**:
- Coordinates data collection and processing phases
- Manages the overall workflow sequence
- Provides high-level error handling and logging

**Workflow Steps**:
1. Collect all metadata and store in models
2. Parse JavaScript files and extract relevant data
3. Correlate metadata with parsed data
4. Save to Dataverse (future enhancement)

**Key Methods**:
- `BuildDataDictionary()`: Main orchestration method

### DvCollector.cs

**Purpose**: Dataverse metadata collection service

**Key Responsibilities**:
- Retrieves solution information from Dataverse
- Collects solution components (entities, attributes, web resources)
- Extracts detailed metadata for each component type
- Maintains allowed lists for filtering relevant data

**Key Properties**:
- `AllowedLogicalNames`: List of entity logical names to process
- `AllowedTableAttributes`: Dictionary of table-specific attributes to include
- `DDSolutions`: Dictionary of collected solution data

**Key Methods**:
- `CollectData()`: Main collection orchestrator
- `GetSolutions(string[] solutionNames)`: Retrieves solution records
- `GetComponentsInSolution(DataDictionarySolution)`: Gets components for a solution
- `LogSchema()`: Retrieves comprehensive entity and attribute metadata

### DvProcessor.cs

**Purpose**: Data processing and correlation engine

**Key Responsibilities**:
- Processes collected metadata into structured data dictionary format
- Applies business logic to attribute metadata
- Correlates JavaScript modifications with attributes
- Generates final output and reporting

**Data Structure**:DataDictionary
├── Entity[1..*]
│   ├── EntityName: string
│   ├── EntityDescription: string
│   ├── EntityType: string
│   ├── Metadata: { key: value, ... }
│   │
│   └── Attribute[1..*]
│       ├── AttributeName: string
│       ├── DataType: string
│       ├── IsNullable: boolean
│       ├── DefaultValue: any
│       ├── Metadata: { key: value, ... }
│       └── ActionFunction: JavaScriptFunctionReference
**Key Methods**:
- `ProcessData(Dictionary<string, DataDictionarySolution>)`: Main processing method
- `PrintDataDictionary()`: Outputs summary information

### DvJavaScriptParser.cs

**Purpose**: JavaScript analysis and field modification detection

**Key Responsibilities**:
- Parses JavaScript web resources (handles base64 encoding)
- Identifies field modification patterns using regular expressions
- Extracts visibility, required level, default value, and other modifications
- Links modifications to specific web resources and line numbers

**Supported Modification Types**:
- **Visibility**: `setVisible(true/false)`
- **Required Level**: `setRequiredLevel("required"/"recommended"/"none")`
- **Default Value**: `setValue(value)`
- **Disabled State**: `setDisabled(true/false)`
- **Display Name**: `setLabel("text")`

**Pattern Recognition**:
- Supports both `formContext` and legacy `Xrm.Page` patterns
- Handles conditional modifications and variable-based values
- Provides line-level traceability

**Key Methods**:
- `ParseFieldModifications(string script, Guid webResourceId, string webResourceName)`: Main parsing method

### Models Directory

The Models directory contains the data structures that represent the data dictionary:

#### Core Models

**DataDictionary** (`DataDictionaryModel.cs`):
- Root container for the entire data dictionary
- Contains collections of solutions and entities

**DataDictionaryEntity** (`DataDictionaryEntity.cs`):
- Represents a Dataverse entity/table
- Contains entity metadata and attribute collections
- Provides methods for attribute management

**DataDictionaryAttribute** (`DataDictionaryAttribute.cs`):
- Represents an entity attribute/field
- Contains attribute metadata and configuration

**DataDictionaryAttributeMetadata** (`DataDictionaryAttributeMetadata.cs`):
- Detailed metadata for attributes
- Links to JavaScript modifications

#### Solution and Component Models

**DataDictionarySolution** (`DataDictionarySolution.cs`):
- Represents a Dataverse solution
- Contains solution components and metadata

**DataDictionarySolutionComponent** (`DataDictionarySolutionComponent.cs`):
- Represents individual solution components
- Links components to their parent solutions

#### JavaScript and Web Resource Models

**DataDictionaryWebResource** (`DataDictionaryWebResource.cs`):
- Represents JavaScript web resources
- Contains parsed field modifications and dependencies

**DataDictionaryJavaScriptFieldModification** (`DataDictionaryJavaScriptFieldModification.cs`):
- Represents specific field modifications found in JavaScript
- Links modifications to web resources and attributes

**WebResourceDependency** (`WebResourceDependency.cs`):
- Represents dependencies between web resources

### Configuration Files

#### appsettings.json

Contains Dataverse connection parameters:
- **CRMURL**: Dataverse environment URL
- **CLIENTID**: Azure AD application client ID
- **CLIENTSECRET**: Azure AD application client secret
- **TENANTID**: Azure AD tenant ID

#### App.config

Standard .NET Framework configuration file containing:
- Runtime binding redirects
- Framework version targeting
- Assembly resolution settings

#### packages.config

NuGet package dependencies including:
- Microsoft Dataverse SDK packages
- Microsoft Extensions Configuration
- JSON processing libraries

### Note.md

Contains detailed pseudocode documentation of the original DataIngestor workflow. This file serves as:
- Reference implementation guide
- Workflow documentation
- Historical context for the processing logic

## Step-by-Step Modification and Extension Instructions

### How to Change Data Extraction Logic

1. **Modify Solution Selection**:// In DvCollector.cs, update GetSolutions method
   GetSolutions(new string[] { "YourSolutionName", "AnotherSolution" });
2. **Filter Entities**:// Add logic in DvCollector.CollectData() to filter entities
   if (allowedEntityTypes.Contains(entity.LogicalName))
   {
       // Process entity
   }
3. **Customize Attribute Collection**:// In DvCollector.LogSchema(), modify attribute filtering
   if (attributeMetadata.AttributeType == AttributeTypeCode.String)
   {
       // Custom processing for string attributes
   }
### How to Support New Metadata Types

1. **Extend Data Models**:// Add new properties to existing models
   public class DataDictionaryAttributeMetadata
   {
       // Existing properties...
       public string NewMetadataProperty { get; set; }
   }
2. **Update Collection Logic**:// In DvCollector.cs, extend metadata collection
   ddAttr.NewMetadataProperty = attributeMetadata.SomeNewProperty;
3. **Modify Processing**:// In DvProcessor.cs, handle new metadata
   ProcessNewMetadataType(attribute.NewMetadataProperty);
### How to Enhance Output

1. **Add New Output Formats**:// Create new method in DvProcessor.cs
   public void ExportToXml()
   {
       // XML export logic
   }
   
   public void ExportToExcel()
   {
       // Excel export logic using EPPlus or similar
   }
2. **Enhance Console Output**:// Modify PrintDataDictionary() in DvProcessor.cs
   Console.WriteLine($"Custom Report: {customCalculation}");
3. **Add File Export**:// Add to DvProcessor.cs
   public void SaveToFile(string filePath)
   {
       var json = JsonConvert.SerializeObject(_ddModel, Formatting.Indented);
       File.WriteAllText(filePath, json);
   }
### How to Parse Additional Script Types

1. **Extend Pattern Recognition**:// Add new patterns to DvJavaScriptParser.cs
var newPatterns = new[]
{
    new {
        Regex = new Regex(@"your-new-pattern", RegexOptions.IgnoreCase),
        Type = JavaScriptModificationType.NewType,
        ValueGroup = 2
    }
   };
2. **Add New Modification Types**:// Extend enum in DataDictionaryJavaScriptFieldModification.cs
   public enum JavaScriptModificationType
   {
       Visibility,
       RequiredLevel,
       DefaultValue,
       DisabledState,
       DisplayName,
       NewModificationType, // Add new type
       Other
   }
3. **Handle New Script Frameworks**:// Add support for React, Angular, etc.
public List<DataDictionaryJavaScriptFieldModification> ParseReactComponents(string script)
{
    // React-specific parsing logic
}
### How to Update Workflow

1. **Add Pre-processing Steps**:// In DictionaryOrchestrator.BuildDataDictionary()
public void BuildDataDictionary()
{
    // Add new step before existing workflow
    PreprocessData();
    
    // Existing workflow...
    DvCollector collector = new DvCollector(_serviceClient);
    collector.CollectData();
}
2. **Add Post-processing**:// Add after existing processing
   processor.PrintDataDictionary();
   
   // New post-processing steps
   PostProcessResults();
   GenerateReports();
   NotifyStakeholders();
3. **Add Parallel Processing**:// Use async/await for parallel operations
public async Task BuildDataDictionaryAsync()
{
    var collectionTask = collector.CollectDataAsync();
    var processingTask = processor.ProcessDataAsync();
    
    await Task.WhenAll(collectionTask, processingTask);
   }
### How to Update Configuration

1. **Add New Configuration Settings**:// In appsettings.json
{
    "CRMURL": "...",
    "CLIENTID": "...",
    "CLIENTSECRET": "...",
    "TENANTID": "...",
    "OutputPath": "C:\\DataDictionary\\Output",
    "MaxRetries": 3,
       "BatchSize": 100
   }
2. **Update Configuration Loading**:// In Program.cs
   string outputPath = configuration["OutputPath"];
   int maxRetries = int.Parse(configuration["MaxRetries"] ?? "3");
   int batchSize = int.Parse(configuration["BatchSize"] ?? "100");
3. **Create Configuration Classes**:// Create new configuration model
public class ProcessorConfiguration
{
    public string OutputPath { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int BatchSize { get; set; } = 100;
       public string[] SolutionNames { get; set; }
   }
## Typical Development Workflow

### 1. Clone Repository
git clone https://github.com/ljrain/DVDataDictionary.git
cd DVDataDictionary/DataDictionaryProcessor
### 2. Build Project

**Prerequisites**:
- .NET Framework 4.6.2 Developer Pack
- Visual Studio 2017 or later
- NuGet Package Manager

**Build Steps**:# Restore NuGet packages
nuget restore DataDictionaryProcessor.sln

# Build the solution
msbuild DataDictionaryProcessor.sln /p:Configuration=Release
**Alternative using Visual Studio**:
1. Open `DataDictionaryProcessor.sln`
2. Build → Rebuild Solution

### 3. Test Changes

#### Unit Testing Setup

1. **Create Test Project**:<!-- Add to solution -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net462</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.13.3" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.2.1" />
    <PackageReference Include="Moq" Version="4.18.4" />
     </ItemGroup>
   </Project>
2. **Create Sample Tests**:[TestFixture]
public class DvJavaScriptParserTests
{
    [Test]
    public void ParseFieldModifications_VisibilityModification_ReturnsCorrectModification()
    {
        // Arrange
        var parser = new DvJavaScriptParser(null, null, null, null);
           var script = "formContext.getControl('customerid').setVisible(false);";
           
           // Act
           var result = parser.ParseFieldModifications(script, Guid.NewGuid(), "test.js");
           
           // Assert
           Assert.AreEqual(1, result.Count);
           Assert.AreEqual("customerid", result[0].FieldName);
           Assert.AreEqual(JavaScriptModificationType.Visibility, result[0].ModificationType);
       }
   }
#### Integration Testing

1. **Test Environment Setup**:
   - Configure test Dataverse environment
   - Create test `appsettings.json` with test environment credentials
   - Set up test solutions with known entities and web resources

2. **Run Integration Tests**:[TestFixture]
[Category("Integration")]
public class DvCollectorIntegrationTests
{
    [Test]
    public void CollectData_ValidSolution_ReturnsMetadata()
    {
        // Test against real Dataverse environment
       }
   }
#### Manual Testing

1. **Configure Test Environment**:// appsettings.json for testing
   {
       "CRMURL": "https://test-env.crm.dynamics.com",
       "CLIENTID": "test-client-id",
       "CLIENTSECRET": "test-secret",
       "TENANTID": "test-tenant-id"
   }
2. **Run and Verify**:# Run the application
   DataDictionaryProcessor.exe
   
   # Verify console output
   # Check generated data dictionary content
   # Validate against known test data
### 4. Document Changes

#### Code Documentation

1. **Update XML Documentation**:/// <summary>
/// Parses JavaScript code to identify field modifications
/// </summary>
/// <param name="script">The JavaScript code to parse</param>
/// <param name="webResourceId">The ID of the web resource</param>
/// <param name="webResourceName">The name of the web resource</param>
/// <returns>List of field modifications found in the script</returns>
   public List<DataDictionaryJavaScriptFieldModification> ParseFieldModifications(
       string script, Guid webResourceId, string webResourceName)
2. **Update README Files**:
   - Document new features in project README
   - Update configuration examples
   - Add troubleshooting information

3. **Update This Developer Guide**:
   - Add new component explanations
   - Update workflow diagrams
   - Document new extension points

#### Change Documentation

1. **Create CHANGELOG.md**:# Changelog

## [1.1.0] - 2024-01-XX
### Added
- New script parsing patterns for React components
- Excel export functionality
- Parallel processing support

### Changed
- Improved error handling in DvCollector
- Enhanced console output formatting

   ### Fixed
   - Issue with base64 script decoding
### 5. Push Changes

#### Git Workflow

1. **Create Feature Branch**:git checkout -b feature/new-script-parser
2. **Commit Changes**:git add .
   git commit -m "Add React component parsing support
   
   - Added new regex patterns for React field modifications
   - Updated JavaScriptModificationType enum
   - Added unit tests for React parsing
   - Updated documentation"
3. **Push and Create Pull Request**:git push origin feature/new-script-parser
   # Create pull request through GitHub interface
#### Code Review Checklist

- [ ] Code follows existing patterns and conventions
- [ ] New functionality has appropriate unit tests
- [ ] Integration tests pass
- [ ] Documentation is updated
- [ ] Breaking changes are documented
- [ ] Performance impact is considered
- [ ] Security implications are reviewed

## Relevant Links

### Core Files

- [Program.cs](./Program.cs) - Application entry point
- [DictionaryOrchestrator.cs](./DictionaryOrchestrator.cs) - Main workflow coordinator
- [DvCollector.cs](./DvCollector.cs) - Dataverse metadata collection
- [DvProcessor.cs](./DvProcessor.cs) - Data processing and correlation
- [DvJavaScriptParser.cs](./DvJavaScriptParser.cs) - JavaScript analysis
- [Models Directory](./Models/) - Data model definitions
- [appsettings.json](./appsettings.json) - Configuration file
- [Note.md](./Note.md) - Workflow pseudocode documentation

### Related Documentation

- [Project README](../README.md) - Overview of the entire DVDataDictionary solution
- [DataIngestor Architecture Review](../docs/dataingestor-architecture-review.md) - Architectural analysis and recommendations
- [Developer Guide](../docs/developer-guide.md) - General development guide for the solution
- [Model Relationships](../docs/model-relationships.md) - Data model relationship diagrams
- [Attribute Metadata Relationship Plan](../docs/attribute-metadata-relationship-plan.md) - Detailed plan to resolve AttributeMetadata to Attribute relationship issues
- [Dataverse Attribute Relationship Explained](../docs/dataverse-attribute-relationship-explained.md) - Technical explanation of the relationship between solution components and metadata

### External Resources

- [Microsoft Dataverse SDK Documentation](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/)
- [.NET Framework 4.6.2 Documentation](https://docs.microsoft.com/en-us/dotnet/framework/)
- [Azure AD Application Registration](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)

---

*This developer guide is maintained as part of the DVDataDictionary project. For questions or contributions, please refer to the project's contribution guidelines and issue tracker.*