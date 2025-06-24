# Data Dictionary Ingestor - Developer Guide

## Overview

The Data Dictionary Ingestor is a refactored and streamlined system for building comprehensive data dictionaries from Microsoft Dataverse solutions. This guide provides detailed information about the architecture, usage, and extensibility of the system.

## Architecture Overview

The system follows the **Separation of Concerns** principle and uses **Dependency Injection** for better testability and maintainability.

### Core Components

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   InjestorV2    │───▶│ SolutionProcessor│    │ EntityProcessor │
│  (Orchestrator) │    │                  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│WebResourceProc. │    │ JavaScriptParser │    │ DataverseService│
│                 │    │                  │    │   (Interface)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

### Key Design Principles

1. **Single Responsibility**: Each class has one specific purpose
2. **Dependency Injection**: All dependencies are injected via constructors
3. **Interface Segregation**: Services implement focused interfaces
4. **Error Handling**: Comprehensive error handling with graceful degradation
5. **Performance Optimization**: Async operations and batch processing

## Components Deep Dive

### 1. InjestorV2 (Orchestrator)

**Purpose**: Coordinates the entire data dictionary building process

**Responsibilities**:
- Orchestrates solution processing workflow
- Manages timing and logging
- Provides public API for consumers

**Usage**:
```csharp
// Using with dependency injection
var injector = new InjestorV2(
    dataverseService,
    solutionProcessor,
    entityProcessor,
    javaScriptParser,
    webResourceProcessor
);

// Using with IOrganizationService (auto-creates dependencies)
var injector = new InjestorV2(organizationService);

// Process solutions
string[] solutions = { "solution1", "solution2" };
injector.ProcessSolutions(solutions);

// Get processing statistics
var stats = injector.GetProcessingStatistics();
```

**Pseudo Code**:
```
FUNCTION ProcessSolutions(solutionNames):
    START timer
    TRY:
        solutions = solutionProcessor.GetSolutions(solutionNames)
        
        FOR EACH solution IN solutions:
            solutionProcessor.PopulateComponentsInSolution(solution)
        END FOR
        
        entityProcessor.ProcessEntities(solutions)
        entityProcessor.ProcessAttributes(solutions)
        webResourceProcessor.ProcessWebResources(solutions)
        
        LogSchemaInformation()
        SaveToDataverse()
        
        STOP timer
        LOG success message
    CATCH exception:
        STOP timer
        LOG error message
        THROW exception
    END TRY
END FUNCTION
```

### 2. SolutionProcessor

**Purpose**: Handles all solution-related operations

**Responsibilities**:
- Retrieve solutions by unique names
- Populate solution components
- Validate solution data

**Key Methods**:
```csharp
Dictionary<string, DataDictionarySolution> GetSolutions(string[] solutionNames)
void PopulateComponentsInSolution(DataDictionarySolution solution)
```

**Pseudo Code**:
```
FUNCTION GetSolutions(solutionNames):
    VALIDATE solutionNames NOT null AND NOT empty
    
    solutions = EMPTY dictionary
    
    query = CREATE QueryExpression("solution")
    query.Criteria = solutionNames IN condition
    
    results = dataverseService.RetrieveMultiple(query)
    
    FOR EACH result IN results:
        solution = CREATE DataDictionarySolution FROM result
        solutions[solution.UniqueName] = solution
        LOG "Retrieved solution: " + solution.FriendlyName
    END FOR
    
    RETURN solutions
END FUNCTION
```

### 3. EntityProcessor

**Purpose**: Processes entities and their attributes

**Responsibilities**:
- Process entity components from solutions
- Process attribute components from solutions
- Map entity relationships

**Key Methods**:
```csharp
void ProcessEntities(Dictionary<string, DataDictionarySolution> solutions)
void ProcessAttributes(Dictionary<string, DataDictionarySolution> solutions)
```

**Pseudo Code**:
```
FUNCTION ProcessEntities(solutions):
    FOR EACH solution IN solutions:
        entityComponents = FILTER solution.Components WHERE ComponentType = 1
        
        FOR EACH component IN entityComponents:
            TRY:
                entity = RetrieveEntityDetails(component.ObjectId)
                solution.AddEntity(entity)
                LOG "Processed entity: " + entity.Name
            CATCH exception:
                LOG "Error processing entity: " + exception.Message
                CONTINUE with next entity
            END TRY
        END FOR
    END FOR
END FUNCTION
```

### 4. JavaScriptParser

**Purpose**: Parses JavaScript content to extract field modifications and API patterns

**Responsibilities**:
- Parse field modification patterns (Hidden, Required, DefaultValue, Disabled)
- Detect API usage patterns
- Extract metadata from JavaScript code

**Key Methods**:
```csharp
List<DataDictionaryJavaScriptFieldModification> ParseFieldModifications(string script, Guid webResourceId, string webResourceName)
List<string> ParseJavaScript(string script, DataDictionaryWebResource webResource = null)
```

**Supported Patterns**:
- `formContext.getControl('field').setVisible(false)` → Hidden
- `formContext.getAttribute('field').setRequiredLevel('required')` → Required
- `formContext.getAttribute('field').setValue(value)` → DefaultValue
- `formContext.getControl('field').setDisabled(true)` → Disabled
- `Xrm.WebApi.*` → API patterns

**Pseudo Code**:
```
FUNCTION ParseFieldModifications(script, webResourceId, webResourceName):
    modifications = EMPTY list
    lines = SPLIT script BY newline
    
    FOR lineIndex, line IN lines:
        FOR EACH pattern IN primaryPatterns:
            matches = pattern.Regex.Matches(line)
            
            FOR EACH match IN matches:
                modification = CREATE DataDictionaryJavaScriptFieldModification
                modification.FieldName = match.Groups[1].Value
                modification.ModificationType = pattern.Type
                modification.LineNumber = lineIndex + 1
                modification.ParsedOn = DateTime.UtcNow
                
                modifications.ADD(modification)
                LOG "Found " + pattern.Type + " for field: " + modification.FieldName
            END FOR
        END FOR
    END FOR
    
    RETURN modifications
END FUNCTION
```

### 5. WebResourceProcessor

**Purpose**: Handles web resource retrieval and processing

**Responsibilities**:
- Retrieve web resources from solutions
- Decode base64 content
- Parse JavaScript content using JavaScriptParser
- Correlate modifications with entity attributes

**Key Methods**:
```csharp
void ProcessWebResources(Dictionary<string, DataDictionarySolution> solutions)
void CorrelateJavaScriptModificationsWithAttributes(Dictionary<string, DataDictionarySolution> solutions)
```

**Pseudo Code**:
```
FUNCTION ProcessWebResources(solutions):
    FOR EACH solution IN solutions:
        webResourceIds = GetWebResourceObjectIds(solution)
        
        IF webResourceIds.Length = 0:
            LOG "No web resources found"
            CONTINUE
        END IF
        
        webResources = GetWebResourcesByObjectIds(webResourceIds)
        processedWebResources = EMPTY list
        
        FOR EACH webResource IN webResources:
            TRY:
                processedWebResource = ProcessWebResource(webResource)
                IF processedWebResource NOT null:
                    processedWebResources.ADD(processedWebResource)
                END IF
            CATCH exception:
                LOG "Error processing web resource: " + exception.Message
                CONTINUE
            END TRY
        END FOR
        
        solution.WebResources = processedWebResources
    END FOR
END FUNCTION
```

### 6. DataverseService

**Purpose**: Abstracts Dataverse operations behind an interface

**Responsibilities**:
- Execute queries against Dataverse
- Perform batch operations
- Handle Dataverse-specific error scenarios

**Interface Definition**:
```csharp
public interface IDataverseService
{
    EntityCollection RetrieveMultiple(QueryBase query);
    void ExecuteBatch(List<OrganizationRequest> requests);
    Guid Create(Entity entity);
    void Update(Entity entity);
    Entity Retrieve(string entityName, Guid id, ColumnSet columnSet);
}
```

## Data Models

### Core Models Hierarchy

```
DataDictionarySolution
├── Components (List<DataDictionarySolutionComponent>)
├── Entities (List<DataDictionaryEntity>)
├── WebResources (List<DataDictionaryWebResource>)
└── AttributeMetadata (List<DataDictionaryAttributeMetadata>)

DataDictionaryWebResource
├── FieldModifications (List<DataDictionaryJavaScriptFieldModification>)
├── ParsedDependencies (List<WebResourceDependency>)
├── ApiPatterns (List<string>)
├── ModifiedAttributes (HashSet<string>)
└── ModifiedTables (HashSet<string>)
```

## Usage Examples

### Basic Usage

```csharp
// Initialize with organization service
using (var serviceClient = new CrmServiceClient(connectionString))
{
    var injector = new InjestorV2(serviceClient);
    
    string[] solutions = { "MySolution", "AnotherSolution" };
    injector.ProcessSolutions(solutions);
    
    // Get results
    var processedSolutions = injector.GetProcessedSolutions();
    var statistics = injector.GetProcessingStatistics();
}
```

### Advanced Usage with Dependency Injection

```csharp
// Setup dependencies
var dataverseService = new DataverseService(organizationService);
var solutionProcessor = new SolutionProcessor(dataverseService);
var entityProcessor = new EntityProcessor(dataverseService);
var javaScriptParser = new JavaScriptParser();
var webResourceProcessor = new WebResourceProcessor(dataverseService, javaScriptParser);

// Create injector with all dependencies
var injector = new InjestorV2(
    dataverseService,
    solutionProcessor,
    entityProcessor,
    javaScriptParser,
    webResourceProcessor
);

// Process solutions
injector.ProcessSolutions(solutionNames);
```

### Metadata-Only Processing

```csharp
// For faster processing when web resources are not needed
injector.ProcessSolutionsMetadataOnly(solutionNames);
```

## Testing

### Unit Testing Approach

The system uses a simple testing framework without external dependencies:

```csharp
// Run all tests
RefactoredIngestorTests.RunAllTests();

// Test specific components
var parser = new JavaScriptParser();
var modifications = parser.ParseFieldModifications(scriptContent, webResourceId, "TestScript");
Assert(modifications.Count > 0, "Should find modifications");
```

### Mock Services

The system includes `MockDataverseService` for testing:

```csharp
var mockService = new MockDataverseService();
var processor = new SolutionProcessor(mockService);

// mockService provides realistic test data
var solutions = processor.GetSolutions(new[] { "test_solution" });
```

## Performance Optimizations

### 1. Batch Processing

- Uses `ExecuteMultiple` for efficient bulk operations
- Configurable batch sizes (default: 100 records)
- Error handling continues processing on individual failures

### 2. Async Operations

- Entity and attribute processing can be done asynchronously
- Web resource processing handles large JavaScript files efficiently

### 3. Memory Management

- Processes solutions individually to manage memory usage
- Clears intermediate collections after processing

### 4. Caching Strategy

- Models use lazy loading for expensive operations
- Computed properties are cached (e.g., `ParsedDependenciesJson`)

## Error Handling Strategy

### 1. Graceful Degradation

```csharp
foreach (var component in components)
{
    try
    {
        ProcessComponent(component);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing component {component.ObjectId}: {ex.Message}");
        // Continue with other components
    }
}
```

### 2. Validation at Entry Points

- All public methods validate input parameters
- Null checks and empty collection checks
- Meaningful exception messages

### 3. Logging Strategy

- Console logging for immediate feedback
- Structured logging with context information
- Performance metrics (timing information)

## Extension Points

### 1. Adding New JavaScript Patterns

```csharp
// Extend JavaScriptParser
public class CustomJavaScriptParser : JavaScriptParser
{
    protected override void InitializePatterns()
    {
        base.InitializePatterns();
        
        // Add custom patterns
        _primaryPatterns.Add(new JavaScriptPattern
        {
            Regex = new Regex(@"customPattern\([""']([^""']+)[""']\)", RegexOptions.IgnoreCase),
            Type = "CustomModification",
            ValueGroup = 1
        });
    }
}
```

### 2. Custom Dataverse Operations

```csharp
// Implement custom service
public class CustomDataverseService : IDataverseService
{
    // Add custom logic, caching, retry mechanisms, etc.
}
```

### 3. Additional Processors

```csharp
// Create new processors following the same pattern
public class CustomProcessor
{
    private readonly IDataverseService _dataverseService;
    
    public CustomProcessor(IDataverseService dataverseService)
    {
        _dataverseService = dataverseService;
    }
    
    // Implement custom processing logic
}
```

## Best Practices

### 1. Resource Management

- Always dispose of IOrganizationService instances
- Use `using` statements for proper cleanup
- Process large datasets in batches

### 2. Error Handling

- Validate inputs at method entry points
- Use specific exception types
- Provide meaningful error messages

### 3. Performance

- Use async methods for I/O operations
- Implement proper batch sizes for bulk operations
- Monitor memory usage for large datasets

### 4. Testing

- Write unit tests for all public methods
- Use mock services for isolated testing
- Test error scenarios and edge cases

## Troubleshooting

### Common Issues

1. **Connection Timeouts**
   - Increase batch sizes
   - Implement retry logic
   - Check network connectivity

2. **Memory Issues**
   - Process solutions individually
   - Implement pagination for large datasets
   - Monitor memory usage

3. **JavaScript Parsing Issues**
   - Verify JavaScript syntax
   - Check for encoded content
   - Review regex patterns for edge cases

### Debug Mode

Enable detailed logging by setting console output verbosity:

```csharp
// Detailed logging for troubleshooting
Console.WriteLine("Debug mode enabled");
injector.ProcessSolutions(solutionNames);
```

## Migration Guide

### From Original InjestorV2

1. **Update References**: Replace direct InjestorV2 usage with new constructor
2. **Handle Dependencies**: Use dependency injection or auto-creation constructor
3. **Update Error Handling**: New error handling is more granular
4. **Test Changes**: Run comprehensive tests after migration

### Example Migration

```csharp
// Old approach
var injector = new InjestorV2(service);
injector.ProcessSolutions(solutions);

// New approach (backward compatible)
var injector = new InjestorV2(service);
injector.ProcessSolutions(solutions);

// Or with dependency injection
var injector = new InjestorV2(
    dataverseService,
    solutionProcessor,
    entityProcessor,
    javaScriptParser,
    webResourceProcessor
);
```

## Conclusion

The refactored Data Dictionary Injestor provides a robust, maintainable, and extensible foundation for building comprehensive data dictionaries from Dataverse solutions. The separation of concerns, comprehensive error handling, and extensive testing make it suitable for production use while remaining easy to extend and customize.