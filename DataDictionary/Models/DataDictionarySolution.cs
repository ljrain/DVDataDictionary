using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDictionary.Models
{
    public class DataDictionarySolution
    {
        public string Name { get; set; }
        public string SolutionId { get; set; }

        public IEnumerable<DataDictionarySolutionComponent> Components { get; set; } = new List<DataDictionarySolutionComponent>();

        public void AddComponent(DataDictionarySolutionComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            ((List<DataDictionarySolutionComponent>)Components).Add(component);
        }
        public void RemoveComponent(DataDictionarySolutionComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            ((List<DataDictionarySolutionComponent>)Components).Remove(component);
        }

        public IEnumerable<DataDictionaryEntity> Entities { get; set; } = new List<DataDictionaryEntity>();

        public void AddEntity(DataDictionaryEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            ((List<DataDictionaryEntity>)Entities).Add(entity);
        }

        public void RemoveEntity(DataDictionaryEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            ((List<DataDictionaryEntity>)Entities).Remove(entity);
        }

    }

}

