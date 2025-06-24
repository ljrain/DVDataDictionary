using DataDictionary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataIngestor.Processors
{
    /// <summary>
    /// Handles parsing JavaScript content to extract field modifications and API patterns
    /// </summary>
    public class JavaScriptParser
    {
        /// <summary>
        /// Pattern for matching different types of JavaScript field modifications
        /// </summary>
        private class JavaScriptPattern
        {
            public Regex Regex { get; set; }
            public string Type { get; set; }
            public int ValueGroup { get; set; }
        }

        private readonly List<JavaScriptPattern> _primaryPatterns;
        private readonly List<JavaScriptPattern> _advancedPatterns;
        private readonly List<string> _apiPatterns;

        public JavaScriptParser()
        {
            InitializePatterns();
        }

        /// <summary>
        /// Initializes the regex patterns for detecting JavaScript modifications
        /// </summary>
        private void InitializePatterns()
        {
            _primaryPatterns = new List<JavaScriptPattern>
            {
                new JavaScriptPattern
                {
                    Regex = new Regex(@"formContext\.getControl\([""']([^""']+)[""']\)\.setVisible\(false\)", RegexOptions.IgnoreCase),
                    Type = "Hidden",
                    ValueGroup = 1
                },
                new JavaScriptPattern
                {
                    Regex = new Regex(@"formContext\.getAttribute\([""']([^""']+)[""']\)\.setRequiredLevel\([""']required[""']\)", RegexOptions.IgnoreCase),
                    Type = "Required",
                    ValueGroup = 1
                },
                new JavaScriptPattern
                {
                    Regex = new Regex(@"formContext\.getAttribute\([""']([^""']+)[""']\)\.setValue\(([^)]+)\)", RegexOptions.IgnoreCase),
                    Type = "DefaultValue",
                    ValueGroup = 2
                },
                new JavaScriptPattern
                {
                    Regex = new Regex(@"formContext\.getControl\([""']([^""']+)[""']\)\.setDisabled\(true\)", RegexOptions.IgnoreCase),
                    Type = "Disabled",
                    ValueGroup = 1
                }
            };

            _advancedPatterns = new List<JavaScriptPattern>
            {
                new JavaScriptPattern
                {
                    Regex = new Regex(@"Xrm\.Page\.getAttribute\([""']([^""']+)[""']\)\.setRequiredLevel\([""']([^""']+)[""']\)", RegexOptions.IgnoreCase),
                    Type = "RequiredLevel",
                    ValueGroup = 2
                },
                new JavaScriptPattern
                {
                    Regex = new Regex(@"Xrm\.Page\.getControl\([""']([^""']+)[""']\)\.setVisible\(([^)]+)\)", RegexOptions.IgnoreCase),
                    Type = "Visibility",
                    ValueGroup = 2
                }
            };

            _apiPatterns = new List<string>
            {
                @"Xrm\.WebApi\.retrieveRecord",
                @"Xrm\.WebApi\.updateRecord",
                @"Xrm\.WebApi\.createRecord",
                @"Xrm\.WebApi\.deleteRecord",
                @"Xrm\.Api\.execute",
                @"Xrm\.WebApi\.retrieveMultipleRecords"
            };
        }

        /// <summary>
        /// Parses JavaScript content to extract field modifications
        /// </summary>
        /// <param name="script">The JavaScript content to parse</param>
        /// <param name="webResourceId">The ID of the web resource containing the script</param>
        /// <param name="webResourceName">The name of the web resource</param>
        /// <returns>List of detected field modifications</returns>
        public List<DataDictionaryJavaScriptFieldModification> ParseFieldModifications(string script, Guid webResourceId, string webResourceName)
        {
            if (string.IsNullOrWhiteSpace(script))
                return new List<DataDictionaryJavaScriptFieldModification>();

            var modifications = new List<DataDictionaryJavaScriptFieldModification>();

            try
            {
                var lines = script.Split('\n');

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    var line = lines[lineIndex];
                    
                    // Process primary patterns first
                    foreach (var pattern in _primaryPatterns)
                    {
                        var matches = pattern.Regex.Matches(line);
                        foreach (Match match in matches)
                        {
                            var modification = new DataDictionaryJavaScriptFieldModification
                            {
                                FieldName = match.Groups[1].Value,
                                WebResourceId = webResourceId,
                                WebResourceName = webResourceName,
                                ModificationType = pattern.Type,
                                ModificationValue = pattern.ValueGroup > 1 ? match.Groups[pattern.ValueGroup].Value : match.Groups[1].Value,
                                JavaScriptCode = line.Trim(),
                                LineNumber = lineIndex + 1,
                                ParsedOn = DateTime.UtcNow
                            };

                            modifications.Add(modification);
                            Console.WriteLine($"Found {pattern.Type} modification for field '{modification.FieldName}': {modification.ModificationValue}");
                        }
                    }

                    // Process advanced patterns only if no primary patterns matched
                    bool foundInPrimary = _primaryPatterns.Any(p => p.Regex.IsMatch(line));
                    if (!foundInPrimary)
                    {
                        foreach (var pattern in _advancedPatterns)
                        {
                            var matches = pattern.Regex.Matches(line);
                            foreach (Match match in matches)
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
                }

                Console.WriteLine($"Parsed {modifications.Count} field modifications from {webResourceName}");
                return modifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing field modifications for {webResourceName}: {ex.Message}");
                return modifications;
            }
        }

        /// <summary>
        /// Parses JavaScript content to extract API patterns and field modifications
        /// </summary>
        /// <param name="script">The JavaScript content to parse</param>
        /// <param name="webResource">Optional web resource to populate with results</param>
        /// <returns>List of found API patterns</returns>
        public List<string> ParseJavaScript(string script, DataDictionaryWebResource webResource = null)
        {
            if (string.IsNullOrWhiteSpace(script))
                return new List<string>();

            var foundPatterns = new List<string>();

            try
            {
                foreach (var pattern in _apiPatterns)
                {
                    var matches = Regex.Matches(script, pattern);
                    if (matches.Count > 0)
                    {
                        foundPatterns.Add(pattern);
                        Console.WriteLine($"Found API pattern: {pattern} ({matches.Count} occurrences)");
                    }
                }

                // Parse field modifications if web resource is provided
                if (webResource != null)
                {
                    webResource.FieldModifications = ParseFieldModifications(script, webResource.WebResourceId, webResource.DisplayName);
                    webResource.ApiPatterns = foundPatterns.ToList();

                    // Extract modified tables and attributes for tracking
                    foreach (var modification in webResource.FieldModifications)
                    {
                        if (!string.IsNullOrWhiteSpace(modification.FieldName))
                        {
                            webResource.ModifiedAttributes.Add(modification.FieldName);
                            
                            // Extract table name if field follows table.field pattern
                            var parts = modification.FieldName.Split('.');
                            if (parts.Length > 1)
                            {
                                webResource.ModifiedTables.Add(parts[0]);
                            }
                        }
                    }
                }

                Console.WriteLine($"JavaScript parsing completed. Found {foundPatterns.Count} API patterns");
                return foundPatterns;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JavaScript: {ex.Message}");
                return foundPatterns;
            }
        }

        /// <summary>
        /// Validates if a field modification pattern is supported
        /// </summary>
        /// <param name="modificationType">The type of modification to validate</param>
        /// <returns>True if the modification type is supported</returns>
        public bool IsValidModificationType(string modificationType)
        {
            if (string.IsNullOrWhiteSpace(modificationType))
                return false;

            var supportedTypes = _primaryPatterns.Concat(_advancedPatterns)
                                              .Select(p => p.Type)
                                              .Distinct()
                                              .ToList();

            return supportedTypes.Contains(modificationType, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets all supported modification types
        /// </summary>
        /// <returns>List of supported modification types</returns>
        public List<string> GetSupportedModificationTypes()
        {
            return _primaryPatterns.Concat(_advancedPatterns)
                                 .Select(p => p.Type)
                                 .Distinct()
                                 .ToList();
        }

        /// <summary>
        /// Gets all supported API patterns
        /// </summary>
        /// <returns>List of supported API patterns</returns>
        public List<string> GetSupportedApiPatterns()
        {
            return _apiPatterns.ToList();
        }
    }
}