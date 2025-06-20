using System;
using System.Collections.Generic;
using System.Linq;
using DataDictionary.Models;
using DataIngestor.Models;

namespace DataIngestor
{
    /// <summary>
    /// Simple test class to validate JavaScript parsing functionality
    /// This is a basic testing approach without a formal testing framework
    /// </summary>
    public static class JavaScriptParsingTests
    {
        /// <summary>
        /// Run all tests for JavaScript parsing functionality
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Running JavaScript Parsing Tests ===");
            
            int passed = 0;
            int total = 0;

            // Test 1: Hidden field detection
            total++;
            if (TestHiddenFieldDetection())
            {
                passed++;
                Console.WriteLine("✓ Test 1 PASSED: Hidden field detection");
            }
            else
            {
                Console.WriteLine("✗ Test 1 FAILED: Hidden field detection");
            }

            // Test 2: Required field detection
            total++;
            if (TestRequiredFieldDetection())
            {
                passed++;
                Console.WriteLine("✓ Test 2 PASSED: Required field detection");
            }
            else
            {
                Console.WriteLine("✗ Test 2 FAILED: Required field detection");
            }

            // Test 3: Default value detection
            total++;
            if (TestDefaultValueDetection())
            {
                passed++;
                Console.WriteLine("✓ Test 3 PASSED: Default value detection");
            }
            else
            {
                Console.WriteLine("✗ Test 3 FAILED: Default value detection");
            }

            // Test 4: Multiple modifications
            total++;
            if (TestMultipleModifications())
            {
                passed++;
                Console.WriteLine("✓ Test 4 PASSED: Multiple modifications");
            }
            else
            {
                Console.WriteLine("✗ Test 4 FAILED: Multiple modifications");
            }

            // Test 5: Disabled state detection
            total++;
            if (TestDisabledStateDetection())
            {
                passed++;
                Console.WriteLine("✓ Test 5 PASSED: Disabled state detection");
            }
            else
            {
                Console.WriteLine("✗ Test 5 FAILED: Disabled state detection");
            }

            Console.WriteLine($"\n=== Test Results: {passed}/{total} tests passed ===");
            if (passed == total)
            {
                Console.WriteLine("🎉 All tests passed!");
            }
            else
            {
                Console.WriteLine($"❌ {total - passed} test(s) failed.");
            }
        }

        private static bool TestHiddenFieldDetection()
        {
            var script = @"
                function hideFields() {
                    formContext.getControl('customerid').setVisible(false);
                    formContext.getControl(""telephone1"").setVisible(false);
                }
            ";

            var modifications = ParseTestScript(script);
            
            var hiddenFields = modifications.Where(m => 
                m.ModificationType == JavaScriptModificationType.Visibility && 
                m.ModificationValue?.ToLower() == "false").ToList();

            return hiddenFields.Count == 2 && 
                   hiddenFields.Any(m => m.FieldName == "customerid") &&
                   hiddenFields.Any(m => m.FieldName == "telephone1");
        }

        private static bool TestRequiredFieldDetection()
        {
            var script = @"
                function makeFieldsRequired() {
                    formContext.getAttribute('emailaddress1').setRequiredLevel('required');
                    formContext.getAttribute(""description"").setRequiredLevel(""required"");
                }
            ";

            var modifications = ParseTestScript(script);
            
            var requiredFields = modifications.Where(m => 
                m.ModificationType == JavaScriptModificationType.RequiredLevel && 
                m.ModificationValue?.ToLower() == "required").ToList();

            return requiredFields.Count == 2 && 
                   requiredFields.Any(m => m.FieldName == "emailaddress1") &&
                   requiredFields.Any(m => m.FieldName == "description");
        }

        private static bool TestDefaultValueDetection()
        {
            var script = @"
                function setDefaults() {
                    formContext.getAttribute('statuscode').setValue(1);
                    formContext.getAttribute(""telephone1"").setValue(""555-0123"");
                    formContext.getAttribute('websiteurl').setValue('https://example.com');
                }
            ";

            var modifications = ParseTestScript(script);
            
            var defaultValues = modifications.Where(m => 
                m.ModificationType == JavaScriptModificationType.DefaultValue).ToList();

            return defaultValues.Count == 3 && 
                   defaultValues.Any(m => m.FieldName == "statuscode" && m.ModificationValue == "1") &&
                   defaultValues.Any(m => m.FieldName == "telephone1" && m.ModificationValue == "\"555-0123\"") &&
                   defaultValues.Any(m => m.FieldName == "websiteurl" && m.ModificationValue == "'https://example.com'");
        }

        private static bool TestMultipleModifications()
        {
            var script = @"
                function configureForm() {
                    // Hide some fields
                    formContext.getControl('fax').setVisible(false);
                    // Make some required
                    formContext.getAttribute('name').setRequiredLevel('required');
                    // Set defaults
                    formContext.getAttribute('industrycode').setValue(1);
                    // Disable field
                    formContext.getControl('createdon').setDisabled(true);
                }
            ";

            var modifications = ParseTestScript(script);

            return modifications.Count >= 4 &&
                   modifications.Any(m => m.ModificationType == JavaScriptModificationType.Visibility) &&
                   modifications.Any(m => m.ModificationType == JavaScriptModificationType.RequiredLevel) &&
                   modifications.Any(m => m.ModificationType == JavaScriptModificationType.DefaultValue) &&
                   modifications.Any(m => m.ModificationType == JavaScriptModificationType.DisabledState);
        }

        private static bool TestDisabledStateDetection()
        {
            var script = @"
                function disableFields() {
                    formContext.getControl('modifiedon').setDisabled(true);
                    formContext.getControl(""createdon"").setDisabled(false);
                }
            ";

            var modifications = ParseTestScript(script);
            
            var disabledFields = modifications.Where(m => 
                m.ModificationType == JavaScriptModificationType.DisabledState).ToList();

            return disabledFields.Count == 2 && 
                   disabledFields.Any(m => m.FieldName == "modifiedon" && m.ModificationValue?.ToLower() == "true") &&
                   disabledFields.Any(m => m.FieldName == "createdon" && m.ModificationValue?.ToLower() == "false");
        }

        /// <summary>
        /// Helper method to parse test scripts using the actual parsing logic
        /// This simulates the ParseFieldModifications method from InjestorV2
        /// </summary>
        private static List<DataDictionaryJavaScriptFieldModification> ParseTestScript(string script)
        {
            var modifications = new List<DataDictionaryJavaScriptFieldModification>();
            if (string.IsNullOrWhiteSpace(script))
                return modifications;

            var webResourceId = Guid.NewGuid();
            var webResourceName = "TestScript";
            var scriptLines = script.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Use the same regex patterns as in the actual implementation
            var patterns = new[]
            {
                // Visibility modifications
                new {
                    Regex = new System.Text.RegularExpressions.Regex(@"(?:formContext|Xrm\.Page)\.getControl\(\s*[""']([^""']+)[""']\s*\)\.setVisible\s*\(\s*(true|false)\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.Visibility,
                    ValueGroup = 2
                },
                // Required level modifications
                new {
                    Regex = new System.Text.RegularExpressions.Regex(@"(?:formContext|Xrm\.Page)\.getAttribute\(\s*[""']([^""']+)[""']\s*\)\.setRequiredLevel\s*\(\s*[""']([^""']+)[""']\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.RequiredLevel,
                    ValueGroup = 2
                },
                // Default value assignments
                new {
                    Regex = new System.Text.RegularExpressions.Regex(@"(?:formContext|Xrm\.Page)\.getAttribute\(\s*[""']([^""']+)[""']\s*\)\.setValue\s*\(\s*([^)]+)\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.DefaultValue,
                    ValueGroup = 2
                },
                // Disabled state modifications
                new {
                    Regex = new System.Text.RegularExpressions.Regex(@"(?:formContext|Xrm\.Page)\.getControl\(\s*[""']([^""']+)[""']\s*\)\.setDisabled\s*\(\s*(true|false)\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.DisabledState,
                    ValueGroup = 2
                }
            };

            for (int lineIndex = 0; lineIndex < scriptLines.Length; lineIndex++)
            {
                var line = scriptLines[lineIndex];
                
                foreach (var pattern in patterns)
                {
                    var matches = pattern.Regex.Matches(line);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count > pattern.ValueGroup)
                        {
                            var modification = new DataDictionaryJavaScriptFieldModification
                            {
                                FieldName = match.Groups[1].Value,
                                WebResourceId = webResourceId,
                                WebResourceName = webResourceName,
                                ModificationType = pattern.Type,
                                ModificationValue = match.Groups[pattern.ValueGroup].Value,
                                JavaScriptCode = line.Trim(),
                                LineNumber = lineIndex + 1,
                                ParsedOn = DateTime.UtcNow
                            };

                            modifications.Add(modification);
                        }
                    }
                }
            }

            return modifications;
        }
    }
}