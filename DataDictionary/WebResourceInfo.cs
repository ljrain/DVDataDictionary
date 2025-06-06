using System;

namespace DataDictionary
{
    /// <summary>  
    /// Represents metadata information about a web resource in Dataverse.  
    /// </summary>  
    public class WebResourceInfo
    {
        /// <summary>  
        /// Gets or sets the unique identifier of the web resource.  
        /// </summary>  
        public Guid Id { get; set; }

        /// <summary>  
        /// Gets or sets the name of the web resource.  
        /// </summary>  
        public string Name { get; set; }

        /// <summary>  
        /// Gets or sets the display name of the web resource.  
        /// </summary>  
        public string DisplayName { get; set; }

        /// <summary>  
        /// Gets or sets the type of the web resource (e.g., JavaScript, HTML, etc.).  
        /// </summary>  
        public string Type { get; set; }

        /// <summary>  
        /// Gets or sets the content of the web resource.  
        /// </summary>  
        public string Content { get; set; }

        /// <summary>  
        /// Gets or sets the solution name associated with the web resource.  
        /// </summary>  
        public string SolutionName { get; set; }
    }
}
