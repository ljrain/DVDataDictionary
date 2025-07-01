using DataDictionary.Models;
//using DataIngestor.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DataIngestor
{
    public class InjestorV2
    {
        public InjestorV2(IOrganizationService servClient)
        {
            _service = servClient ?? throw new ArgumentNullException(nameof(servClient));
        }

        #region Private Fields

        private IOrganizationService _service;
        private Dictionary<string, DataDictionarySolution> _ddSolutions = new Dictionary<string, DataDictionarySolution>();
        private List<string> _allowedLogicalNames = new List<string>();

        #endregion


        /// <summary>
        /// Processes the specified solutions by their unique names.
        /// </summary>
        /// <remarks>This method will process a string array of solution unique names to build a data dictionary.</remarks>
        /// <param name="solutionUniqueNames">An array of unique names representing the solutions to be processed.  Each name must be non-null and
        public void ProcessSolutions(string[] solutionUniqueNames)
        {
            Stopwatch timerGlobal = new Stopwatch(); // Replace Timer with Stopwatch
            timerGlobal.Start();

            GetSolutions(solutionUniqueNames);
            foreach (var ddSolution in _ddSolutions.Values)
            {
                Console.WriteLine($"Processing Solution: {ddSolution.UniqueName}");
                GetComponentsInSolution(ddSolution);


            }
            ProcessEntities();

            Console.WriteLine(timerGlobal.Elapsed.ToString());

            _ddSolutions["SampleSolution"].GetLogicalEntitiesFromSolutions();

            #region Get all web resources and javascript decoded
            string[] webResourceObjectIds = GetWebResourceObjectIds();
            if (webResourceObjectIds.Length > 0)
            {
                Console.WriteLine($"Found {webResourceObjectIds.Length} Web Resource Object IDs to process.");
                var webResources = GetWebResourcesByObjectIds(webResourceObjectIds);
                foreach (var webResource in webResources) // Rename the outer loop variable to avoid conflict
                {
                    foreach (var ddSolution in _ddSolutions.Values) // Correctly define 'ddSolution' in the outer loop
                    {
                        foreach (var resource in ddSolution.WebResources) // Use 'ddSolution' from the outer loop
                        {
                            if (resource.FieldModifications == null)
                                continue;

                            foreach (var modification in resource.FieldModifications)
                            {
                                // Process modifications here
                            }
                        }
                    }

                    Console.WriteLine($"Found Web Resource: {webResource.GetAttributeValue<string>("displayname")} ({webResource.GetAttributeValue<Guid>("webresourceidunique")})");

                    string base64Content = webResource.GetAttributeValue<string>("content");
                    string javascript = Encoding.UTF8.GetString(Convert.FromBase64String(base64Content));
                    //Console.WriteLine(javascript);
                    DataDictionaryWebResource webRes = new DataDictionaryWebResource();
                    webRes.WebResourceId = webResource.Id;
                    webRes.DisplayName = webResource.Attributes["name"].ToString();
                    webRes.Content = javascript;
                    webRes.DependencyXml = webResource.GetAttributeValue<string>("dependencyxml") ?? string.Empty;

                    webRes.ParseDependencies(); // Parse the DependencyXml and populate ParsedDependencies

                    // --- FIX: Assign the result of ParseFieldModifications to FieldModifications property ---
                    webRes.FieldModifications = ParseFieldModifications(webRes.Content, webRes.WebResourceId, webRes.DisplayName);

                    // Optionally, add debug logging:
                    Console.WriteLine($"[DEBUG] FieldModifications count for {webRes.DisplayName}: {webRes.FieldModifications.Count}");

                    // If ParseJavaScript is still needed for API pattern detection, call it separately and assign its result to ApiPatterns:
                    webRes.ApiPatterns = ParseJavaScript(webRes.Content, webRes);

                    _ddSolutions["SampleSolution"].WebResources.Add(new DataDictionary.Models.DataDictionaryWebResource
                    {
                        WebResourceId = webRes.WebResourceId,
                        DisplayName = webRes.DisplayName,
                        Content = webRes.Content,
                        DependencyXml = webRes.DependencyXml,
                        FieldModifications = webRes.FieldModifications,
                        ApiPatterns = webRes.ApiPatterns
                    });

                    // Populate the ParsedDependencies list separately using the `AddRange` method.
                    _ddSolutions["SampleSolution"].WebResources.Last().ParsedDependencies.AddRange(webRes.ParsedDependencies.Select(dep => new DataDictionary.Models.WebResourceDependency
                    {
                        ComponentType = dep.AttributeType, // Map DependencyType to ComponentType
                        AttributeName = dep.AttributeName  // Map DependencyName to AttributeName

                    }));
                    _ddSolutions["SampleSolution"].WebResources.Last().ParsedDependencies.AddRange(webRes.ParsedDependencies.Select(dep => new DataDictionary.Models.WebResourceDependency
                    {
                        ComponentType = dep.AttributeType, // Map DependencyType to ComponentType
                        AttributeName = dep.AttributeName,  // Map DependencyName to AttributeName
                        AttributeId = dep.AttributeId, // Map EntityId to AttributeId
                        AttributeLogicalName = dep.AttributeLogicalName, // Map EntityLogicalName to AttributeLogicalName
                        EntityName = dep.EntityName, // Map EntityName to EntityName
                        AttributeType = dep.AttributeType // Map AttributeType to AttributeType
                    }));
                    List<string> parsedJavaScript = ParseJavaScript(webRes.Content, webRes); // Parse the JavaScript content for Dataverse API events/actions and field modifications
                    Console.WriteLine($"Parsed {parsedJavaScript.Count} Dataverse API events/actions and {webRes.FieldModifications.Count} field modifications from Web Resource: {webRes.DisplayName}");
                }
                #endregion

                SaveJavascriptToDataverse(); // Save web resources to Dataverse
                CorrelateJavaScriptModificationsWithAttributes(); // Correlate JS modifications with attribute metadata
                LogSchema();
                Console.WriteLine($"Processed {_ddSolutions.Count} solutions with components and entities.");
                SaveToDataverse();

                timerGlobal.Stop(); // Stop the timer
                Console.WriteLine($"Processing Complete. Time elapsed: {timerGlobal.Elapsed}"); // Use timerGlobal
            }

            #region Display Web Resource Modifications
            foreach (var ddSolution in _ddSolutions.Values)
            {
                foreach (var webResource in ddSolution.WebResources)
                {
                    Console.WriteLine($"Web Resource: {webResource.DisplayName} ({webResource.WebResourceId})");

                    //Display modified tables
                    //if (webResource.ModifiedTables.Count > 0)
                    //{
                    //    Console.WriteLine("  Modified Tables:");
                    //    foreach (var table in webResource.ModifiedTables)
                    //    {
                    //        Console.WriteLine($"    - {table}");
                    //    }
                    //}
                    //else
                    //{
                    //    Console.WriteLine("  Modified Tables: None");
                    //}

                    //// Display modified attributes
                    //if (webResource.ModifiedAttributes.Count > 0)
                    //{
                    //    Console.WriteLine("  Modified Attributes:");
                    //    foreach (var attr in webResource.ModifiedAttributes)
                    //    {
                    //        Console.WriteLine($"    - {attr}");
                    //    }
                    //}
                    //else
                    //{
                    //    Console.WriteLine("  Modified Attributes: None");
                    //}

                    Console.WriteLine();
                }
            }
            #endregion



            #region Group models together based on Entity and Attribute metadata

            foreach (var ddSolution in _ddSolutions.Values)
            {
                Console.WriteLine($"Solution: {ddSolution.UniqueName}");
                foreach (var entity in ddSolution.Entities)
                {
                    Console.WriteLine($"  Entity: {entity.LogicalName} ({entity.EntityId})");
                    foreach (var attribute in entity.Attributes)
                    {
                        Console.WriteLine($"    Attribute: {attribute.LogicalName} ({attribute.AttributeId})");
                    }
                }
            }

            #endregion
        }


        #region Private Methods

        private string[] GetWebResourceObjectIds()
        {
            List<string> objectIds = new List<string>();
            foreach (var ddSolution in _ddSolutions.Values)
            {
                foreach (var ddComponent in ddSolution.Components)
                {
                    if (ddComponent.ComponentType == 61) // Assuming 61 is the type for Web Resource
                    {
                        objectIds.Add(ddComponent.ObjectId.ToString());
                    }
                }
            }
            return objectIds.ToArray();
        }


        /// <summary>
        /// Takes in a string array of solution unique names and retrieves the solutions from CRM.
        /// A dictionary of DataDictionarySolution objects is built, keyed by the unique name of the solution.
        /// </summary>
        /// <param name="solutionNames">String array of unique names of solutions to process.</param>
        private void GetSolutions(string[] solutionNames)
        {
            foreach (var solutionName in solutionNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename"),
                    Criteria = new FilterExpression
                    {
                        FilterOperator = LogicalOperator.And
                    }
                };

                query.Criteria.AddCondition(new ConditionExpression
                {
                    AttributeName = "uniquename",
                    Operator = ConditionOperator.Equal,
                    Values = { solutionName }
                });

                var solutions = _service.RetrieveMultiple(query);
                foreach (var solution in solutions.Entities)
                {
                    DataDictionarySolution ddSolution = new DataDictionarySolution
                    {
                        FriendlyName = solution.GetAttributeValue<string>("friendlyname"),
                        SolutionId = solution.GetAttributeValue<Guid>("solutionid").ToString(),
                        UniqueName = solution.GetAttributeValue<string>("uniquename")
                    };
                    _ddSolutions.Add(ddSolution.UniqueName, ddSolution);
                }
            }
        }


        private void GetComponentsInSolution(DataDictionarySolution ddSolution)
        {
            // Query solutioncomponent for forms in the solution
            var componentQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype", "ismetadata", "rootcomponentbehavior", "rootsolutioncomponentid", "solutionid"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("solutionid", ConditionOperator.Equal, new Guid(ddSolution.SolutionId)),
            }
                }
            };

            var components = _service.RetrieveMultiple(componentQuery);
            foreach (var component in components.Entities)
            {
                DataDictionarySolutionComponent ddComponent = new DataDictionarySolutionComponent
                {
                    ObjectId = component.GetAttributeValue<Guid>("objectid"),
                    ComponentType = component.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0,
                    IsMetadata = component.GetAttributeValue<bool>("ismetadata"),
                    //RootComponentBehavior = component.GetAttributeValue<OptionSetValue>("rootcomponentbehavior")?.Value ?? 0,
                    RootSolutionComponentId = component.GetAttributeValue<Guid>("rootsolutioncomponentid")
                };
                ddSolution.AddComponent(ddComponent);
                //                if (ddComponent.ComponentType == 1)


                Console.WriteLine($"Component Type: {ddComponent.ComponentType}, Is Metadata: {ddComponent.IsMetadata}, Object Id: {ddComponent.ObjectId}");
            }
        }


        private void ProcessEntities()
        {
            foreach (DataDictionarySolution ddSolution in _ddSolutions.Values)
            {
                foreach (DataDictionarySolutionComponent ddComponent in ddSolution.Components)
                {
                    if (ddComponent.ComponentType == 1) // Assuming 1 is the type for Entity
                    {
                        // Retrieve entity details and attributes here
                        Console.WriteLine($"Processing Entity Component: {ddComponent.ObjectId} in Solution: {ddSolution.UniqueName}");

                        var entityQuery = new QueryExpression("entity")
                        {
                            ColumnSet = new ColumnSet("name", "objecttypecode", "basetablename", "collectionname", "componentstate", "entityid", "entitysetname", "isactivity", "logicalname"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("entityid", ConditionOperator.Equal, ddComponent.ObjectId)
                                }
                            }
                        };

                        EntityCollection results = _service.RetrieveMultiple(entityQuery);
                        foreach (var entity in results.Entities)
                        {
                            DataDictionaryEntity ddEntity = new DataDictionaryEntity
                            {
                                Name = entity.GetAttributeValue<string>("name"),
                                ObjectTypeCode = entity.GetAttributeValue<int>("objecttypecode"),
                                EntityId = entity.GetAttributeValue<Guid>("entityid"),
                                EntitySetName = entity.GetAttributeValue<string>("entitysetname"),
                                BaseTableName = entity.GetAttributeValue<string>("basetablename"),
                                CollectionName = entity.GetAttributeValue<string>("collectionname"),
                                ComponentState = entity.GetAttributeValue<OptionSetValue>("componentstate")?.Value ?? 0,
                                IsActivity = entity.GetAttributeValue<bool>("isactivity"),
                                LogicalName = entity.GetAttributeValue<string>("logicalname")
                            };
                            ddSolution.AddEntity(ddEntity);
                            _allowedLogicalNames.Add(ddEntity.LogicalName); // Add logical name to allowed list

                            Console.WriteLine($"Entity: {ddEntity.Name}, Object Type Code: {ddEntity.ObjectTypeCode}, Entity Set Name: {ddEntity.EntitySetName}");
                        }
                    }
                }
            }
        }

        private void ProcessAttributesAsync()
        {
            foreach (DataDictionarySolution ddSolution in _ddSolutions.Values)
            {
                foreach (DataDictionarySolutionComponent ddComponent in ddSolution.Components)
                {
                    if (ddComponent.ComponentType == 2) // 2 is the type for Attribute
                    {
                        // Retrieve attribute details here
                        Console.WriteLine($"Processing Attribute Component: {ddComponent.ObjectId} in Solution: {ddSolution.UniqueName}");
                        var attributeQuery = new QueryExpression("attribute")
                        {
                            ColumnSet = new ColumnSet("attributeid", "attributeof", "attributetypeid", "componentstate", "externalname", "logicalname"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("attributeid", ConditionOperator.Equal, ddComponent.ObjectId)
                                }
                            }
                        };

                        var results = _service.RetrieveMultiple(attributeQuery);
                        foreach (var attribute in results.Entities)
                        {
                            DataDictionaryAttribute ddAttribute = new DataDictionaryAttribute
                            {
                                AttributeId = attribute.GetAttributeValue<Guid>("attributeid"),
                                AttributeOf = attribute.GetAttributeValue<Guid>("attributeof"),
                                AttributeTypeId = attribute.GetAttributeValue<Guid>("attributetypeid"),
                                LogicalName = attribute.GetAttributeValue<string>("logicalname"),
                            };

                            ddSolution.AddAttribute(ddSolution, ddAttribute);
                            Console.WriteLine($"Attribute: {ddAttribute.AttributeName}, Logical Name: {ddAttribute.LogicalName}");
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Retrieves all web resources matching the given object IDs.
        /// </summary>
        /// <param name="objectIds">Array of webresourceid strings (GUIDs).</param>
        /// <returns>List of Entity objects representing web resources.</returns>
        public List<Entity> GetWebResourcesByObjectIds(string[] objectIds)
        {
            if (objectIds == null || objectIds.Length == 0)
                return new List<Entity>();

            //_tracingService?.Trace("Retrieving web resources for object IDs: {0}", string.Join(", ", objectIds));

            var query = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet(
                    "webresourceid",
                    "webresourceidunique",
                    "webresourcetype",
                    "dependencyxml",
                    "description",
                    "displayname",
                    "name",
                    "content",
                    "createdon",
                    "modifiedon"
                ),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And
                }
            };

            // Add the 'In' condition for the webresourceid
            var guidList = objectIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(id => new Guid(id))
                .ToList();

            if (guidList.Count == 0)
                return new List<Entity>();

            query.Criteria.AddCondition(new ConditionExpression("webresourceid", ConditionOperator.In, guidList.Cast<object>().ToArray()));

            var results = _service.RetrieveMultiple(query);

            //_tracingService?.Trace("Found {0} web resources.", results.Entities.Count);

            return results.Entities.ToList();
        }


        /// <summary>
        /// Parses a JavaScript string and extracts Dataverse API events/actions and field modifications.
        /// </summary>
        /// <param name="script">The JavaScript code as a string.</param>
        /// <param name="webResource">The web resource being parsed.</param>
        /// <returns>List of found Dataverse API events/actions and hidden fields (legacy format).</returns>
        private List<string> ParseJavaScript(string script, DataDictionaryWebResource webResource = null)
        {
            var found = new List<string>();
            if (string.IsNullOrWhiteSpace(script))
                return found;

            // List of common Dataverse JS API patterns to look for
            var patterns = new[]
            {
                @"Xrm\.Page",
                @"formContext",
                @"Xrm\.WebApi",
                @"Xrm\.Navigation",
                @"Xrm\.Utility",
                @"addOnLoad",
                @"addOnSave",
                @"addOnChange",
                @"removeOnLoad",
                @"removeOnSave",
                @"removeOnChange",
                @"getAttribute",
                @"getControl",
                @"Xrm\.App",
                @"Xrm\.Device",
                @"Xrm\.Encoding",
                @"Xrm\.Panel",
                @"Xrm\.Process",
                @"Xrm\.Utility\.openEntityForm",
                @"Xrm\.WebApi\.retrieveRecord",
                @"Xrm\.WebApi\.updateRecord",
                @"Xrm\.WebApi\.createRecord",
                @"Xrm\.WebApi\.deleteRecord",
                @"Xrm\.Api\.execute",
                @"Xrm\.WebApi\.retrieveMultipleRecords"
            };

            foreach (var pattern in patterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(script, pattern);
                if (matches.Count > 0)
                {
                    found.Add(pattern);
                }
            }

            // Parse field modifications if web resource is provided
            if (webResource != null)
            {
                webResource.FieldModifications = ParseFieldModifications(script, webResource.WebResourceId, webResource.DisplayName);
                webResource.ApiPatterns = found.ToList();

                // NEW: Track modified attributes from field modifications
                foreach (var mod in webResource.FieldModifications)
                {
                    if (!string.IsNullOrWhiteSpace(mod.AttributeLogicalName))
                        webResource.ModifiedAttributes.Add(mod.AttributeLogicalName);
                    if (!string.IsNullOrWhiteSpace(mod.AttributeTable))
                        webResource.ModifiedTables.Add(mod.AttributeTable);
                }
            }

            // Legacy hidden field parsing for backward compatibility
            var hiddenFields = webResource?.FieldModifications?
                .Where(fm => fm.ModificationType == JavaScriptModificationType.Visibility &&
                           fm.ModificationValue?.ToLower() == "false")
                .Select(fm => fm.FieldName)
                .ToList() ?? new List<string>();

            if (hiddenFields.Count > 0)
            {
                Console.WriteLine("Hidden fields found in script:");
                foreach (var field in hiddenFields)
                {
                    Console.WriteLine($" - {field}");
                    found.Add("HiddenField:" + field);
                }
            }

            // Optionally, log or output the found events/actions
            if (found.Count > 0)
            {
                Console.WriteLine("Dataverse API events/actions found in script:");
                foreach (var evt in found)
                {
                    if (!evt.StartsWith("HiddenField:"))
                        Console.WriteLine($" - {evt}");
                }
            }

            return found;
        }

        /// <summary>
        /// Parses JavaScript code to identify field modifications (visibility, required level, default values, etc.)
        /// </summary>
        /// <param name="script">The JavaScript code to parse.</param>
        /// <param name="webResourceId">The ID of the web resource containing this script.</param>
        /// <param name="webResourceName">The name of the web resource containing this script.</param>
        /// <returns>List of field modifications found in the script.</returns>
        private List<DataDictionaryJavaScriptFieldModification> ParseFieldModifications(string script, Guid webResourceId, string webResourceName)
        {
            var modifications = new List<DataDictionaryJavaScriptFieldModification>();
            if (string.IsNullOrWhiteSpace(script))
                return modifications;

            var scriptLines = script.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Pattern definitions for different types of field modifications
            var patterns = new[]
            {
                // Visibility modifications - both formContext and Xrm.Page patterns
                new {
                    Regex = new Regex(@"(?:formContext|Xrm\.Page)\.getControl\(\s*[""']([^""']+)[""']\s*\)\.setVisible\s*\(\s*(true|false)\s*\)", RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.Visibility,
                    ValueGroup = 2
                },
                // Required level modifications - supports "required", "recommended", "none"
                new {
                    Regex = new Regex(@"(?:formContext|Xrm\.Page)\.getAttribute\(\s*[""']([^""']+)[""']\s*\)\.setRequiredLevel\s*\(\s*[""']([^""']+)[""']\s*\)", RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.RequiredLevel,
                    ValueGroup = 2
                },
                // Default value assignments - handles various value types
                new {
                    Regex = new Regex(@"(?:formContext|Xrm\.Page)\.getAttribute\(\s*[""']([^""']+)[""']\s*\)\.setValue\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.DefaultValue,
                    ValueGroup = 2
                },
                // Disabled state modifications
                new {
                    Regex = new Regex(@"(?:formContext|Xrm\.Page)\.getControl\(\s*[""']([^""']+)[""']\s*\)\.setDisabled\s*\(\s*(true|false)\s*\)", RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.DisabledState,
                    ValueGroup = 2
                },
                // Display name/label modifications
                new {
                    Regex = new Regex(@"(?:formContext|Xrm\.Page)\.getControl\(\s*[""']([^""']+)[""']\s*\)\.setLabel\s*\(\s*[""']([^""']*)[""']\s*\)", RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.DisplayName,
                    ValueGroup = 2
                }
            };

            // Additional patterns for more advanced scenarios
            var advancedPatterns = new[]
            {
                // Conditional visibility - e.g., setVisible(someCondition)
                new {
                    Regex = new Regex(@"(?:formContext|Xrm\.Page)\.getControl\(\s*[""']([^""']+)[""']\s*\)\.setVisible\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.Visibility,
                    ValueGroup = 2
                },
                // Variable-based setValue
                new {
                    Regex = new Regex(@"(?:formContext|Xrm\.Page)\.getAttribute\(\s*[""']([^""']+)[""']\s*\)\.setValue\s*\(\s*(\w+)\s*\)", RegexOptions.IgnoreCase),
                    Type = JavaScriptModificationType.DefaultValue,
                    ValueGroup = 2
                }
            };

            for (int lineIndex = 0; lineIndex < scriptLines.Length; lineIndex++)
            {
                var line = scriptLines[lineIndex];

                // Process primary patterns first
                foreach (var pattern in patterns)
                {
                    var matches = pattern.Regex.Matches(line);
                    foreach (Match match in matches)
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

                            Console.WriteLine($"Found {pattern.Type} modification for field '{modification.FieldName}': {modification.ModificationValue}");
                        }
                    }
                }

                // Process advanced patterns only if no primary patterns matched
                bool foundInPrimary = patterns.Any(p => p.Regex.IsMatch(line));
                if (!foundInPrimary)
                {
                    foreach (var pattern in advancedPatterns)
                    {
                        var matches = pattern.Regex.Matches(line);
                        foreach (Match match in matches)
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
                                    Notes = "Advanced pattern detected - may need manual verification",
                                    ParsedOn = DateTime.UtcNow
                                };

                                modifications.Add(modification);

                                Console.WriteLine($"Found advanced {pattern.Type} modification for field '{modification.FieldName}': {modification.ModificationValue}");
                            }
                        }
                    }
                }
            }

            return modifications;
        }


        private void LogSchema()
        {
            RetrieveAllEntitiesRequest request = null;
            RetrieveAllEntitiesResponse response = null;
            PicklistAttributeMetadata PicklistAM = null;
            OptionMetadata om = null;
            string FormulaDefinition = null;

            // create Default solution to hold metadata
            if (!_ddSolutions.ContainsKey("Default"))
            {
                _ddSolutions.Add("Default", new DataDictionarySolution
                {
                    FriendlyName = "Default Solution",
                    UniqueName = "Default"
                });
            }

            try
            {
                Console.Write("Retrieving Metadata .");
                request = new RetrieveAllEntitiesRequest()
                {
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = false,
                };
                response = (RetrieveAllEntitiesResponse)_service.Execute(request);

                // Pseudocode plan:
                // 1. Accept a list of allowed logical names (e.g., List<string> allowedLogicalNames or string[] allowedLogicalNames).
                // 2. Filter the EntityMetadata results so that only those with LogicalName in the allowed list are included.
                // 3. Apply this filter in the LINQ query where results are built.

                // Example: Add a parameter to LogSchema or make allowedLogicalNames available in scope
                // For demonstration, assume a variable allowedLogicalNames is available (e.g., string[] allowedLogicalNames).

                IEnumerable<EntityMetadata> results = response.EntityMetadata
                    .Where(e => e.IsCustomizable != null
                        && _allowedLogicalNames.Contains(e.LogicalName))
                    .OrderBy(e => e.LogicalName)
                    .ToList();

                if (results != null)
                {
                    foreach (EntityMetadata entity in results)
                    {
                        foreach (AttributeMetadata attribute in entity.Attributes)
                        {
                            DataDictionaryAttributeMetadata ddMeta = new DataDictionaryAttributeMetadata();
                            ddMeta.Table = entity.LogicalName;
                            ddMeta.ColumnDisplay = (attribute.DisplayName.UserLocalizedLabel == null ? String.Empty : attribute.DisplayName.UserLocalizedLabel.Label);
                            ddMeta.ColumnLogical = attribute.LogicalName;
                            ddMeta.ColumnSchema = attribute.SchemaName;
                            ddMeta.DataType = attribute.AttributeType.Value.ToString();
                            ddMeta.Description = attribute.Description?.UserLocalizedLabel?.Label ?? string.Empty;
                            ddMeta.IsCustom = attribute.IsCustomAttribute ?? false;
                            ddMeta.AuditEnabled = attribute.IsAuditEnabled.Value;
                            //ddMeta.IsCalculated = attribute.IsCalculated.Value ?? false;
                            ddMeta.LangCode = attribute.DisplayName.UserLocalizedLabel?.LanguageCode ?? 0;
                            ddMeta.ModifiedOn = attribute.ModifiedOn ?? DateTime.MinValue;
                            ddMeta.AttributeOf = entity.MetadataId.ToString(); // Assuming AttributeOf is the Entity's MetadataId
                            ddMeta.AttributeType = attribute.AttributeType.Value.ToString();
                            //ddMeta.AttributeTypeName = attribute.AttributeTypeName ?? string.Empty;
                            //ddMeta.AutoNumberFormat = (attribute as AutoNumberAttributeMetadata)?.Format ?? string.Empty;
                            ddMeta.CanBeSecuredForCreate = attribute.CanBeSecuredForCreate;
                            ddMeta.CanBeSecuredForRead = attribute.CanBeSecuredForRead;
                            ddMeta.CanBeSecuredForUpdate = attribute.CanBeSecuredForUpdate;
                            //ddMeta.CanModifiedAdditionalSettings = attribute.CanModifyAdditionalSettings;
                            ddMeta.ColumnNumber = attribute.ColumnNumber;
                            ddMeta.CreatedOn = attribute.CreatedOn ?? DateTime.MinValue;
                            ddMeta.DeprecatedVersion = attribute.DeprecatedVersion ?? string.Empty;
                            ddMeta.DisplayName = attribute.DisplayName.UserLocalizedLabel?.Label ?? string.Empty;
                            ddMeta.EntityLogicalName = entity.LogicalName;
                            ddMeta.ExtensionData = attribute.ExtensionData?.ToString() ?? string.Empty;
                            ddMeta.ExternalName = attribute.ExternalName ?? string.Empty;
                            ddMeta.HasChanged = attribute.HasChanged ?? false;
                            ddMeta.InheritsFrom = attribute.InheritsFrom?.ToString() ?? string.Empty;
                            ddMeta.IntroducedVersion = attribute.IntroducedVersion ?? string.Empty;
                            //ddMeta.IsAuditEnabled = attribute.IsAuditEnabled ?? false;
                            ddMeta.IsCustomAttribute = attribute.IsCustomAttribute ?? false;
                            ddMeta.IsCustomizable = attribute.IsCustomizable?.Value ?? false;



                            switch (attribute.AttributeType)
                            {
                                //case AttributeTypeCode.Boolean:
                                //    break;

                                case AttributeTypeCode.BigInt:
                                    ddMeta.MinValue = (Int64?)((BigIntAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((BigIntAttributeMetadata)attribute).MaxValue;
                                    break;

                                //case AttributeTypeCode.CalendarRules:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((CalendarRulesAttributeMetadata)attribute).MinValue, ((BigIntAttributeMetadata)attribute).MaxValue);
                                //    break;

                                //case AttributeTypeCode.Customer:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((CustomerAttributeMetadata)attribute).MinValue, ((BigIntAttributeMetadata)attribute).MaxValue);
                                //    break;

                                case AttributeTypeCode.DateTime:

                                    ddMeta.FormulaDefinition = ((DateTimeAttributeMetadata)attribute).FormulaDefinition;
                                    FormulaDefinition = ((DateTimeAttributeMetadata)attribute).FormulaDefinition;
                                    break;

                                case AttributeTypeCode.Decimal:
                                    ddMeta.DataType = "Decimal";
                                    FormulaDefinition = ((DecimalAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((DecimalAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((DecimalAttributeMetadata)attribute).MaxValue;
                                    ddMeta.Precision = ((DecimalAttributeMetadata)attribute).Precision;
                                    break;

                                case AttributeTypeCode.Double:
                                    ddMeta.DataType = "Double";
                                    ddMeta.MinValue = (Int64?)((DoubleAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((DoubleAttributeMetadata)attribute).MaxValue;
                                    break;

                                //case AttributeTypeCode.EntityName:
                                //    // !! sb.AppendFormat("\t\t{0}\t{1}", ((EntityNameAttributeMetadata)attribute).IsPrimaryId, ((EntityNameAttributeMetadata)attribute).IsPrimaryName);
                                //    break;

                                case AttributeTypeCode.Integer:
                                    FormulaDefinition = ((IntegerAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((IntegerAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((IntegerAttributeMetadata)attribute).MaxValue;
                                    break;

                                case AttributeTypeCode.Lookup:
                                    ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).Targets != null
                                        ? string.Join(",", ((LookupAttributeMetadata)attribute).Targets)
                                        : null;

                                    ddMeta.LookupTo = ((LookupAttributeMetadata)attribute).Targets != null
                                        ? string.Join(",", ((LookupAttributeMetadata)attribute).Targets)
                                        : null;

                                    break;

                                //case AttributeTypeCode.ManagedProperty:
                                //    // sb.AppendFormat("\t{0}\t\t", ((ManagedPropertyAttributeMetadata)attribute));
                                //    break;

                                case AttributeTypeCode.Memo:
                                    ddMeta.DataType = "Memo";
                                    ddMeta.MaxLength = ((MemoAttributeMetadata)attribute).MaxLength;
                                    break;

                                case AttributeTypeCode.Money:
                                    ddMeta.DataType = "Money";
                                    FormulaDefinition = ((MoneyAttributeMetadata)attribute).FormulaDefinition;
                                    ddMeta.MinValue = (Int64?)((MoneyAttributeMetadata)attribute).MinValue;
                                    ddMeta.MaxValue = (Int64?)((MoneyAttributeMetadata)attribute).MaxValue;
                                    break;

                                //case AttributeTypeCode.Owner:
                                //    sb.AppendFormat("\t\t\t{0}\t{1}", ((OwnerAttributeMetadata)attribute).MinValue);
                                //    break;

                                //case AttributeTypeCode.PartyList:
                                //    sb.AppendFormat("\t\t{0}\t\t", ((PartyListAttributeMetadata)attribute).OptionSet.OptionSetType.ToString());
                                //    break;

                                case AttributeTypeCode.Picklist:
                                    ddMeta.DataType = "Picklist";
                                    FormulaDefinition = ((PicklistAttributeMetadata)attribute).FormulaDefinition;
                                    PicklistAM = (PicklistAttributeMetadata)attribute;

                                    switch (PicklistAM.OptionSet.Options.Count)
                                    {
                                        case 0:
                                            //sb.Append("\t\t\t");
                                            break;
                                        case 1:
                                            om = PicklistAM.OptionSet.Options[0];

                                            break;
                                        default:
                                            om = PicklistAM.OptionSet.Options[0];
                                            //sb.AppendFormat("\t{0}\t{1}\t{2}", om.Value, om.Label.UserLocalizedLabel.Label, om.Label.UserLocalizedLabel.LanguageCode);
                                            for (int j = 1; j < PicklistAM.OptionSet.Options.Count; j++)
                                            {
                                                //sb.AppendLine();
                                                //for (int i = 0; i < tabIndex; i++) { //sb.Append("\t"); }
                                                ////om = PicklistAM.OptionSet.Options[j];
                                                ////sb.AppendFormat("\t{0}\t{1}\t{2}", om.Value, om.Label.UserLocalizedLabel.Label, om.Label.UserLocalizedLabel.LanguageCode);
                                            }
                                            break;
                                    }
                                    //sb.AppendLine();
                                    break;

                                //case AttributeTypeCode.State:
                                //    sb.AppendFormat("\t{0}\t\t", ((StateAttributeMetadata)attribute).OptionSet.OptionSetType.ToString());
                                //    break;

                                //case AttributeTypeCode.Status:
                                //    sb.AppendFormat("\t{0}\t\t", ((StatusAttributeMetadata)attribute).OptionSet.OptionSetType.ToString());
                                //    break;

                                case AttributeTypeCode.String:
                                    ddMeta.DataType = "String";
                                    FormulaDefinition = ((StringAttributeMetadata)attribute).FormulaDefinition;
                                    //sb.AppendFormat("\t{0}\t{1}\t\t{2}\t\t\t"
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (FormulaDefinition.Trim().StartsWith("<?"))
                                    //    , String.IsNullOrEmpty(FormulaDefinition) ? false : (!FormulaDefinition.Trim().StartsWith("<?") && FormulaDefinition.Trim().Length > 0)
                                    //    , ((StringAttributeMetadata)attribute).MaxLength
                                    //    );
                                    break;

                                //case AttributeTypeCode.Uniqueidentifier:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((UniqueIdentifierAttributeMetadata)attribute).IsPrimaryId, ((UniqueIdentifierAttributeMetadata)attribute).MaxValue);
                                //    break;

                                //case AttributeTypeCode.Virtual:
                                //    sb.AppendFormat("\t\t{0}\t{1}", ((VirtualAttributeMetadata)attribute).MinValue, ((VirtualAttributeMetadata)attribute).MaxValue);
                                //    break;

                                default:
                                    break;
                            }

                            _ddSolutions["Default"].AttributeMetadata.Add(ddMeta);
                        }
                    }

                }
                else
                {
                    Console.Write("Failed to retrieve entity metadata");
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                om = null;
                PicklistAM = null;
                response = null;
                request = null;
            }
        }

        /// <summary>
        /// Correlates JavaScript field modifications with attribute metadata
        /// </summary>
        private void CorrelateJavaScriptModificationsWithAttributes()
        {
            Console.WriteLine("Correlating JavaScript modifications with attribute metadata...");

            foreach (var ddSolution in _ddSolutions.Values)
            {
                if (ddSolution.WebResources == null || ddSolution.AttributeMetadata == null)
                    continue;

                // Create a lookup dictionary for faster attribute metadata access
                var attributeLookup = ddSolution.AttributeMetadata
                    .GroupBy(attr => attr.ColumnLogical)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Process all field modifications from all web resources
                foreach (var webResource in ddSolution.WebResources)
                {
                    if (webResource.FieldModifications == null)
                        continue;

                    foreach (var modification in webResource.FieldModifications)
                    {
                        if (attributeLookup.TryGetValue(modification.FieldName, out var attributes))
                        {
                            foreach (var attribute in attributes)
                            {
                                // Add the modification to the attribute's collection and set parent reference
                                if (!attribute.JavaScriptFieldModifications.Contains(modification))
                                    attribute.JavaScriptFieldModifications.Add(modification);
                                modification.ParentAttribute = attribute;
                                modification.AttributeTable = attribute.Table;
                                modification.AttributeLogicalName = attribute.ColumnLogical;

                                // Update attribute metadata based on JavaScript modifications
                                switch (modification.ModificationType)
                                {
                                    case JavaScriptModificationType.Visibility:
                                        if (modification.ModificationValue?.ToLower() == "false")
                                        {
                                            attribute.IsHiddenByScript = true;
                                        }
                                        break;

                                    case JavaScriptModificationType.RequiredLevel:
                                        if (modification.ModificationValue?.ToLower() == "required")
                                        {
                                            attribute.IsRequiredByScript = true;
                                        }
                                        break;

                                    case JavaScriptModificationType.DefaultValue:
                                        attribute.HasDefaultValueByScript = true;
                                        if (string.IsNullOrWhiteSpace(attribute.ScriptDefaultValue))
                                        {
                                            attribute.ScriptDefaultValue = modification.ModificationValue;
                                        }
                                        else
                                        {
                                            // Multiple default values found
                                            attribute.ScriptDefaultValue += $"; {modification.ModificationValue}";
                                        }
                                        break;
                                }

                                // Track which web resources modify this field
                                var webResourceName = modification.WebResourceName ?? modification.WebResourceId.ToString();
                                if (string.IsNullOrWhiteSpace(attribute.ModifyingWebResources))
                                {
                                    attribute.ModifyingWebResources = webResourceName;
                                }
                                else if (!attribute.ModifyingWebResources.Contains(webResourceName))
                                {
                                    attribute.ModifyingWebResources += $"; {webResourceName}";
                                }
                            }
                        }
                    }
                }
            }

            // Log summary
            foreach (var ddSolution in _ddSolutions.Values)
            {
                if (ddSolution.AttributeMetadata == null) continue;

                var hiddenByScript = ddSolution.AttributeMetadata.Count(a => a.IsHiddenByScript == true);
                var requiredByScript = ddSolution.AttributeMetadata.Count(a => a.IsRequiredByScript == true);
                var hasDefaultByScript = ddSolution.AttributeMetadata.Count(a => a.HasDefaultValueByScript == true);

                Console.WriteLine($"Solution '{ddSolution.UniqueName}': {hiddenByScript} fields hidden by script, {requiredByScript} required by script, {hasDefaultByScript} with script defaults");
            }
        }

        private void SaveJavascriptToDataverse()
        {
            // Save all web resources to Dataverse (Upsert)
            foreach (var ddSolution in _ddSolutions.Values)
            {
                if (ddSolution.WebResources == null || ddSolution.WebResources.Count == 0)
                    continue;
                foreach (var webResource in ddSolution.WebResources)
                {
                    try
                    {
                        // --- Upsert logic: use ljr_displayname as unique key (adjust if you have a better unique key) ---
                        var query = new QueryExpression("ljr_webresource")
                        {
                            ColumnSet = new ColumnSet("ljr_webresourceid", "ljr_name", "ljr_displayname"),
                            Criteria = new FilterExpression
                            {
                                FilterOperator = LogicalOperator.And
                            }
                        };
                        query.Criteria.AddCondition("ljr_displayname", ConditionOperator.Equal, webResource.DisplayName);

                        var result = _service.RetrieveMultiple(query);

                        var entity = new Entity("ljr_webresource");
                        //entity["webresourceidunique"] = webResource.WebResourceId; // Use unique ID for updates if available
                        //entity["ljr_webresourceidunique"] = webResource.WebResourceId.ToString(); // Use string representation for consistency
                        entity["ljr_displayname"] = webResource.DisplayName;
                        entity["ljr_name"] = webResource.DisplayName;
                        entity["ljr_javascript"] = webResource.Content;
                        entity["ljr_dependencyxml"] = webResource.DependencyXml;
                        if (!string.IsNullOrWhiteSpace(webResource.ParsedDependenciesJson))
                            entity["ljr_parseddependencies"] = webResource.ParsedDependenciesJson;

                        Guid webResourceRecordId;
                        if (result.Entities.Count > 0)
                        {
                            // Update existing record
                            entity.Id = result.Entities[0].Id;
                            _service.Update(entity);
                            webResourceRecordId = entity.Id;
                            Console.WriteLine($"Updated Web Resource: {webResource.DisplayName} ({webResource.WebResourceId})");
                        }
                        else
                        {
                            // Create new record
                            webResourceRecordId = _service.Create(entity);
                            Console.WriteLine($"Created Web Resource: {webResource.DisplayName} ({webResource.WebResourceId})");
                        }

                        // Save dependencies for this web resource
                        SaveWebResourceDependenciesToDataverse(webResource, webResourceRecordId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error upserting Web Resource '{webResource.DisplayName}': {ex.Message}");
                    }
                }
            }

            // Save JavaScript field modifications to Dataverse
            SaveJavaScriptFieldModifications();
        }

        /// <summary>
        /// Saves each dependency of a web resource to a related table in Dataverse.
        /// </summary>
        /// <param name="webResource">The DataDictionaryWebResource instance.</param>
        /// <param name="webResourceRecordId">The Dataverse record ID of the parent web resource.</param>
        private void SaveWebResourceDependenciesToDataverse(DataDictionary.Models.DataDictionaryWebResource webResource, Guid webResourceRecordId)
        {
            if (webResource.ParsedDependencies == null || webResource.ParsedDependencies.Count == 0)
                return;

            foreach (var dependency in webResource.ParsedDependencies)
            {
                try
                {
                    var entity = new Entity("ljr_webresourcedependency");
                    entity["ljr_webresourceid"] = new EntityReference("ljr_webresource", webResourceRecordId);
                    if (dependency.AttributeId.HasValue)
                        entity["ljr_attributeid"] = dependency.AttributeId.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeName))
                        entity["ljr_attributename"] = dependency.AttributeName;
                    if (!string.IsNullOrWhiteSpace(dependency.EntityName))
                        entity["ljr_entityname"] = dependency.EntityName;
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeType))
                        entity["ljr_attributetype"] = dependency.AttributeType;
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeLogicalName))
                        entity["ljr_attributelogicalname"] = dependency.AttributeLogicalName;
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeType))
                        entity["ljr_componenttype"] = dependency.AttributeType;
                    if (!string.IsNullOrWhiteSpace(dependency.EntityName) && !string.IsNullOrWhiteSpace(dependency.AttributeLogicalName))
                    {
                        entity["ljr_webresourcedependencyname"] = dependency.EntityName + "-" + dependency.AttributeLogicalName;

                        // Query 1: Check if ljr_webresourcedependency exists (update or insert)
                        var dependencyQuery = new QueryExpression("ljr_webresourcedependency")
                        {
                            ColumnSet = new ColumnSet("ljr_webresourcedependencyid"),
                            Criteria = new FilterExpression
                            {
                                FilterOperator = LogicalOperator.And
                            }
                        };
                        dependencyQuery.Criteria.AddCondition("ljr_webresourceid", ConditionOperator.Equal, webResourceRecordId);
                        if (dependency.AttributeId.HasValue)
                            dependencyQuery.Criteria.AddCondition("ljr_attributeid", ConditionOperator.Equal, dependency.AttributeId.Value.ToString());
                        else if (!string.IsNullOrWhiteSpace(dependency.AttributeLogicalName) && !string.IsNullOrWhiteSpace(dependency.EntityName))
                        {
                            dependencyQuery.Criteria.AddCondition("ljr_attributelogicalname", ConditionOperator.Equal, dependency.AttributeLogicalName);
                            dependencyQuery.Criteria.AddCondition("ljr_entityname", ConditionOperator.Equal, dependency.EntityName);
                        }
                        var dependencyResult = _service.RetrieveMultiple(dependencyQuery);

                        // Query 2: Lookup ljr_datadictionaryattributemetadata by table and field name
                        Guid? attributeMetadataId = null;
                        if (!string.IsNullOrWhiteSpace(dependency.EntityName) && !string.IsNullOrWhiteSpace(dependency.AttributeLogicalName))
                        {
                            var attrMetaQuery = new QueryExpression("ljr_datadictionaryattributemetadata")
                            {
                                ColumnSet = new ColumnSet("ljr_datadictionaryattributemetadataid"),
                                Criteria = new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And
                                }
                            };
                            attrMetaQuery.Criteria.AddCondition("ljr_table", ConditionOperator.Equal, dependency.EntityName);
                            attrMetaQuery.Criteria.AddCondition("ljr_columnlogical", ConditionOperator.Equal, dependency.AttributeLogicalName);
                            var attrMetaResult = _service.RetrieveMultiple(attrMetaQuery);
                            if (attrMetaResult.Entities.Count > 0)
                            {
                                attributeMetadataId = attrMetaResult.Entities[0].GetAttributeValue<Guid>("ljr_datadictionaryattributemetadataid");
                                entity["ljr_attributemetadatalookup"] = new EntityReference("ljr_datadictionaryattributemetadata", attributeMetadataId.Value);
                            }
                        }

                        if (dependencyResult.Entities.Count > 0)
                        {
                            // Update existing record
                            entity.Id = dependencyResult.Entities[0].Id;
                            _service.Update(entity);
                        }
                        else
                        {
                            // Create new record
                            _service.Create(entity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving dependency for Web Resource '{webResource.DisplayName}': {ex.Message}");
                }
            }
        }

        private void SaveJavaScriptFieldModifications()
        {
            Console.WriteLine("Saving JavaScript field modifications to Dataverse...");

            foreach (var ddSolution in _ddSolutions.Values)
            {
                if (ddSolution.WebResources == null || ddSolution.WebResources.Count == 0)
                    continue;

                foreach (var webResource in ddSolution.WebResources)
                {
                    if (webResource.FieldModifications == null || webResource.FieldModifications.Count == 0)
                        continue;

                    foreach (var modification in webResource.FieldModifications)
                    {
                        try
                        {
                            // 1. Lookup ljr_webresource using ljr_displayname as the key to build the EntityReference
                            var webResourceQuery = new QueryExpression("ljr_webresource")
                            {
                                ColumnSet = new ColumnSet("ljr_webresourceid", "ljr_name", "ljr_displayname"),
                                Criteria = new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And
                                }
                            };
                            webResourceQuery.Criteria.AddCondition("ljr_displayname", ConditionOperator.Equal, webResource.DisplayName);
                            var webResourceResult = _service.RetrieveMultiple(webResourceQuery);

                            if (webResourceResult.Entities.Count == 0)
                            {
                                Console.WriteLine($"Web Resource '{webResource.DisplayName}' not found. Skipping modification save.");
                                continue;
                            }

                            var webResourceId = webResourceResult.Entities[0].GetAttributeValue<Guid>("ljr_webresourceid");

                            // 2. Lookup for the existence of the modification record (upsert logic)
                            var modificationQuery = new QueryExpression("ljr_javascriptfieldmodification")
                            {
                                ColumnSet = new ColumnSet("ljr_javascriptfieldmodificationid"),
                                Criteria = new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And
                                }
                            };
                            modificationQuery.Criteria.AddCondition("ljr_webresourcelookup", ConditionOperator.Equal, webResourceId);
                            modificationQuery.Criteria.AddCondition("ljr_fieldname", ConditionOperator.Equal, modification.FieldName);
                            modificationQuery.Criteria.AddCondition("ljr_modificationtype", ConditionOperator.Equal, modification.ModificationType.ToString());

                            var modificationResult = _service.RetrieveMultiple(modificationQuery);

                            var entity = new Entity("ljr_javascriptfieldmodification");
                            entity["ljr_webresourcelookup"] = new EntityReference("ljr_webresource", webResourceId);
                            entity["ljr_fieldname"] = modification.FieldName;
                            entity["ljr_modificationtype"] = modification.ModificationType.ToString();
                            entity["ljr_modificationvalue"] = modification.ModificationValue;
                            entity["ljr_javascriptcode"] = modification.JavaScriptCode;
                            entity["ljr_linenumber"] = modification.LineNumber;
                            entity["ljr_notes"] = modification.Notes;
                            entity["ljr_parsedon"] = modification.ParsedOn;
                            entity["ljr_name"] = $"{webResource.DisplayName} - {modification.FieldName} - ({modification.ModificationType})";

                            if (modificationResult.Entities.Count > 0)
                            {
                                // Update existing record
                                entity.Id = modificationResult.Entities[0].Id;
                                _service.Update(entity);
                                Console.WriteLine($"Updated JavaScript Field Modification: {modification.FieldName} ({modification.ModificationType})");
                            }
                            else
                            {
                                // Create new record
                                _service.Create(entity);
                                Console.WriteLine($"Created JavaScript Field Modification: {modification.FieldName} ({modification.ModificationType})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving JavaScript Field Modification '{modification.FieldName}' for Web Resource '{webResource.DisplayName}': {ex.Message}");
                        }
                    }
                }
            }
        }

        private void SaveToDataverse()
        {
            // Use ExecuteMultipleRequest for batch saving to Dataverse
            var batchSize = 1000; // Adjust as needed for performance and Dataverse limits

            // Track alternate key values to detect duplicates in the current batch
            var seenAltKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ddSolution in _ddSolutions.Values)
            {
                if (ddSolution.AttributeMetadata == null || ddSolution.AttributeMetadata.Count == 0)
                    continue;

                var requests = new List<OrganizationRequest>();
                foreach (var attrMeta in ddSolution.AttributeMetadata)
                {
                    // Construct the alternate key value
                    var altKey = attrMeta.Table + "-" + attrMeta.ColumnLogical;

                    if (string.IsNullOrWhiteSpace(altKey))
                    {
                        Console.WriteLine("Skipping record with null/empty alternate key.");
                        continue;
                    }

                    // Check for duplicate alternate key in the current batch
                    if (!seenAltKeys.Add(altKey))
                    {
                        Console.WriteLine($"Duplicate alternate key detected in batch: '{altKey}'. Skipping this record.");
                        continue;
                    }

                    var entity = new Entity("ljr_datadictionaryattributemetadata");
                    entity["ljr_datadictionaryattributemetadata1"] = altKey;
                    entity["ljr_table"] = attrMeta.Table;
                    entity["ljr_columndisplay"] = attrMeta.ColumnDisplay;
                    entity["ljr_columnlogical"] = attrMeta.ColumnLogical;
                    entity["ljr_columnschema"] = attrMeta.ColumnSchema;
                    entity["ljr_datatype"] = attrMeta.DataType;
                    entity["ljr_formuladefinition"] = attrMeta.FormulaDefinition;
                    entity["ljr_iscustom"] = attrMeta.IsCustom;
                    entity["ljr_auditenabled"] = attrMeta.AuditEnabled;
                    entity["ljr_iscalculated"] = attrMeta.IsCalculated;
                    entity["ljr_isformula"] = attrMeta.IsFormula;
                    entity["ljr_lookupto"] = attrMeta.LookupTo;
                    entity["ljr_metadatamodifiedon"] = attrMeta.ModifiedOn != DateTime.MinValue ? (object)attrMeta.ModifiedOn : null;
                    entity["ljr_attributeof"] = attrMeta.AttributeOf;
                    entity["ljr_attributetype"] = attrMeta.AttributeType;
                    entity["ljr_attributetypename"] = attrMeta.AttributeTypeName;
                    entity["ljr_autonumberformat"] = attrMeta.AutoNumberFormat;
                    entity["ljr_canbesecuredforcreate"] = attrMeta.CanBeSecuredForCreate ?? false;
                    entity["ljr_canbesecuredforread"] = attrMeta.CanBeSecuredForRead ?? false;
                    entity["ljr_canbesecuredforupdate"] = attrMeta.CanBeSecuredForUpdate ?? false;
                    entity["ljr_canmodifiedadditionalsettings"] = attrMeta.CanModifiedAdditionalSettings ?? false;
                    entity["ljr_columnnumber"] = attrMeta.ColumnNumber ?? 0;
                    entity["ljr_deprecatedversion"] = attrMeta.DeprecatedVersion;
                    entity["ljr_displayname"] = attrMeta.DisplayName;
                    entity["ljr_entitylogicalname"] = attrMeta.EntityLogicalName;
                    entity["ljr_externalname"] = attrMeta.ExternalName;
                    entity["ljr_haschanged"] = attrMeta.HasChanged ?? false;
                    entity["ljr_inheritsfrom"] = attrMeta.InheritsFrom;
                    entity["ljr_introducedversion"] = attrMeta.IntroducedVersion;
                    entity["ljr_isauditenabled"] = attrMeta.IsAuditEnabled ?? false;
                    entity["ljr_metadatacreatedon"] = attrMeta.CreatedOn != DateTime.MinValue ? (object)attrMeta.CreatedOn : null;

                    // Only add MaxValue, MinValue, Precision, MaxLength if not null
                    if (attrMeta.MaxValue != null)
                        entity["ljr_maxvalue"] = attrMeta.MaxValue;
                    if (attrMeta.MinValue != null)
                        entity["ljr_minvalue"] = attrMeta.MinValue;
                    if (attrMeta.Precision != null)
                        entity["ljr_precision"] = attrMeta.Precision;
                    if (attrMeta.MaxLength != null)
                        entity["ljr_maxlength"] = attrMeta.MaxLength;

                    if (attrMeta.OptionSet != null)
                        entity["ljr_optionset"] = attrMeta.OptionSet;
                    if (attrMeta.Value != null)
                        entity["ljr_value"] = attrMeta.Value;

                    entity["ljr_description"] = attrMeta.Description;
                    entity["ljr_langcode"] = attrMeta.LangCode;

                    // JavaScript modification fields
                    entity["ljr_ishiddenbyscript"] = attrMeta.IsHiddenByScript ?? false;
                    entity["ljr_isrequiredbyscript"] = attrMeta.IsRequiredByScript ?? false;
                    entity["ljr_hasdefaultvaluebyscript"] = attrMeta.HasDefaultValueByScript ?? false;
                    if (!string.IsNullOrWhiteSpace(attrMeta.ScriptDefaultValue))
                        entity["ljr_scriptdefaultvalue"] = attrMeta.ScriptDefaultValue;
                    if (!string.IsNullOrWhiteSpace(attrMeta.ModifyingWebResources))
                        entity["ljr_modifyingwebresources"] = attrMeta.ModifyingWebResources;

                    // --- Check if record exists in Dataverse by alternate key ---
                    bool exists = false;
                    try
                    {
                        var query = new QueryExpression("ljr_datadictionaryattributemetadata")
                        {
                            ColumnSet = new ColumnSet("ljr_datadictionaryattributemetadataid"),
                            Criteria = new FilterExpression
                            {
                                FilterOperator = LogicalOperator.And
                            }
                        };
                        query.Criteria.AddCondition("ljr_datadictionaryattributemetadata1", ConditionOperator.Equal, altKey);

                        var result = _service.RetrieveMultiple(query);
                        if (result.Entities.Count > 0)
                        {
                            exists = true;
                            entity.Id = result.Entities[0].Id; // Set the ID for update
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking existence for alternate key '{altKey}': {ex.Message}");
                    }

                    if (exists)
                    {
                        // Use UpdateRequest if record exists
                        var updateRequest = new UpdateRequest { Target = entity };
                        requests.Add(updateRequest);
                        Console.WriteLine($"UpdateRequest queued for alternate key: {altKey}");
                    }
                    else
                    {
                        // Use CreateRequest if record does not exist
                        var createRequest = new CreateRequest { Target = entity };
                        requests.Add(createRequest);
                        Console.WriteLine($"CreateRequest queued for alternate key: {altKey}");
                    }

                    // Send batch if batchSize reached
                    if (requests.Count == batchSize)
                    {
                        ExecuteBatch(requests);
                        requests.Clear();
                        seenAltKeys.Clear(); // Clear for next batch
                    }
                }

                // Send any remaining requests
                if (requests.Count > 0)
                {
                    ExecuteBatch(requests);
                }
            }
        }

        private void ExecuteBatch(List<OrganizationRequest> requests)
        {
            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Requests = new OrganizationRequestCollection(),
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                }
            };

            foreach (var req in requests)
            {
                executeMultipleRequest.Requests.Add(req);
            }

            try
            {
                var response = (ExecuteMultipleResponse)_service.Execute(executeMultipleRequest);
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    var item = response.Responses[i];
                    var req = executeMultipleRequest.Requests[item.RequestIndex];
                    Entity entity = null;
                    string altKey = null;
                    if (req is UpdateRequest updateReq)
                    {
                        entity = updateReq.Target as Entity;
                        altKey = entity?["ljr_datadictionaryattributemetadata1"] as string;
                    }
                    else if (req is CreateRequest createReq)
                    {
                        entity = createReq.Target as Entity;
                        altKey = entity?["ljr_datadictionaryattributemetadata1"] as string;
                    }

                    if (item.Fault != null)
                    {
                        Console.WriteLine($"Failed to save attribute metadata (Alternate Key: '{altKey}'): {item.Fault.Message}");
                        if (item.Fault.InnerFault != null)
                        {
                            Console.WriteLine("Inner Fault: " + item.Fault.InnerFault.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Saved attribute metadata: {altKey}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Batch save failed: " + ex.ToString());
            }
        }

        #endregion

    }

    public class DataDictionaryWebResource
    {
        public Guid WebResourceId { get; set; }
        public string DisplayName { get; set; }
        public string Content { get; set; }
        public string DependencyXml { get; set; }
        public List<DataDictionaryJavaScriptFieldModification> FieldModifications { get; set; } = new List<DataDictionaryJavaScriptFieldModification>();
        public List<string> ApiPatterns { get; set; } = new List<string>();
        public List<WebResourceDependency> ParsedDependencies { get; private set; } = new List<WebResourceDependency>();
        public string ParsedDependenciesJson => JsonConvert.SerializeObject(ParsedDependencies);

        // Used for tracking, but not saved to Dataverse
        public HashSet<string> ModifiedTables { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // NOT SAVED TO DATAVERSE
        public HashSet<string> ModifiedAttributes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // NOT SAVED TO DATAVERSE

        public void ParseDependencies()
        {
            ParsedDependencies.Clear();

            if (string.IsNullOrWhiteSpace(DependencyXml))
                return;

            var xml = XDocument.Parse(DependencyXml);
            foreach (var dependencyElement in xml.Descendants("Dependency"))
            {
                var componentType = dependencyElement.Attribute("componentType")?.Value;

                foreach (var attributeElement in dependencyElement.Elements("Attribute"))
                {
                    var dependency = new WebResourceDependency();
                    dependency.EntityName = attributeElement.Attribute("entityName")?.Value;
                    dependency.AttributeName = attributeElement.Attribute("attributeName")?.Value;
                    dependency.AttributeId = Guid.TryParse(attributeElement.Attribute("attributeId")?.Value, out var attrId) ? (Guid?)attrId : null;
                    dependency.AttributeType = attributeElement.Attribute("attributeType")?.Value;
                    dependency.AttributeLogicalName = attributeElement.Attribute("attributeLogicalName")?.Value;
                    ParsedDependencies.Add(dependency);

                    // Track tables and attributes from dependencies  
                    if (!string.IsNullOrWhiteSpace(dependency.EntityName))
                        ModifiedTables.Add(dependency.EntityName);
                    if (!string.IsNullOrWhiteSpace(dependency.AttributeLogicalName))
                        ModifiedAttributes.Add(dependency.AttributeLogicalName);
                }
            }
        }
    }

    public class WebResourceDependency
    {
        public string ComponentType { get; set; }
        public Guid? AttributeId { get; set; }
        public string AttributeName { get; set; }
        public string EntityName { get; set; }
        public string AttributeType { get; set; }
        public string AttributeLogicalName { get; set; }
    }
}

