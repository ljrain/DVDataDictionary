using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDictionary.Models
{
    public class DataDictionarySolutionComponent
    {
        public Guid ObjectId { get; set; }
        public int ComponentType { get; set; }
        public string ComponentTypeName { get; set; }
        public bool IsMetadata { get; set; }
        public string IsMetadataName { get; set; }
        public int RootComponentBehavior { get; set; }
        public string RootComponentBehaviorName { get; set; }
        public Guid RootSolutionComponentId { get; set; }

        public override string ToString()
        {
                
            return $"{ComponentTypeName} (Type: {ComponentType}, Is Metadata: {IsMetadataName})";
        }


    }
}
