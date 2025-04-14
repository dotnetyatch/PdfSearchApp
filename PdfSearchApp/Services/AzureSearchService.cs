using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PdfSearchApp.Services
{
    public class AzureSearchService
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _indexClient;
        private readonly string _indexName;

        public AzureSearchService(IConfiguration config)
        {
            var serviceEndpoint = new Uri(config["AzureSearch:ServiceEndpoint"]);
            var apiKey = new AzureKeyCredential(config["AzureSearch:ApiKey"]);
            _indexName = config["AzureSearch:IndexName"];

            _indexClient = new SearchIndexClient(serviceEndpoint, apiKey);
            _searchClient = new SearchClient(serviceEndpoint, _indexName, apiKey);
        }

        public async Task CreateIndexAsync()
        {
            var index = new SearchIndex(_indexName)
            {
                Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("content"),
                new SimpleField("url", SearchFieldDataType.String)
            }
            };
            await _indexClient.CreateOrUpdateIndexAsync(index);
        }

        public async Task AddDocumentAsync(string id, string content, string url)
        {
            var doc = new { id, content, url };
            await _searchClient.UploadDocumentsAsync(new[] { doc });
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = await _searchClient.SearchAsync<SearchResult>(query);

            // Convert Pageable<SearchResult> to List<SearchResult>
            List<SearchResult> searchResults = new List<SearchResult>();

            await foreach (var result in results.Value.GetResultsAsync())
            {
                searchResults.Add(result.Document);
            }

            return searchResults;
        }


        public class SearchResult
        {
            public string Content { get; set; }
            public string Url { get; set; }
            public string Title { get; set; }
        }
    }
}

/*
 * In Azure Cognitive Search, you need to create an index where the documents (PDFs) will be stored and searchable.

3.1 Create a Search Index
Go to your Azure Cognitive Search resource in the Azure Portal.

Under Indexing, click on "Indexes".

Click "+ Add Index" to create a new index. Define the fields to hold the PDF content (e.g., Content, Metadata).

Example index structure:
Content (type: Edm.String): Stores the actual text from the PDF.
Url (type: Edm.String): Stores the URL of the document for linking in search results.
Create the index once all fields are defined.
 */