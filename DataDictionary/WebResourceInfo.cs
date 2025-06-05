namespace DataDictionary
{
    /// <summary>
    /// Represents a web resource in Dataverse, such as a JavaScript file.
    /// </summary>
    public class WebResourceInfo
    {
        /// <summary>
        /// The unique identifier of the web resource.
        /// </summary>
        public System.Guid Id { get; set; }

        /// <summary>
        /// The name (logical name) of the web resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The display name of the web resource.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The description of the web resource.
        /// </summary>
        public string Description { get; set; }
    }
}
