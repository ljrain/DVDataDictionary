using System;

namespace DataDictionary
{
    /// <summary>
    /// Enhanced field form location information
    /// Now part of the comprehensive DataDictionaryAttributeMetadata model
    /// This standalone class is maintained for backward compatibility
    /// </summary>
    public class FieldFormLocation
    {
        public string FormName { get; set; }
        public string FormId { get; set; }
        public string TabName { get; set; }
        public string SectionName { get; set; }
        public bool IsVisible { get; set; }
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public int? Row { get; set; }
        public int? Column { get; set; }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public FieldFormLocation()
        {
        }

        /// <summary>
        /// Enhanced constructor with all properties
        /// </summary>
        public FieldFormLocation(string formName, string formId, string tabName, string sectionName, 
            bool isVisible = true, bool isRequired = false, bool isReadOnly = false, int? row = null, int? column = null)
        {
            FormName = formName;
            FormId = formId;
            TabName = tabName;
            SectionName = sectionName;
            IsVisible = isVisible;
            IsRequired = isRequired;
            IsReadOnly = isReadOnly;
            Row = row;
            Column = column;
        }

        public override string ToString()
        {
            return $"{FormName} > {TabName} > {SectionName} (Row: {Row}, Col: {Column})";
        }
    }
}