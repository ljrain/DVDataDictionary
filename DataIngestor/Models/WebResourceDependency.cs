using System;

namespace DataDictionary.Models
{
    /// <summary>
    /// Represents a dependency parsed from a web resource's DependencyXml.
    /// </summary>
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