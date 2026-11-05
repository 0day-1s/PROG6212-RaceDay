using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Filters;
using RaceDay.API.Services;

namespace RaceDay.API
{
    [ApiController]
    public class UploadsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly BlobStorageService _blobs;

        public UploadsController(RaceDayDbContext context, BlobStorageService blobs)
        {
            _context = context;
            _blobs = blobs;
        }

        // POST /api/events/{id}/banner - Organiser only, must own the event
        [RequireRole("Organiser")]
        [HttpPost("api/events/{id:int}/banner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            var ev = await _context.Events.FindAsync(id);

            if (ev == null) return NotFound();
            if (ev.OrganiserId != organiserId) return StatusCode(403, "You do not own this event.");

            try
            {
                string url = await _blobs.UploadAsync(file, "event-banners");
                ev.BannerImageUrl = url;
                await _context.SaveChangesAsync();
                return Ok(new { imageUrl = url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST /api/profile/me/picture - any logged-in user, own profile only
        [RequireRole]
        [HttpPost("api/profile/me/picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            try
            {
                string url = await _blobs.UploadAsync(file, "profile-pictures");
                user.ProfileImageUrl = url;
                await _context.SaveChangesAsync();
                return Ok(new { imageUrl = url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET /api/images/{blobName} - streams an image from the private container
        [HttpGet("api/images/{**blobName}")]
        public async Task<IActionResult> GetImage(string blobName)
        {
            bool allowed = !blobName.Contains("..") &&
                           (blobName.StartsWith("event-banners/") || blobName.StartsWith("profile-pictures/"));
            if (!allowed) return NotFound();

            var image = await _blobs.OpenReadAsync(blobName);
            if (image == null) return NotFound();

            Response.Headers.CacheControl = "public,max-age=3600";
            return File(image.Value.Content, image.Value.ContentType);
        }
    }
}