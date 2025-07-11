﻿using System;
using System.Collections.Generic;

namespace DataDictionary.Models
{
    public class DataDictionarySolution
    {
        public List<DataDictionaryAttributeMetadata> AttributeMetadata { get; set; } = new List<DataDictionaryAttributeMetadata>();

        public string FriendlyName { get; set; }
        public string SolutionId { get; set; }
        public string UniqueName { get; set; }

        public List<DataDictionarySolutionComponent> Components { get; set; } = new List<DataDictionarySolutionComponent>();

        public void AddComponent(DataDictionarySolutionComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            Components.Add(component);
        }
        public void RemoveComponent(DataDictionarySolutionComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            Components.Remove(component);
        }

        public List<DataDictionaryEntity> Entities { get; set; } = new List<DataDictionaryEntity>();

        public void AddEntity(DataDictionaryEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Entities.Add(entity);
        }

        public void RemoveEntity(DataDictionaryEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Entities.Remove(entity);
        }

        public void AddAttribute(DataDictionarySolution ddSolution, DataDictionaryAttribute ddAttr)
        { 
            foreach (DataDictionaryEntity ddEntity in ddSolution.Entities)
            {
                if (ddEntity.EntityId == ddAttr.AttributeOf)
                {
                    ddEntity.AddAttribute(ddAttr);
                    return;
                }
                else
                { 
                    Console.WriteLine($"Entity {ddEntity.EntityId} does not match AttributeOf {ddAttr.AttributeOf}");
                }
            }
        }
    }
}

