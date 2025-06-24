using DataDictionary.Models;
using DataIngestor.Processors;
using DataIngestor.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DataIngestor
{
    /// <summary>
    /// Refactored and streamlined Data Dictionary Ingestor
    /// Responsible for orchestrating the data dictionary building process
    /// </summary>
    public class InjestorV2
    {
        #region Private Fields
        private readonly IDataverseService _dataverseService;
        private readonly SolutionProcessor _solutionProcessor;
        private readonly EntityProcessor _entityProcessor;
        private readonly JavaScriptParser _javaScriptParser;
        private readonly WebResourceProcessor _webResourceProcessor;

        private Dictionary<string, DataDictionarySolution> _solutions;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the InjestorV2 class with dependency injection
        /// </summary>
        /// <param name="dataverseService">The Dataverse service for data operations</param>
        /// <param name="solutionProcessor">The solution processor for solution operations</param>
        /// <param name="entityProcessor">The entity processor for entity operations</param>
        /// <param name="javaScriptParser">The JavaScript parser for parsing web resources</param>
        /// <param name="webResourceProcessor">The web resource processor for web resource operations</param>
        public InjestorV2(
            IDataverseService dataverseService,
            SolutionProcessor solutionProcessor,
            EntityProcessor entityProcessor,
            JavaScriptParser javaScriptParser,
            WebResourceProcessor webResourceProcessor)
        {
            _dataverseService = dataverseService ?? throw new ArgumentNullException(nameof(dataverseService));
            _solutionProcessor = solutionProcessor ?? throw new ArgumentNullException(nameof(solutionProcessor));
            _entityProcessor = entityProcessor ?? throw new ArgumentNullException(nameof(entityProcessor));
            _javaScriptParser = javaScriptParser ?? throw new ArgumentNullException(nameof(javaScriptParser));
            _webResourceProcessor = webResourceProcessor ?? throw new ArgumentNullException(nameof(webResourceProcessor));

            _solutions = new Dictionary<string, DataDictionarySolution>();
        }

        /// <summary>
        /// Initializes a new instance of the InjestorV2 class with IOrganizationService
        /// This constructor creates the necessary processors internally
        /// </summary>
        /// <param name="organizationService">The Dataverse organization service</param>
        public InjestorV2(IOrganizationService organizationService)
        {
            if (organizationService == null)
                throw new ArgumentNullException(nameof(organizationService));

            _dataverseService = new DataverseService(organizationService);
            _solutionProcessor = new SolutionProcessor(_dataverseService);
            _entityProcessor = new EntityProcessor(_dataverseService);
            _javaScriptParser = new JavaScriptParser();
            _webResourceProcessor = new WebResourceProcessor(_dataverseService, _javaScriptParser);

            _solutions = new Dictionary<string, DataDictionarySolution>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Processes the specified solutions by their unique names to build a data dictionary
        /// </summary>
        /// <param name="solutionUniqueNames">Array of unique names representing the solutions to be processed</param>
        public void ProcessSolutions(string[] solutionUniqueNames)
        {
            if (solutionUniqueNames == null || solutionUniqueNames.Length == 0)
                throw new ArgumentException("Solution names cannot be null or empty", nameof(solutionUniqueNames));

            var timer = Stopwatch.StartNew();

            try
            {
                Console.WriteLine("=== Starting Data Dictionary Processing ===");
                Console.WriteLine($"Processing {solutionUniqueNames.Length} solutions: {string.Join(", ", solutionUniqueNames)}");

                // Step 1: Retrieve Solutions
                _solutions = _solutionProcessor.GetSolutions(solutionUniqueNames);

                // Step 2: Get Components for each Solution
                foreach (var solution in _solutions.Values)
                {
                    Console.WriteLine($"Processing Solution: {solution.UniqueName}");
                    _solutionProcessor.PopulateComponentsInSolution(solution);
                }

                // Step 3: Process Entities
                _entityProcessor.ProcessEntities(_solutions);

                // Step 4: Process Attributes
                _entityProcessor.ProcessAttributes(_solutions);

                // Step 5: Process Web Resources
                _webResourceProcessor.ProcessWebResources(_solutions);

                // Step 6: Log Schema Information
                LogSchemaInformation();

                // Step 7: Save to Dataverse
                SaveToDataverse();

                timer.Stop();
                Console.WriteLine($"Processing Complete. Time elapsed: {timer.Elapsed}");
                Console.WriteLine($"Processed {_solutions.Count} solutions with components and entities.");
            }
            catch (Exception ex)
            {
                timer.Stop();
                Console.WriteLine($"Error during processing: {ex.Message}");
                Console.WriteLine($"Processing failed after: {timer.Elapsed}");
                throw;
            }
        }

        /// <summary>
        /// Processes solutions without web resources (lighter operation)
        /// </summary>
        /// <param name="solutionUniqueNames">Array of solution unique names</param>
        public void ProcessSolutionsMetadataOnly(string[] solutionUniqueNames)
        {
            if (solutionUniqueNames == null || solutionUniqueNames.Length == 0)
                throw new ArgumentException("Solution names cannot be null or empty", nameof(solutionUniqueNames));

            var timer = Stopwatch.StartNew();

            try
            {
                Console.WriteLine("=== Starting Metadata-Only Processing ===");

                // Step 1: Retrieve Solutions
                _solutions = _solutionProcessor.GetSolutions(solutionUniqueNames);

                // Step 2: Get Components for each Solution
                foreach (var solution in _solutions.Values)
                {
                    _solutionProcessor.PopulateComponentsInSolution(solution);
                }

                // Step 3: Process Entities
                _entityProcessor.ProcessEntities(_solutions);

                // Step 4: Process Attributes
                _entityProcessor.ProcessAttributes(_solutions);

                // Step 5: Log Schema Information
                LogSchemaInformation();

                timer.Stop();
                Console.WriteLine($"Metadata-only processing complete. Time elapsed: {timer.Elapsed}");
            }
            catch (Exception ex)
            {
                timer.Stop();
                Console.WriteLine($"Error during metadata processing: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the processed solutions
        /// </summary>
        /// <returns>Dictionary of processed solutions</returns>
        public Dictionary<string, DataDictionarySolution> GetProcessedSolutions()
        {
            return new Dictionary<string, DataDictionarySolution>(_solutions);
        }

        /// <summary>
        /// Gets solution statistics
        /// </summary>
        /// <returns>Dictionary containing solution statistics</returns>
        public Dictionary<string, object> GetProcessingStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalSolutions"] = _solutions.Count,
                ["TotalEntities"] = _solutions.Values.Sum(s => s.Entities.Count),
                ["TotalComponents"] = _solutions.Values.Sum(s => s.Components.Count),
                ["TotalWebResources"] = _solutions.Values.Sum(s => s.WebResources?.Count ?? 0),
                ["TotalFieldModifications"] = _solutions.Values
                    .SelectMany(s => s.WebResources ?? new List<DataDictionaryWebResource>())
                    .Sum(wr => wr.FieldModifications?.Count ?? 0)
            };

            return stats;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Logs schema information for processed solutions
        /// </summary>
        private void LogSchemaInformation()
        {
            try
            {
                Console.WriteLine("=== Schema Information ===");
                
                foreach (var solutionPair in _solutions)
                {
                    var solution = solutionPair.Value;
                    Console.WriteLine($"Solution: {solution.FriendlyName} ({solution.UniqueName})");
                    Console.WriteLine($"  Entities: {solution.Entities.Count}");
                    Console.WriteLine($"  Components: {solution.Components.Count}");
                    Console.WriteLine($"  Web Resources: {solution.WebResources?.Count ?? 0}");

                    foreach (var entity in solution.Entities.Take(5)) // Show first 5 entities
                    {
                        Console.WriteLine($"    Entity: {entity.Name} ({entity.LogicalName})");
                    }

                    if (solution.Entities.Count > 5)
                    {
                        Console.WriteLine($"    ... and {solution.Entities.Count - 5} more entities");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging schema information: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves processed data to Dataverse
        /// </summary>
        private void SaveToDataverse()
        {
            try
            {
                Console.WriteLine("=== Saving to Dataverse ===");
                
                var totalRecords = 0;
                
                foreach (var solution in _solutions.Values)
                {
                    totalRecords += SaveSolutionToDataverse(solution);
                }

                Console.WriteLine($"Successfully saved {totalRecords} records to Dataverse");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to Dataverse: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves a single solution's data to Dataverse
        /// </summary>
        /// <param name="solution">The solution to save</param>
        /// <returns>Number of records saved</returns>
        private int SaveSolutionToDataverse(DataDictionarySolution solution)
        {
            var requests = new List<OrganizationRequest>();
            var batchSize = 100;
            var recordCount = 0;

            try
            {
                // Save entities
                foreach (var entity in solution.Entities)
                {
                    var entityRecord = CreateEntityRecord(entity, solution);
                    requests.Add(new Microsoft.Xrm.Sdk.Messages.CreateRequest { Target = entityRecord });
                    recordCount++;

                    if (requests.Count >= batchSize)
                    {
                        _dataverseService.ExecuteBatch(requests);
                        requests.Clear();
                    }
                }

                // Save web resources and their modifications
                if (solution.WebResources != null)
                {
                    foreach (var webResource in solution.WebResources)
                    {
                        var webResourceRecord = CreateWebResourceRecord(webResource, solution);
                        requests.Add(new Microsoft.Xrm.Sdk.Messages.CreateRequest { Target = webResourceRecord });
                        recordCount++;

                        if (requests.Count >= batchSize)
                        {
                            _dataverseService.ExecuteBatch(requests);
                            requests.Clear();
                        }
                    }
                }

                // Execute remaining requests
                if (requests.Count > 0)
                {
                    _dataverseService.ExecuteBatch(requests);
                }

                Console.WriteLine($"Saved {recordCount} records for solution: {solution.UniqueName}");
                return recordCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving solution {solution.UniqueName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates an entity record for Dataverse storage
        /// </summary>
        /// <param name="entity">The entity to create a record for</param>
        /// <param name="solution">The parent solution</param>
        /// <returns>Entity record ready for Dataverse</returns>
        private Entity CreateEntityRecord(DataDictionaryEntity entity, DataDictionarySolution solution)
        {
            var record = new Entity("ljr_datadictionaryentity");
            
            record["ljr_name"] = entity.Name;
            record["ljr_logicalname"] = entity.LogicalName;
            record["ljr_objecttypecode"] = entity.ObjectTypeCode;
            record["ljr_entitysetname"] = entity.EntitySetName;
            record["ljr_basetablename"] = entity.BaseTableName;
            record["ljr_collectionname"] = entity.CollectionName;
            record["ljr_isactivity"] = entity.IsActivity;
            record["ljr_solutionuniquename"] = solution.UniqueName;

            return record;
        }

        /// <summary>
        /// Creates a web resource record for Dataverse storage
        /// </summary>
        /// <param name="webResource">The web resource to create a record for</param>
        /// <param name="solution">The parent solution</param>
        /// <returns>Entity record ready for Dataverse</returns>
        private Entity CreateWebResourceRecord(DataDictionaryWebResource webResource, DataDictionarySolution solution)
        {
            var record = new Entity("ljr_datadictionarywebresource");
            
            record["ljr_displayname"] = webResource.DisplayName;
            record["ljr_webresourceid"] = webResource.WebResourceId.ToString();
            record["ljr_dependencyxml"] = webResource.DependencyXml;
            record["ljr_parseddependenciesjson"] = webResource.ParsedDependenciesJson;
            record["ljr_solutionuniquename"] = solution.UniqueName;

            if (webResource.ApiPatterns?.Any() == true)
            {
                record["ljr_apipatterns"] = string.Join(";", webResource.ApiPatterns);
            }

            return record;
        }
        #endregion
    }
}