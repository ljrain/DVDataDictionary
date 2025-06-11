namespace DataDictionary
{
    /// <summary>
    /// Represents a web resource in Dataverse, such as a JavaScript file.
    /// </summary>
    public class WebResourceInfo
    {
        /// <summary>
        /// The name (logical name) of the web resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The display name of the web resource.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The path of the web resource.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The type of the web resource.
        /// </summary>
        public int? WebResourceType { get; set; }

        /// <summary>
        /// The unique identifier of the web resource.
        /// </summary>
        public string Guid { get; set; }

        private void UpsertEntity(string entityName, Dictionary<string, object> attributes, string keyField, object keyValue)
        {
            var entity = new Entity(entityName);
            foreach (var kvp in attributes)
                entity[kvp.Key] = kvp.Value;

            var query = new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet($"{entityName}id"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression(keyField, ConditionOperator.Equal, keyValue) }
                }
            };
            var results = _service.RetrieveMultiple(query);
            if (results.Entities.Count > 0)
            {
                entity.Id = results.Entities[0].Id;
                _service.Update(entity);
            }
            else
            {
                _service.Create(entity);
            }
        }
    }
}
