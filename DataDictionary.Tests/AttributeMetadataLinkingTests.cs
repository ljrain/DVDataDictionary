using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataDictionary.Models;

namespace DataDictionary.Tests
{
    [TestClass]
    public class AttributeMetadataLinkingTests
    {
        [TestMethod]
        public void DataDictionaryAttributeMetadata_ShouldHaveAttributeIdProperty()
        {
            // Arrange & Act
            var attributeMetadata = new DataDictionaryAttributeMetadata();
            var testAttributeId = Guid.NewGuid();
            
            // Set the AttributeId
            attributeMetadata.AttributeId = testAttributeId;
            
            // Assert
            Assert.IsNotNull(attributeMetadata.AttributeId, "AttributeId property should exist");
            Assert.AreEqual(testAttributeId, attributeMetadata.AttributeId, "AttributeId should be settable and retrievable");
        }

        [TestMethod]
        public void DataDictionaryAttributeMetadata_AttributeIdShouldBeNullable()
        {
            // Arrange & Act
            var attributeMetadata = new DataDictionaryAttributeMetadata();
            
            // Assert
            Assert.IsNull(attributeMetadata.AttributeId, "AttributeId should be nullable and default to null");
            
            // Test setting to null
            attributeMetadata.AttributeId = null;
            Assert.IsNull(attributeMetadata.AttributeId, "AttributeId should accept null values");
        }

        [TestMethod]
        public void DataDictionaryAttribute_CanBeLinkToMetadataViaAttributeId()
        {
            // Arrange
            var attributeId = Guid.NewGuid();
            
            var attribute = new DataDictionaryAttribute
            {
                AttributeId = attributeId,
                LogicalName = "test_field",
                AttributeName = "Test Field"
            };

            var attributeMetadata = new DataDictionaryAttributeMetadata
            {
                AttributeId = attributeId,
                ColumnLogical = "test_field",
                ColumnDisplay = "Test Field",
                DataType = "String"
            };

            // Act & Assert
            Assert.AreEqual(attribute.AttributeId, attributeMetadata.AttributeId, 
                "DataDictionaryAttribute and DataDictionaryAttributeMetadata should be linkable via AttributeId");
        }

        [TestMethod]
        public void CanCorrelateAttributeMetadataWithSolutionComponent()
        {
            // Arrange
            var componentObjectId = Guid.NewGuid();
            
            // Simulate a solution component for an attribute (ComponentType = 2)
            var solutionComponent = new DataDictionarySolutionComponent
            {
                ObjectId = componentObjectId,
                ComponentType = 2, // 2 = Attribute
                ComponentTypeName = "Attribute"
            };

            // Simulate attribute metadata with the same ID (in real scenario this would be MetadataId from Dataverse)
            var attributeMetadata = new DataDictionaryAttributeMetadata
            {
                AttributeId = componentObjectId, // This should link to the solution component's ObjectId
                ColumnLogical = "test_attribute",
                DataType = "String",
                Table = "test_entity"
            };

            // Act & Assert
            Assert.AreEqual(solutionComponent.ObjectId, attributeMetadata.AttributeId,
                "AttributeMetadata.AttributeId should match SolutionComponent.ObjectId for attributes");
            Assert.AreEqual(2, solutionComponent.ComponentType,
                "Solution component should be of type 2 (Attribute)");
        }

        [TestMethod]
        public void FindAttributeMetadataByAttributeId_ReturnsCorrectMetadata()
        {
            // Arrange
            var targetAttributeId = Guid.NewGuid();
            var otherAttributeId = Guid.NewGuid();
            
            var metadataList = new List<DataDictionaryAttributeMetadata>
            {
                new DataDictionaryAttributeMetadata
                {
                    AttributeId = otherAttributeId,
                    ColumnLogical = "other_field",
                    DataType = "Integer"
                },
                new DataDictionaryAttributeMetadata
                {
                    AttributeId = targetAttributeId,
                    ColumnLogical = "target_field",
                    DataType = "String"
                }
            };

            // Act
            var foundMetadata = metadataList.FirstOrDefault(m => m.AttributeId == targetAttributeId);

            // Assert
            Assert.IsNotNull(foundMetadata, "Should find metadata by AttributeId");
            Assert.AreEqual("target_field", foundMetadata.ColumnLogical, "Should return the correct metadata");
            Assert.AreEqual("String", foundMetadata.DataType, "Should return the correct metadata");
        }
    }
}