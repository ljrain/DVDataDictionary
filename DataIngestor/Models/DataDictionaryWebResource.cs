using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataIngestor.Models
{
    public class DataDictionaryWebResource
    {
        public Guid WebResourceId { get; set; }
        public string DisplayName { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
    }
}
