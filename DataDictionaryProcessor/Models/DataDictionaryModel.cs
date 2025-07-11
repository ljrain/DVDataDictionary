using System.Collections.Generic;

namespace DataDictionary.Models
{
    /// <summary>
    /// Represents the root DataDictionary structure.
    /// </summary>
    public class DataDictionary
    {
        public DataDictionary() { }

        public List<string> Solutions { get; set; } = new List<string>();

        public Dictionary<string, DataDictionaryEntity> Entities { get; set; } = new Dictionary<string, DataDictionaryEntity>();

        public Dictionary<string, DataDictionaryWebResource> WebResources { get; set; } = new Dictionary<string, DataDictionaryWebResource>();

    }

}