using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDictionary.Models
{
    public class DataDictionarySolutionComponent
    {
        public string ComponentType { get; set; }
        public string ComponentTypeName { get; set; }
        public bool IsMetadata { get; set; }
        public string IsMetadataName { get; set; }
        public Guid RootComponentBehavior { get; set; }
        public string RootComponentBehaviorName { get; set; }
        public Guid RootSolutionComponentId { get; set; }
    }
}
