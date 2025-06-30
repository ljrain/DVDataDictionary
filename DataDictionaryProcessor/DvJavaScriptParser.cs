using DataDictionary.Models;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataDictionaryProcessor
{
    public class DvJavaScriptParser
    {
        private CrmServiceClient _serviceClient;
        private Dictionary<string, DataDictionarySolution> _ddSolutions;
        private List<string> _allowedLogicalNames;
        private Dictionary<string, List<string>> _allowedTableAttributes;

        // Constructor matching the usage in DvCollector.cs
        public DvJavaScriptParser(
            CrmServiceClient serviceClient,
            Dictionary<string, DataDictionarySolution> ddSolutions,
            List<string> allowedLogicalNames,
            Dictionary<string, List<string>> allowedTableAttributes)
        {
            _serviceClient = serviceClient;
            _ddSolutions = ddSolutions;
            _allowedLogicalNames = allowedLogicalNames;
            _allowedTableAttributes = allowedTableAttributes;
        }

        /// <summary>
        /// Parses JavaScript code to identify field modifications (visibility, required level, default values, etc.)
        /// </summary>
        /// <param name="script">The JavaScript code to parse (may be base64-encoded).</param>
        /// <param name="webResourceId">The ID of the web resource containing this script.</param>
        /// <param name="webResourceName">The name of the web resource containing this script.</param>
        /// <returns>List of field modifications found in the script.</returns>
        public List<DataDictionaryJavaScriptFieldModification> ParseFieldModifications(string script, Guid webResourceId, string webResourceName)
        {
            var modifications = new List<DataDictionaryJavaScriptFieldModification>();
            if (string.IsNullOrWhiteSpace(script))
                return modifications;

            // Try to decode script from base64 if it looks like base64
            string decodedScript = script;
            try
            {
                // Heuristic: base64 is usually longer, only contains base64 chars, and not valid JS
                // If script is not valid JS (e.g. contains no semicolons or function keywords), try decode
                bool looksLikeBase64 = script.Length % 4 == 0 &&
                    Regex.IsMatch(script, @"^[A-Za-z0-9+/=\r\n]+$") &&
                    !script.Contains("function") && !script.Contains(";") && !script.Contains("var ") && !script.Contains("let ") && !script.Contains("const ");

                if (looksLikeBase64)
                {
                    var bytes = Convert.FromBase64String(script);
                    decodedScript = Encoding.UTF8.GetString(bytes);
                }
            }
            catch
            {
                // If decoding fails, just use the original script
                decodedScript = script;
            }

            var scriptLines = decodedScript.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

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


    }
}
