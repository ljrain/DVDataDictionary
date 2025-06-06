using System;
using System.Collections.Generic;
using System.Linq;
using DataDictionary;

namespace DataDictionary.Tests
{
    /// <summary>
    /// Simple tests to verify metadata classes and helper functions work correctly
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DataDictionary Tests Starting...");
            
            try
            {
                TestFieldMetadataCreation();
                TestEntityMetadataCreation();
                TestOptionMetadataCreation();
                TestWebResourceInfoCreation();
                TestFormFieldLocationCreation();
                TestRelationshipMetadataCreation();
                
                Console.WriteLine("All tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void TestFieldMetadataCreation()
        {
            Console.WriteLine("Testing FieldMetadata creation...");
            
            var field = new FieldMetadata
            {
                EntityName = "account",
                SchemaName = "name",
                DisplayName = "Account Name",
                Type = "String",
                RequiredLevel = "ApplicationRequired",
                Description = "The name of the account",
                MaxLength = 100,
                IsCustomAttribute = false,
                IsManaged = false,
                IsPrimaryName = true,
                SourceType = "Standard",
                ScriptReferences = new List<string>(),
                FormLocations = new List<FieldFormLocation>()
            };

            if (field.EntityName != "account")
                throw new Exception("FieldMetadata EntityName not set correctly");
            if (field.IsPrimaryName != true)
                throw new Exception("FieldMetadata IsPrimaryName not set correctly");
                
            Console.WriteLine("✓ FieldMetadata creation test passed");
        }

        static void TestEntityMetadataCreation()
        {
            Console.WriteLine("Testing EntityMetadataInfo creation...");
            
            var entity = new EntityMetadataInfo
            {
                LogicalName = "account",
                SchemaName = "Account",
                DisplayName = "Account",
                IsCustomEntity = false,
                IsManaged = false,
                ObjectTypeCode = 1,
                PrimaryIdAttribute = "accountid",
                PrimaryNameAttribute = "name",
                Fields = new List<FieldMetadata>(),
                OneToManyRelationships = new List<RelationshipMetadata>(),
                ManyToOneRelationships = new List<RelationshipMetadata>(),
                ManyToManyRelationships = new List<RelationshipMetadata>(),
                Keys = new List<EntityKeyMetadata>(),
                Privileges = new List<SecurityPrivilegeMetadata>()
            };

            if (entity.LogicalName != "account")
                throw new Exception("EntityMetadataInfo LogicalName not set correctly");
            if (entity.ObjectTypeCode != 1)
                throw new Exception("EntityMetadataInfo ObjectTypeCode not set correctly");
                
            Console.WriteLine("✓ EntityMetadataInfo creation test passed");
        }

        static void TestOptionMetadataCreation()
        {
            Console.WriteLine("Testing OptionMetadata creation...");
            
            var option = new OptionMetadata
            {
                Value = 1,
                Label = "Active",
                Description = "Active status",
                Color = "#00FF00",
                IsDefault = true
            };

            if (option.Value != 1)
                throw new Exception("OptionMetadata Value not set correctly");
            if (option.Label != "Active")
                throw new Exception("OptionMetadata Label not set correctly");
                
            Console.WriteLine("✓ OptionMetadata creation test passed");
        }

        static void TestWebResourceInfoCreation()
        {
            Console.WriteLine("Testing WebResourceInfo creation...");
            
            var webResource = new WebResourceInfo
            {
                Id = Guid.NewGuid(),
                Name = "test_script.js",
                DisplayName = "Test Script",
                Type = "JavaScript",
                Content = "VGVzdCBjb250ZW50", // Base64 for "Test content"
                SolutionName = "TestSolution"
            };

            if (string.IsNullOrEmpty(webResource.Name))
                throw new Exception("WebResourceInfo Name not set correctly");
            if (webResource.Id == Guid.Empty)
                throw new Exception("WebResourceInfo Id not set correctly");
                
            Console.WriteLine("✓ WebResourceInfo creation test passed");
        }

        static void TestFormFieldLocationCreation()
        {
            Console.WriteLine("Testing FieldFormLocation creation...");
            
            var location = new FieldFormLocation
            {
                FormName = "Main Form",
                TabName = "General",
                TabVisible = true,
                SectionName = "Account Information",
                SectionVisible = true,
                FieldVisible = true,
                FieldName = "name",
                FieldDescription = "Account name field"
            };

            if (location.FormName != "Main Form")
                throw new Exception("FieldFormLocation FormName not set correctly");
            if (!location.FieldVisible)
                throw new Exception("FieldFormLocation FieldVisible not set correctly");
                
            Console.WriteLine("✓ FieldFormLocation creation test passed");
        }

        static void TestRelationshipMetadataCreation()
        {
            Console.WriteLine("Testing RelationshipMetadata creation...");
            
            var relationship = new RelationshipMetadata
            {
                SchemaName = "account_primary_contact",
                RelationshipType = "ManyToOne",
                ReferencedEntity = "contact",
                ReferencedAttribute = "contactid",
                ReferencingEntity = "account",
                ReferencingAttribute = "primarycontactid",
                IsCustomizable = true,
                IsManaged = false,
                CascadeConfiguration = "NoCascade"
            };

            if (relationship.SchemaName != "account_primary_contact")
                throw new Exception("RelationshipMetadata SchemaName not set correctly");
            if (relationship.RelationshipType != "ManyToOne")
                throw new Exception("RelationshipMetadata RelationshipType not set correctly");
                
            Console.WriteLine("✓ RelationshipMetadata creation test passed");
        }
    }
}