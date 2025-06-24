using DataDictionary.Models;
using DataIngestor.Services;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataIngestor.Processors
{
    /// <summary>
    /// Handles all solution-related operations including retrieving solutions and their components
    /// </summary>
    public class SolutionProcessor
    {
        private readonly IDataverseService _dataverseService;

        public SolutionProcessor(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService ?? throw new ArgumentNullException(nameof(dataverseService));
        }

        /// <summary>
        /// Retrieves solutions by their unique names
        /// </summary>
        /// <param name="solutionNames">Array of solution unique names to retrieve</param>
        /// <returns>Dictionary of solution unique names to DataDictionarySolution objects</returns>
        public Dictionary<string, DataDictionarySolution> GetSolutions(string[] solutionNames)
        {
            if (solutionNames == null || solutionNames.Length == 0)
                throw new ArgumentException("Solution names cannot be null or empty", nameof(solutionNames));

            var solutions = new Dictionary<string, DataDictionarySolution>();

            try
            {
                var solutionQuery = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("uniquename", "friendlyname", "solutionid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("uniquename", ConditionOperator.In, solutionNames)
                        }
                    }
                };

                var solutionResults = _dataverseService.RetrieveMultiple(solutionQuery);

                foreach (var solution in solutionResults.Entities)
                {
                    var ddSolution = new DataDictionarySolution
                    {
                        UniqueName = solution.GetAttributeValue<string>("uniquename"),
                        FriendlyName = solution.GetAttributeValue<string>("friendlyname"),
                        SolutionId = solution.GetAttributeValue<Guid>("solutionid").ToString()
                    };

                    solutions[ddSolution.UniqueName] = ddSolution;
                    Console.WriteLine($"Retrieved solution: {ddSolution.FriendlyName} ({ddSolution.UniqueName})");
                }

                Console.WriteLine($"Successfully retrieved {solutions.Count} solutions out of {solutionNames.Length} requested");
                return solutions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving solutions: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all components within a specific solution
        /// </summary>
        /// <param name="solution">The solution to get components for</param>
        public void PopulateComponentsInSolution(DataDictionarySolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            try
            {
                var componentQuery = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("componenttype", "objectid", "ismetadata", "rootsolutioncomponentid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("solutionid", ConditionOperator.Equal, Guid.Parse(solution.SolutionId))
                        }
                    }
                };

                var componentResults = _dataverseService.RetrieveMultiple(componentQuery);

                foreach (var component in componentResults.Entities)
                {
                    var ddComponent = new DataDictionarySolutionComponent
                    {
                        ComponentType = component.GetAttributeValue<int>("componenttype"),
                        ObjectId = component.GetAttributeValue<Guid>("objectid"),
                        IsMetadata = component.GetAttributeValue<bool>("ismetadata"),
                        RootSolutionComponentId = component.GetAttributeValue<Guid>("rootsolutioncomponentid")
                    };

                    solution.AddComponent(ddComponent);
                    Console.WriteLine($"Component Type: {ddComponent.ComponentType}, Is Metadata: {ddComponent.IsMetadata}, Object Id: {ddComponent.ObjectId}");
                }

                Console.WriteLine($"Retrieved {solution.Components.Count} components for solution: {solution.UniqueName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving components for solution {solution.UniqueName}: {ex.Message}");
                throw;
            }
        }
    }
}