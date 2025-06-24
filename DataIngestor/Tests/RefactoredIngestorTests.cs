using DataDictionary.Models;
using DataIngestor.Processors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataIngestor.Tests
{
    /// <summary>
    /// Comprehensive test suite for the refactored Data Dictionary components
    /// Uses a simple testing approach without external frameworks
    /// </summary>
    public static class RefactoredIngestorTests
    {
        /// <summary>
        /// Runs all tests for the refactored components
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Running Refactored Ingestor Tests ===");
            
            int passed = 0;
            int total = 0;

            // Test JavaScriptParser
            passed += RunJavaScriptParserTests(ref total);

            // Test SolutionProcessor
            passed += RunSolutionProcessorTests(ref total);

            // Test EntityProcessor
            passed += RunEntityProcessorTests(ref total);

            // Test WebResourceProcessor
            passed += RunWebResourceProcessorTests(ref total);

            // Test Integration scenarios
            passed += RunIntegrationTests(ref total);

            // Summary
            Console.WriteLine("=== Test Results ===");
            Console.WriteLine($"Total Tests: {total}");
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {total - passed}");
            
            if (passed == total)
            {
                Console.WriteLine("✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"❌ {total - passed} test(s) failed.");
            }
        }

        #region JavaScriptParser Tests
        private static int RunJavaScriptParserTests(ref int total)
        {
            Console.WriteLine("\n--- JavaScriptParser Tests ---");
            int localPassed = 0;

            // Test 1: Field modification parsing
            total++;
            if (TestFieldModificationParsing())
            {
                localPassed++;
                Console.WriteLine("✓ Field modification parsing test PASSED");
            }
            else
            {
                Console.WriteLine("✗ Field modification parsing test FAILED");
            }

            // Test 2: API pattern detection
            total++;
            if (TestApiPatternDetection())
            {
                localPassed++;
                Console.WriteLine("✓ API pattern detection test PASSED");
            }
            else
            {
                Console.WriteLine("✗ API pattern detection test FAILED");
            }

            // Test 3: Invalid JavaScript handling
            total++;
            if (TestInvalidJavaScriptHandling())
            {
                localPassed++;
                Console.WriteLine("✓ Invalid JavaScript handling test PASSED");
            }
            else
            {
                Console.WriteLine("✗ Invalid JavaScript handling test FAILED");
            }

            // Test 4: Complex JavaScript parsing
            total++;
            if (TestComplexJavaScriptParsing())
            {
                localPassed++;
                Console.WriteLine("✓ Complex JavaScript parsing test PASSED");
            }
            else
            {
                Console.WriteLine("✗ Complex JavaScript parsing test FAILED");
            }

            return localPassed;
        }

        private static bool TestFieldModificationParsing()
        {
            try
            {
                var parser = new JavaScriptParser();
                var script = @"
                    formContext.getControl('firstname').setVisible(false);
                    formContext.getAttribute('lastname').setRequiredLevel('required');
                    formContext.getAttribute('websiteurl').setValue('https://example.com');
                    formContext.getControl('telephone1').setDisabled(true);
                ";

                var webResourceId = Guid.NewGuid();
                var modifications = parser.ParseFieldModifications(script, webResourceId, "TestWebResource");

                return modifications.Count == 4 &&
                       modifications.Any(m => m.FieldName == "firstname" && m.ModificationType == "Hidden") &&
                       modifications.Any(m => m.FieldName == "lastname" && m.ModificationType == "Required") &&
                       modifications.Any(m => m.FieldName == "websiteurl" && m.ModificationType == "DefaultValue") &&
                       modifications.Any(m => m.FieldName == "telephone1" && m.ModificationType == "Disabled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestFieldModificationParsing error: {ex.Message}");
                return false;
            }
        }

        private static bool TestApiPatternDetection()
        {
            try
            {
                var parser = new JavaScriptParser();
                var script = @"
                    Xrm.WebApi.retrieveRecord('account', accountId, '?$select=name');
                    Xrm.WebApi.createRecord('contact', data);
                    Xrm.WebApi.updateRecord('lead', leadId, updateData);
                ";

                var patterns = parser.ParseJavaScript(script);

                return patterns.Count == 3 &&
                       patterns.Contains(@"Xrm\.WebApi\.retrieveRecord") &&
                       patterns.Contains(@"Xrm\.WebApi\.createRecord") &&
                       patterns.Contains(@"Xrm\.WebApi\.updateRecord");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestApiPatternDetection error: {ex.Message}");
                return false;
            }
        }

        private static bool TestInvalidJavaScriptHandling()
        {
            try
            {
                var parser = new JavaScriptParser();
                
                // Test with null/empty script
                var modifications1 = parser.ParseFieldModifications(null, Guid.NewGuid(), "Test");
                var modifications2 = parser.ParseFieldModifications("", Guid.NewGuid(), "Test");
                var patterns1 = parser.ParseJavaScript(null);
                var patterns2 = parser.ParseJavaScript("");

                return modifications1.Count == 0 &&
                       modifications2.Count == 0 &&
                       patterns1.Count == 0 &&
                       patterns2.Count == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestInvalidJavaScriptHandling error: {ex.Message}");
                return false;
            }
        }

        private static bool TestComplexJavaScriptParsing()
        {
            try
            {
                var parser = new JavaScriptParser();
                var script = @"
                    // Complex script with multiple patterns
                    function onLoad(executionContext) {
                        var formContext = executionContext.getFormContext();
                        
                        // Hide some fields conditionally
                        if (someCondition) {
                            formContext.getControl('field1').setVisible(false);
                            formContext.getControl('field2').setVisible(false);
                        }
                        
                        // Set required fields
                        formContext.getAttribute('requiredfield').setRequiredLevel('required');
                        
                        // API calls
                        Xrm.WebApi.retrieveRecord('account', accountId, '?$select=name').then(
                            function success(result) {
                                formContext.getAttribute('accountname').setValue(result.name);
                            }
                        );
                    }
                ";

                var webResource = new DataDictionaryWebResource
                {
                    WebResourceId = Guid.NewGuid(),
                    DisplayName = "ComplexTestScript"
                };

                var patterns = parser.ParseJavaScript(script, webResource);

                return webResource.FieldModifications.Count >= 3 &&
                       patterns.Count >= 1 &&
                       webResource.ModifiedAttributes.Count >= 3;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestComplexJavaScriptParsing error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Mock Service Tests
        private static int RunSolutionProcessorTests(ref int total)
        {
            Console.WriteLine("\n--- SolutionProcessor Tests ---");
            int localPassed = 0;

            // Test 1: Constructor validation
            total++;
            if (TestSolutionProcessorConstructor())
            {
                localPassed++;
                Console.WriteLine("✓ SolutionProcessor constructor validation test PASSED");
            }
            else
            {
                Console.WriteLine("✗ SolutionProcessor constructor validation test FAILED");
            }

            // Test 2: GetSolutions parameter validation
            total++;
            if (TestGetSolutionsParameterValidation())
            {
                localPassed++;
                Console.WriteLine("✓ GetSolutions parameter validation test PASSED");
            }
            else
            {
                Console.WriteLine("✗ GetSolutions parameter validation test FAILED");
            }

            return localPassed;
        }

        private static bool TestSolutionProcessorConstructor()
        {
            try
            {
                // Test null parameter
                try
                {
                    var processor = new SolutionProcessor(null);
                    return false; // Should have thrown
                }
                catch (ArgumentNullException)
                {
                    return true; // Expected behavior
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestSolutionProcessorConstructor error: {ex.Message}");
                return false;
            }
        }

        private static bool TestGetSolutionsParameterValidation()
        {
            try
            {
                var mockService = new MockDataverseService();
                var processor = new SolutionProcessor(mockService);

                // Test null parameter
                try
                {
                    processor.GetSolutions(null);
                    return false; // Should have thrown
                }
                catch (ArgumentException)
                {
                    // Expected behavior
                }

                // Test empty parameter
                try
                {
                    processor.GetSolutions(new string[0]);
                    return false; // Should have thrown
                }
                catch (ArgumentException)
                {
                    return true; // Expected behavior
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestGetSolutionsParameterValidation error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region EntityProcessor Tests
        private static int RunEntityProcessorTests(ref int total)
        {
            Console.WriteLine("\n--- EntityProcessor Tests ---");
            int localPassed = 0;

            // Test 1: Constructor validation
            total++;
            if (TestEntityProcessorConstructor())
            {
                localPassed++;
                Console.WriteLine("✓ EntityProcessor constructor validation test PASSED");
            }
            else
            {
                Console.WriteLine("✗ EntityProcessor constructor validation test FAILED");
            }

            return localPassed;
        }

        private static bool TestEntityProcessorConstructor()
        {
            try
            {
                // Test null parameter
                try
                {
                    var processor = new EntityProcessor(null);
                    return false; // Should have thrown
                }
                catch (ArgumentNullException)
                {
                    return true; // Expected behavior
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestEntityProcessorConstructor error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region WebResourceProcessor Tests
        private static int RunWebResourceProcessorTests(ref int total)
        {
            Console.WriteLine("\n--- WebResourceProcessor Tests ---");
            int localPassed = 0;

            // Test 1: Constructor validation
            total++;
            if (TestWebResourceProcessorConstructor())
            {
                localPassed++;
                Console.WriteLine("✓ WebResourceProcessor constructor validation test PASSED");
            }
            else
            {
                Console.WriteLine("✗ WebResourceProcessor constructor validation test FAILED");
            }

            return localPassed;
        }

        private static bool TestWebResourceProcessorConstructor()
        {
            try
            {
                var mockService = new MockDataverseService();
                var parser = new JavaScriptParser();

                // Test null dataverse service
                try
                {
                    var processor = new WebResourceProcessor(null, parser);
                    return false; // Should have thrown
                }
                catch (ArgumentNullException)
                {
                    // Expected behavior
                }

                // Test null parser
                try
                {
                    var processor = new WebResourceProcessor(mockService, null);
                    return false; // Should have thrown
                }
                catch (ArgumentNullException)
                {
                    return true; // Expected behavior
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestWebResourceProcessorConstructor error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Integration Tests
        private static int RunIntegrationTests(ref int total)
        {
            Console.WriteLine("\n--- Integration Tests ---");
            int localPassed = 0;

            // Test 1: Full workflow simulation
            total++;
            if (TestFullWorkflowSimulation())
            {
                localPassed++;
                Console.WriteLine("✓ Full workflow simulation test PASSED");
            }
            else
            {
                Console.WriteLine("✗ Full workflow simulation test FAILED");
            }

            return localPassed;
        }

        private static bool TestFullWorkflowSimulation()
        {
            try
            {
                // Create mock components
                var mockService = new MockDataverseService();
                var solutionProcessor = new SolutionProcessor(mockService);
                var entityProcessor = new EntityProcessor(mockService);
                var javaScriptParser = new JavaScriptParser();
                var webResourceProcessor = new WebResourceProcessor(mockService, javaScriptParser);

                // Test that components can be created together
                var injector = new InjestorV2(mockService, solutionProcessor, entityProcessor, javaScriptParser, webResourceProcessor);

                // Verify statistics method works
                var stats = injector.GetProcessingStatistics();

                return stats != null &&
                       stats.ContainsKey("TotalSolutions") &&
                       stats.ContainsKey("TotalEntities") &&
                       stats.ContainsKey("TotalComponents");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestFullWorkflowSimulation error: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}