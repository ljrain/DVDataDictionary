using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace DataIngestor
{
    public class DataverseWebApiHelper
    {
        private readonly string _baseUrl;
        private readonly string _accessToken;

        public DataverseWebApiHelper(string baseUrl, string accessToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _accessToken = accessToken;
        }

        public async Task<List<EntityDefinitionResult>> GetEntityDefinitionsAsync()
        {
            var entities = new List<EntityDefinitionResult>();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_baseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Query EntityDefinitions with Attributes expanded
                var url = "/api/data/v9.2/EntityDefinitions?$select=LogicalName,DisplayName&$expand=Attributes($select=LogicalName,AttributeType,DisplayName)";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                foreach (var entity in json["value"])
                {
                    var entityResult = new EntityDefinitionResult
                    {
                        LogicalName = entity["LogicalName"]?.ToString(),
                        DisplayName = entity["DisplayName"]?["UserLocalizedLabel"]?["Label"]?.ToString(),
                        Attributes = new List<AttributeDefinitionResult>()
                    };

                    if (entity["Attributes"] != null)
                    {
                        foreach (var attr in entity["Attributes"])
                        {
                            entityResult.Attributes.Add(new AttributeDefinitionResult
                            {
                                LogicalName = attr["LogicalName"]?.ToString(),
                                AttributeType = attr["AttributeType"]?.ToString(),
                                DisplayName = attr["DisplayName"]?["UserLocalizedLabel"]?["Label"]?.ToString()
                            });
                        }
                    }

                    entities.Add(entityResult);
                }
            }
            return entities;
        }

        public static async Task<string> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret, string resource)
        {
            var authContext = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");
            var clientCred = new ClientCredential(clientId, clientSecret);
            var result = await authContext.AcquireTokenAsync(resource, clientCred);
            return result.AccessToken;
        }
    }

    public class EntityDefinitionResult
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public List<AttributeDefinitionResult> Attributes { get; set; }
    }

    public class AttributeDefinitionResult
    {
        public string LogicalName { get; set; }
        public string AttributeType { get; set; }
        public string DisplayName { get; set; }
    }
}
