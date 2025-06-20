using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDictionary.Models
{
    public class DataDictionaryWebResource
    {
        public Guid WebResourceId { get; set; }
        public string DisplayName { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }

        /// <summary>
        /// List of JavaScript field modifications found in this web resource
        /// </summary>
        public List<DataDictionaryJavaScriptFieldModification> FieldModifications { get; set; } = new List<DataDictionaryJavaScriptFieldModification>();

        /// <summary>
        /// List of raw Dataverse API patterns found in this web resource (legacy)
        /// </summary>
        public List<string> ApiPatterns { get; set; } = new List<string>();
    }
}
