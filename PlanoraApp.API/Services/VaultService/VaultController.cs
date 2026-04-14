using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanoraApp.API.Infrastructure;

namespace PlanoraApp.API.Services.VaultService
{
    [ApiController]
    [Route("api/v1/vault")]
    public class VaultController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IBlobStorageService _blobService;
        private readonly IOcrParserService _ocrService;

        public VaultController(AppDbContext db, IBlobStorageService blobService, IOcrParserService ocrService)
        {
            _db = db;
            _blobService = blobService;
            _ocrService = ocrService;
        }

        [HttpGet("{tripId}")]
        public async Task<IActionResult> GetVaultItems(Guid tripId)
        {
            var items = await _db.VaultItems
                .Where(v => v.TripId == tripId)
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] Guid tripId, [FromForm] Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            // 1. Upload to Blob Storage
            using var stream = file.OpenReadStream();
            var blobUrl = await _blobService.UploadDocumentAsync(file.FileName, stream, file.ContentType);

            // 2. Extact text + structure using Azure AI Document Intelligence
            var parsedJson = await _ocrService.ParseTravelDocumentAsync(blobUrl);

            // 3. Save to SQL
            var vaultItem = new VaultItem
            {
                TripId = tripId,
                UserId = userId,
                Type = file.ContentType.Contains("pdf") ? "document" : "image",
                BlobUrl = blobUrl,
                ParsedData = parsedJson,
                Source = "upload"
            };

            _db.VaultItems.Add(vaultItem);
            await _db.SaveChangesAsync();

            return Ok(vaultItem);
        }

        [HttpDelete("{itemId}")]
        public async Task<IActionResult> DeleteItem(Guid itemId)
        {
            var item = await _db.VaultItems.FindAsync(itemId);
            if (item == null) return NotFound();

            // Delete blob
            await _blobService.DeleteDocumentAsync(item.BlobUrl);

            // Delete entity
            _db.VaultItems.Remove(item);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
