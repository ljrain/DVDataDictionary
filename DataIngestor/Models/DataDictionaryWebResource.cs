using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace DataDictionary.Models
{
    public class DataDictionaryWebResource
    {
        public Guid WebResourceId { get; set; }
        public string DisplayName { get; set; }
        public string Content { get; set; }
        public string DependencyXml { get; set; }
        public string Type { get; set; }
        public List<DataDictionaryJavaScriptFieldModification> FieldModifications { get; set; } = new List<DataDictionaryJavaScriptFieldModification>();
        public List<string> ApiPatterns { get; set; } = new List<string>();
        public List<WebResourceDependency> ParsedDependencies { get; private set; } = new List<WebResourceDependency>();
        public string ParsedDependenciesJson => JsonConvert.SerializeObject(ParsedDependencies);

        public void ParseDependencies()
        {
            if (string.IsNullOrWhiteSpace(DependencyXml))
            {
                ParsedDependencies.Clear();
                return;
            }

            try
            {
                var xml = XDocument.Parse(DependencyXml);
                foreach (var element in xml.Descendants("Dependency"))
                {
                    var dependency = new WebResourceDependency
                    {
                        ComponentType = element.Attribute("type")?.Value,
                        AttributeName = element.Attribute("name")?.Value
                    };
                    ParsedDependencies.Add(dependency);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing dependencies: {ex.Message}");
            }
        }
    }
}
