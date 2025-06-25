# DataIngestor Developer Guide

This guide provides an overview of the main models in the DataIngestor system, their properties, relationships, and how they are used in the ingestion process.

---

## Model Overview

### DataDictionarySolution
- **Purpose:** Represents a Dataverse solution and its components.
- **Key Properties:**
  - `string UniqueName`
  - `string FriendlyName`
  - `string SolutionId`
  - `List<DataDictionarySolutionComponent> Components`
  - `List<DataDictionaryEntity> Entities`
  - `List<DataDictionaryAttributeMetadata> AttributeMetadata`
  - `List<DataDictionaryWebResource> WebResources`
- **Usage:** Top-level container for all metadata related to a solution.

---

### DataDictionarySolutionComponent
- **Purpose:** Represents a component (entity, attribute, web resource, etc.) within a solution.
- **Key Properties:**
  - `Guid ObjectId`
  - `int ComponentType`
  - `bool IsMetadata`
  - `Guid RootSolutionComponentId`
- **Usage:** Used by `DataDictionarySolution` to track all components.

---

### DataDictionaryEntity
- **Purpose:** Represents a Dataverse entity (table).
- **Key Properties:**
  - `string Name`
  - `string LogicalName`
  - `Guid EntityId`
  - `List<DataDictionaryAttribute> Attributes`
- **Usage:** Contained in `DataDictionarySolution.Entities`.

---

### DataDictionaryAttributeMetadata
- **Purpose:** Represents metadata for an entity attribute (column).
- **Key Properties:**
  - `string Table`
  - `string ColumnLogical`
  - `string DataType`
  - `List<DataDictionaryJavaScriptFieldModification> JavaScriptFieldModifications`
  - `bool? IsHiddenByScript`
  - `bool? IsRequiredByScript`
  - `string ModifyingWebResources`
- **Usage:** Contained in `DataDictionarySolution.AttributeMetadata`. Linked to JavaScript modifications.

---

### DataDictionaryWebResource
- **Purpose:** Represents a web resource (e.g., JavaScript) in Dataverse.
- **Key Properties:**
  - `Guid WebResourceId`
  - `string DisplayName`
  - `string Content`
  - `List<DataDictionaryJavaScriptFieldModification> FieldModifications`
  - `List<WebResourceDependency> ParsedDependencies`
- **Usage:** Contained in `DataDictionarySolution.WebResources`. Tracks JS field modifications and dependencies.

---

### DataDictionaryJavaScriptFieldModification
- **Purpose:** Represents a JavaScript modification applied to a field/control.
- **Key Properties:**
  - `string FieldName`
  - `Guid WebResourceId`
  - `JavaScriptModificationType ModificationType`
  - `string ModificationValue`
  - `DataDictionaryAttributeMetadata ParentAttribute`
- **Usage:** Linked to both `DataDictionaryWebResource` and `DataDictionaryAttributeMetadata`.

---

### WebResourceDependency
- **Purpose:** Represents a dependency between a web resource and an attribute/entity.
- **Key Properties:**
  - `string EntityName`
  - `string AttributeName`
  - `Guid? AttributeId`
- **Usage:** Contained in `DataDictionaryWebResource.ParsedDependencies`.

---

## Model Relationships

The following diagram and description illustrate how the main models relate to each other:

**Legend:**
- `o--` = contains (composition/aggregation)
- `-->` = reference/association

- **DataDictionarySolution**
  - Contains: `List<DataDictionarySolutionComponent> Components`
  - Contains: `List<DataDictionaryEntity> Entities`
  - Contains: `List<DataDictionaryAttributeMetadata> AttributeMetadata`
  - Contains: `List<DataDictionaryWebResource> WebResources`
- **DataDictionaryEntity**
  - Contains: `List<DataDictionaryAttribute> Attributes`
- **DataDictionaryAttributeMetadata**
  - Contains: `List<DataDictionaryJavaScriptFieldModification> JavaScriptFieldModifications`
  - Referenced by: `DataDictionaryJavaScriptFieldModification.ParentAttribute`
- **DataDictionaryWebResource**
  - Contains: `List<DataDictionaryJavaScriptFieldModification> FieldModifications`
  - Contains: `List<WebResourceDependency> ParsedDependencies`
- **DataDictionaryJavaScriptFieldModification**
  - References: `DataDictionaryAttributeMetadata` via `ParentAttribute`
  - References: `DataDictionaryWebResource` via `WebResourceId`
- **WebResourceDependency**
  - Used within: `DataDictionaryWebResource.ParsedDependencies`

---

## Sample Data Table

| Model                                 | Key Properties / Example Values                                                                                   |
|----------------------------------------|------------------------------------------------------------------------------------------------------------------|
| DataDictionarySolution                 | UniqueName: "SampleSolution", FriendlyName: "Sample Solution", SolutionId: "guid", Components: [...], Entities: [...] |
| DataDictionarySolutionComponent        | ObjectId: "guid", ComponentType: 1, IsMetadata: true, RootSolutionComponentId: "guid"                            |
| DataDictionaryEntity                   | Name: "Account", LogicalName: "account", EntityId: "guid", Attributes: [...]                                     |
| DataDictionaryAttributeMetadata        | Table: "account", ColumnLogical: "name", DataType: "String", JavaScriptFieldModifications: [...], IsHiddenByScript: false |
| DataDictionaryWebResource              | WebResourceId: "guid", DisplayName: "account_main.js", Content: "...", FieldModifications: [...], ParsedDependencies: [...] |
| DataDictionaryJavaScriptFieldModification | FieldName: "name", WebResourceId: "guid", ModificationType: "Visibility", ModificationValue: "false", ParentAttribute: ref |
| WebResourceDependency                  | EntityName: "account", AttributeName: "name", AttributeId: "guid"                                                |

---

## Usage Patterns

- **InjestorV2** orchestrates the ingestion process:
  - Loads solutions and their components.
  - Populates entities, attributes, and web resources.
  - Correlates JavaScript field modifications with attribute metadata.
  - Saves all metadata and relationships to Dataverse.

**Example:**
- `DataDictionarySolution` contains all metadata for a solution.
- Each `DataDictionaryEntity` contains its attributes.
- Each `DataDictionaryWebResource` contains parsed JS modifications and dependencies.
- Each `DataDictionaryAttributeMetadata` tracks which JS modifications and web resources affect it.

---

## See Also

- [model-relationships.md](model-relationships.md) for a visual diagram.
- [dataingestor-architecture-review.md](dataingestor-architecture-review.md) for comprehensive architecture review and recommendations.
- [dataingestor-refactoring-examples.md](dataingestor-refactoring-examples.md) for specific code examples and refactoring guidance.
- [dataingestor-review-summary.md](dataingestor-review-summary.md) for executive summary and implementation roadmap.

## Architecture Review (December 2024)

A comprehensive architecture review has been conducted by a senior Dynamics 365 architect. Key findings:

### Strengths
- Excellent domain knowledge and understanding of Dataverse concepts
- Comprehensive metadata extraction capabilities
- Sophisticated JavaScript analysis for field modifications
- Well-designed data models

### Areas for Improvement
- **Monolithic Architecture**: The main InjestorV2 class (1,539 lines) should be broken into focused services
- **Naming Inconsistencies**: Fix "Injestor" misspelling and standardize namespaces
- **Logging**: Replace Console.WriteLine with structured logging
- **Error Handling**: Implement comprehensive error handling and result patterns
- **Testing**: Expand beyond basic JavaScript parsing tests

### Recommended Next Steps
1. **Phase 1 (Weeks 1-2)**: Implement structured logging, clean up project files, fix naming
2. **Phase 2 (Weeks 3-4)**: Extract services, implement dependency injection, add async patterns
3. **Phase 3 (Weeks 5-6)**: Add comprehensive testing, optimize performance, improve configuration
4. **Phase 4 (Weeks 7-8)**: Add monitoring, enhance security, update documentation

See the detailed architecture review documents for complete analysis and implementation guidance.
