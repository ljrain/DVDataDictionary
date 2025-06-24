using DataDictionary.Models;
using DataIngestor.Services;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataIngestor.Processors
{
    /// <summary>
    /// Handles all web resource related operations including retrieval and JavaScript parsing
    /// </summary>
    public class WebResourceProcessor
    {
        private readonly IDataverseService _dataverseService;
        private readonly JavaScriptParser _javaScriptParser;

        public WebResourceProcessor(IDataverseService dataverseService, JavaScriptParser javaScriptParser)
        {
            _dataverseService = dataverseService ?? throw new ArgumentNullException(nameof(dataverseService));
            _javaScriptParser = javaScriptParser ?? throw new ArgumentNullException(nameof(javaScriptParser));
        }

        /// <summary>
        /// Processes web resources for all provided solutions
        /// </summary>
        /// <param name="solutions">Dictionary of solutions to process web resources for</param>
        public void ProcessWebResources(Dictionary<string, DataDictionarySolution> solutions)
        {
            if (solutions == null || !solutions.Any())
                throw new ArgumentException("Solutions cannot be null or empty", nameof(solutions));

            try
            {
                Console.WriteLine("=== Processing Web Resources ===");

                foreach (var solutionPair in solutions)
                {
                    var solution = solutionPair.Value;
                    ProcessWebResourcesForSolution(solution);
                }

                Console.WriteLine($"Completed processing web resources for {solutions.Count} solutions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing web resources: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Processes web resources for a specific solution
        /// </summary>
        /// <param name="solution">The solution to process web resources for</param>
        private void ProcessWebResourcesForSolution(DataDictionarySolution solution)
        {
            if (solution?.Components == null)
                return;

            try
            {
                var webResourceObjectIds = GetWebResourceObjectIds(solution);
                
                if (webResourceObjectIds.Length == 0)
                {
                    Console.WriteLine($"No web resources found in solution: {solution.UniqueName}");
                    return;
                }

                Console.WriteLine($"Found {webResourceObjectIds.Length} web resource object IDs to process for solution: {solution.UniqueName}");

                var webResources = GetWebResourcesByObjectIds(webResourceObjectIds);
                var processedWebResources = new List<DataDictionaryWebResource>();

                foreach (var webResource in webResources)
                {
                    try
                    {
                        var processedWebResource = ProcessWebResource(webResource);
                        if (processedWebResource != null)
                        {
                            processedWebResources.Add(processedWebResource);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing web resource {webResource.Id}: {ex.Message}");
                        // Continue processing other web resources
                    }
                }

                // Initialize WebResources collection if null
                if (solution.WebResources == null)
                {
                    solution.WebResources = new List<DataDictionaryWebResource>();
                }

                solution.WebResources.AddRange(processedWebResources);
                
                Console.WriteLine($"Successfully processed {processedWebResources.Count} web resources for solution: {solution.UniqueName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing web resources for solution {solution.UniqueName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets web resource object IDs from solution components
        /// </summary>
        /// <param name="solution">The solution to get web resource IDs from</param>
        /// <returns>Array of web resource object IDs</returns>
        private string[] GetWebResourceObjectIds(DataDictionarySolution solution)
        {
            try
            {
                // Component type 61 represents Web Resources
                var webResourceComponents = solution.Components
                    .Where(c => c.ComponentType == 61)
                    .Select(c => c.ObjectId.ToString())
                    .ToArray();

                Console.WriteLine($"Found {webResourceComponents.Length} web resource components in solution: {solution.UniqueName}");
                return webResourceComponents;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting web resource object IDs: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Retrieves web resources by their object IDs
        /// </summary>
        /// <param name="objectIds">Array of web resource object IDs</param>
        /// <returns>Collection of web resource entities</returns>
        private List<Microsoft.Xrm.Sdk.Entity> GetWebResourcesByObjectIds(string[] objectIds)
        {
            if (objectIds == null || objectIds.Length == 0)
                return new List<Microsoft.Xrm.Sdk.Entity>();

            try
            {
                var webResourceQuery = new QueryExpression("webresource")
                {
                    ColumnSet = new ColumnSet("webresourceid", "name", "displayname", "content", 
                                            "dependencyxml", "webresourcetype"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("webresourceid", ConditionOperator.In, objectIds),
                            new ConditionExpression("webresourcetype", ConditionOperator.Equal, 3) // JavaScript files only
                        }
                    }
                };

                var results = _dataverseService.RetrieveMultiple(webResourceQuery);
                
                Console.WriteLine($"Retrieved {results.Entities.Count} JavaScript web resources");
                return results.Entities.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving web resources: {ex.Message}");
                return new List<Microsoft.Xrm.Sdk.Entity>();
            }
        }

        /// <summary>
        /// Processes a single web resource entity
        /// </summary>
        /// <param name="webResourceEntity">The web resource entity to process</param>
        /// <returns>Processed DataDictionaryWebResource object</returns>
        private DataDictionaryWebResource ProcessWebResource(Microsoft.Xrm.Sdk.Entity webResourceEntity)
        {
            try
            {
                Console.WriteLine($"Processing Web Resource: {webResourceEntity.GetAttributeValue<string>("displayname")} ({webResourceEntity.GetAttributeValue<Guid>("webresourceid")})");

                string base64Content = webResourceEntity.GetAttributeValue<string>("content");
                if (string.IsNullOrWhiteSpace(base64Content))
                {
                    Console.WriteLine("Web resource has no content, skipping JavaScript parsing");
                    return null;
                }

                string javascript = DecodeBase64Content(base64Content);
                if (string.IsNullOrWhiteSpace(javascript))
                {
                    Console.WriteLine("Could not decode web resource content, skipping");
                    return null;
                }

                var webResource = new DataDictionaryWebResource
                {
                    WebResourceId = webResourceEntity.Id,
                    DisplayName = webResourceEntity.GetAttributeValue<string>("name"),
                    Content = javascript,
                    DependencyXml = webResourceEntity.GetAttributeValue<string>("dependencyxml") ?? string.Empty
                };

                // Parse dependencies
                webResource.ParseDependencies();

                // Parse JavaScript for field modifications and API patterns
                var apiPatterns = _javaScriptParser.ParseJavaScript(javascript, webResource);

                Console.WriteLine($"Found {webResource.FieldModifications?.Count ?? 0} field modifications and {apiPatterns.Count} API patterns");

                return webResource;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing web resource: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decodes base64 content to JavaScript string
        /// </summary>
        /// <param name="base64Content">Base64 encoded content</param>
        /// <returns>Decoded JavaScript content</returns>
        private string DecodeBase64Content(string base64Content)
        {
            try
            {
                byte[] contentBytes = Convert.FromBase64String(base64Content);
                return Encoding.UTF8.GetString(contentBytes);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Invalid base64 format: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding content: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Correlates JavaScript modifications with attribute metadata
        /// </summary>
        /// <param name="solutions">Dictionary of solutions containing web resources and entities</param>
        public void CorrelateJavaScriptModificationsWithAttributes(Dictionary<string, DataDictionarySolution> solutions)
        {
            if (solutions == null || !solutions.Any())
                return;

            try
            {
                Console.WriteLine("=== Correlating JavaScript modifications with attributes ===");

                int correlationCount = 0;

                foreach (var solution in solutions.Values)
                {
                    if (solution.WebResources == null)
                        continue;

                    foreach (var webResource in solution.WebResources)
                    {
                        if (webResource.FieldModifications == null)
                            continue;

                        foreach (var modification in webResource.FieldModifications)
                        {
                            // Try to find matching entity and attribute
                            var matchingEntity = solution.Entities
                                .FirstOrDefault(e => modification.FieldName.StartsWith(e.LogicalName + ".") ||
                                                   webResource.ModifiedTables.Contains(e.LogicalName));

                            if (matchingEntity != null)
                            {
                                modification.EntityLogicalName = matchingEntity.LogicalName;
                                modification.EntityName = matchingEntity.Name;
                                correlationCount++;

                                Console.WriteLine($"Correlated modification '{modification.FieldName}' with entity '{matchingEntity.LogicalName}'");
                            }
                        }
                    }
                }

                Console.WriteLine($"Successfully correlated {correlationCount} JavaScript modifications with entity attributes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error correlating JavaScript modifications: {ex.Message}");
            }
        }
    }
}