using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PdfSearchApp.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PdfSearchApp.Controllers
{
    [Route("search")]
    public class SearchController : Controller
    {
        private readonly BlobStorageService _blobStorage;
        private readonly DocumentProcessingService _docProcessing;
        private readonly AzureSearchService _searchService;

        public SearchController(BlobStorageService blobStorage, DocumentProcessingService docProcessing, AzureSearchService searchService)
        {
            _blobStorage = blobStorage;
            _docProcessing = docProcessing;
            _searchService = searchService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Invalid file.");

            var url = await _blobStorage.UploadFileAsync(file.OpenReadStream(), file.FileName);
            await _docProcessing.ProcessDocumentAsync(url);

            ViewData["UploadMessage"] = "✅ File uploaded and indexed successfully.";
            return View("~/Views/Home/Index.cshtml");
            // return Ok(new { Message = "File uploaded and indexed.", Url = url });
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View(); // If no query is provided, return the view with no results
            }

            // Call the search service to get the results based on the query
            var results = await _searchService.SearchAsync(query);

            // Pass the results to the view using ViewData (or ViewBag)
            ViewData["Results"] = results;

            // Return the same view with results populated
            return View("~/Views/Home/Index.cshtml");
        }

    }
}
