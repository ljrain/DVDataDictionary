using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDictionary.Models
{
    public class BADDataDictionaryAttribute
    {
        public Guid AttributeId { get; set; }
        public string AttributeName { get; set; }
        public Guid SchemaId { get; set; }
    }
}
