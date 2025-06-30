# AttributeMetadata to Attribute Linking

## Overview

This update adds the ability to link `DataDictionaryAttributeMetadata` objects to their corresponding `DataDictionaryAttribute` objects using the `AttributeId` property.

## How It Works

### 1. AttributeId Property
- Added `AttributeId` property (nullable Guid) to `DataDictionaryAttributeMetadata` class
- This property captures the `MetadataId` from Dataverse `AttributeMetadata` which corresponds to the `AttributeId`

### 2. Solution Component Correlation
- When processing solution components with `ComponentType = 2` (Attribute), the `ObjectId` is the AttributeId
- When processing attribute metadata from Dataverse, the `MetadataId` is captured as `AttributeId`
- These two IDs are the same, enabling correlation

### 3. Usage Example

```csharp
// After processing both solution components and metadata
var injector = new InjestorV2();

// Process solution components (gets attributes with IDs)
await injector.ProcessAttributesAsync();

// Process metadata (gets detailed metadata with same IDs)
injector.LogSchema();

// Correlate them
injector.CorrelateAttributesWithMetadata();
```

### 4. Finding Metadata for an Attribute

```csharp
// Given an attribute from a solution component
var attribute = new DataDictionaryAttribute { AttributeId = someGuid };

// Find corresponding metadata
var metadata = solution.AttributeMetadata
    .FirstOrDefault(m => m.AttributeId == attribute.AttributeId);

if (metadata != null)
{
    // Now you have both the attribute and its rich metadata
    Console.WriteLine($"Attribute {attribute.LogicalName} has data type {metadata.DataType}");
}
```

## Key Benefits

1. **Complete Traceability**: Can now trace from solution components to detailed attribute metadata
2. **Rich Information**: Combine basic attribute info with detailed Dataverse metadata
3. **Easy Correlation**: Simple GUID matching enables fast lookups
4. **Solution-Aware**: Know which attributes belong to which solutions

## Testing

Unit tests are provided in `DataDictionary.Tests/AttributeMetadataLinkingTests.cs` to verify:
- AttributeId property exists and works correctly
- Correlation between attributes and metadata functions properly
- Solution component to metadata linking works as expected