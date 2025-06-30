using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace DataDictionary.Models
{
    /// <summary>
    /// Represents the root DataDictionary structure.
    /// </summary>
    public class DataDictionary
    {
        public DataDictionary() { }
        
        public List<string> Solutions { get; set; } = new List<string>();

        public Dictionary<string,DataDictionaryEntity> Entities { get; set; } = new Dictionary<string, DataDictionaryEntity>();



    }

}