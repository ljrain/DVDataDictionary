using System.Collections.Generic;
using DataDictionary.Models;

namespace DataDictionary
{
    /// <summary>
    /// Legacy field metadata class - DEPRECATED
    /// Use DataDictionaryAttributeMetadata for comprehensive attribute information
    /// This class is kept for backward compatibility but should be phased out
    /// </summary>
    [System.Obsolete("Use DataDictionaryAttributeMetadata instead", false)]
    public class FieldMetadata
    {
        public string EntityFieldName { get; set; } // Logical name of the field in the entity
        public string EntityName { get; set; }
        public string SchemaName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string RequiredLevel { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public List<FieldFormLocation> FormLocations { get; set; }
        public List<string> ScriptReferences { get; set; }
        public bool HiddenByScript { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanCreate { get; set; }
        public string Permissions { get; set; }
        public List<string> SolutionNames { get; set; }

        /// <summary>
        /// Convert to the new comprehensive metadata model
        /// </summary>
        /// <returns>Enhanced metadata object</returns>
        public DataDictionaryAttributeMetadata ToDataDictionaryAttributeMetadata()
        {
            return new DataDictionaryAttributeMetadata
            {
                LogicalName = this.EntityFieldName,
                EntityLogicalName = this.EntityName,
                SchemaName = this.SchemaName,
                DisplayName = this.DisplayName,
                Description = this.Description,
                DataType = this.Type,
                RequiredLevel = this.RequiredLevel,
                MaxLength = this.MaxLength,
                Precision = this.Precision,
                MinValue = this.MinValue,
                MaxValue = this.MaxValue,
                FormLocations = this.FormLocations ?? new List<FieldFormLocation>(),
                ScriptReferences = this.ScriptReferences ?? new List<string>(),
                IsValidForRead = this.CanRead,
                IsValidForUpdate = this.CanWrite,
                IsValidForCreate = this.CanCreate,
                SolutionNames = this.SolutionNames ?? new List<string>()
            };
        }
    }
}
