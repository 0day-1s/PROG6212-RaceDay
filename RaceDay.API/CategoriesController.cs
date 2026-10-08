using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Filters;
using RaceDay.API.Models;
using RaceDay.Contracts;

namespace RaceDay.API
{
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET /api/events/{eventId}/categories - both roles (and public) can view
        [HttpGet("api/events/{eventId}/categories")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategoriesForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
            {
                return NotFound("Event not found.");
            }

            var categories = await _context.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    EventId = c.EventId,
                    CategoryName = c.CategoryName,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST /api/events/{eventId}/categories - Organiser only, must own the event
        [RequireRole("Organiser")]
        [HttpPost("api/events/{eventId}/categories")]
        public async Task<ActionResult<CategoryDto>> CreateCategory(int eventId, CreateCategoryRequest request)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
            {
                return NotFound("Event not found.");
            }

            if (ev.OrganiserId != organiserId)
            {
                return StatusCode(403, "You do not own this event.");
            }

            var category = new Category
            {
                EventId = eventId,
                CategoryName = request.CategoryName,
                Description = request.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var dto = new CategoryDto
            {
                CategoryId = category.CategoryId,
                EventId = category.EventId,
                CategoryName = category.CategoryName,
                Description = category.Description
            };

            return CreatedAtAction(nameof(GetCategoriesForEvent), new { eventId }, dto);
        }

        // PUT /api/categories/{id} - Organiser only, must own the parent event
        [RequireRole("Organiser")]
        [HttpPut("api/categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, CreateCategoryRequest request)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Event.OrganiserId != organiserId)
            {
                return StatusCode(403, "You do not own the event this category belongs to.");
            }

            category.CategoryName = request.CategoryName;
            category.Description = request.Description;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/categories/{id} - Organiser only, must own the parent event
        [RequireRole("Organiser")]
        [HttpDelete("api/categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Event.OrganiserId != organiserId)
            {
                return StatusCode(403, "You do not own the event this category belongs to.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}