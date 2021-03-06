﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace ConveyorBelt.Tooling
{
    public class ElasticsearchClient : IElasticsearchClient
    {
        private IHttpClient _httpClient;

        private const string IndexFormat = "{0}/{1}";
        private const string IndexSearchFormat = "{0}/{1}/_search?size=0";
        private const string MappingFormat = "{0}/{1}/{2}/_mapping";

        public ElasticsearchClient(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> CreateIndexIfNotExistsAsync(string baseUrl, string indexName)
        {
            baseUrl = baseUrl.TrimEnd('/');
            string searchUrl = string.Format(IndexSearchFormat, baseUrl, indexName);
            var response = await _httpClient.GetAsync(searchUrl);

            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return false;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var url = string.Format(IndexFormat, baseUrl, indexName);
                var result = await _httpClient.PutAsJsonAsync(url, string.Empty);
                result.EnsureSuccessStatusCode();
                return true;
            }
            else
            {
                throw new ApplicationException(string.Format("Error {0}: {1}",
                    response.StatusCode,
                    text));
            }
        }

        public async Task<bool> MappingExistsAsync(string baseUrl, string indexName, string typeName)
        {
            baseUrl = baseUrl.TrimEnd('/');
            var url = string.Format(MappingFormat, baseUrl, indexName, typeName);
            var response = await _httpClient.GetAsync(url);
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && text != "{}")
            {
                return true;
            }
            if (response.StatusCode == HttpStatusCode.NotFound || text == "{}")
            {
                return false;
            }
            else
            {
                throw new ApplicationException(string.Format("Error {0}: {1}",
                    response.StatusCode,
                    text));
            }
        }

        public async Task<bool> UpdateMappingAsync(string baseUrl, string indexName, string typeName, string mapping)
        {
            baseUrl = baseUrl.TrimEnd('/');
            var url = string.Format(MappingFormat, baseUrl, indexName, typeName);
            var response = await _httpClient.PutAsync(url, 
                new StringContent(mapping, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                throw new ApplicationException(string.Format("Error {0}: {1}",
                    response.StatusCode,
                    text));
            }
        }

    }
}
