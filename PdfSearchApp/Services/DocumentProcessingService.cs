using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PdfSearchApp.Services
{
    public class DocumentProcessingService
    {
        private readonly DocumentAnalysisClient _client;
        private readonly SearchClient _searchClient;

        public DocumentProcessingService(IConfiguration config)
        {
            //Initialize Form Recognizer client
            _client = new DocumentAnalysisClient(
                new Uri(config["AzureAI:Endpoint"]),
                new AzureKeyCredential(config["AzureAI:ApiKey"])
            );
            //var credential = new ManagedIdentityCredential();
            //_client = new DocumentAnalysisClient(new Uri(config["AzureAI:Endpoint"]), credential);
            // Initialize Azure Cognitive Search client
            var searchCredential = new AzureKeyCredential(config["AzureSearch:ApiKey"]);
            _searchClient = new SearchClient(new Uri(config["AzureSearch:ServiceEndpoint"]), config["AzureSearch:IndexName"], searchCredential);
        }

        // Extract text from the provided document URL using Azure Form Recognizer
        public async Task<string> ExtractTextAsync(string documentUrl)
        {
            Uri fileUri = new Uri(documentUrl);
            AnalyzeDocumentOperation operation = await _client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, "prebuilt-read", fileUri);
            AnalyzeResult result = operation.Value;

            // Extract text from all pages
            string extractedText = string.Join("\n", result.Pages.SelectMany(p => p.Lines.Select(l => l.Content)));

            return extractedText;
        }

        // Index the extracted text into Azure Cognitive Search
        public async Task IndexExtractedTextAsync(string extractedText, string documentUrl)
        {
            Uri uri = new Uri(documentUrl);

            // Extract the file name from the URL (last part after the last "/")
            string fileName = Path.GetFileName(uri.AbsolutePath);
            // Create the document to match the Azure Cognitive Search index schema
            var document = new SearchDocument
            {
                { "id", Guid.NewGuid().ToString() },
                { "Content", extractedText },  // Store the extracted text
                { "Url", documentUrl },         // Store the URL of the document (PDF)
                { "Title", fileName }         // Store the URL of the document (PDF)
            };

            // Create an IndexDocumentsBatch and upload the document
            var batch = IndexDocumentsBatch.Upload(new[] { document });

            // Upload the document to the search index
            await _searchClient.IndexDocumentsAsync(batch);
        }

        // Combined method to process the document (extract text and index)
        public async Task ProcessDocumentAsync(string documentUrl)
        {
            // Step 1: Extract text from the PDF document using Azure Form Recognizer
            var extractedText = await ExtractTextAsync(documentUrl);

            // Step 2: Index the extracted text into Azure Cognitive Search
            await IndexExtractedTextAsync(extractedText, documentUrl);
        }
    }
}
