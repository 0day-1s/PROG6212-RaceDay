using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Filters;
using RaceDay.API.Models;
using RaceDay.Contracts;

namespace RaceDay.API
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET /api/events - both roles (and public) can view
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
        {
            var events = await _context.Events
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    EventType = e.EventType,
                    Distance = e.Distance,
                    OrganiserId = e.OrganiserId,
                    VenueId = e.VenueId,
                    BannerImageUrl = e.BannerImageUrl
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET /api/events/{id} - both roles (and public) can view
        [HttpGet("{id}")]
        public async Task<ActionResult<EventDto>> GetEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            var dto = new EventDto
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                EventType = ev.EventType,
                Distance = ev.Distance,
                OrganiserId = ev.OrganiserId,
                VenueId = ev.VenueId,
                BannerImageUrl = ev.BannerImageUrl
            };

            return Ok(dto);
        }

        // GET /api/events/mine - Organiser only, their own events
        [RequireRole("Organiser")]
        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetMyEvents()
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

            var events = await _context.Events
                .Where(e => e.OrganiserId == organiserId)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    EventType = e.EventType,
                    Distance = e.Distance,
                    OrganiserId = e.OrganiserId,
                    VenueId = e.VenueId,
                    BannerImageUrl = e.BannerImageUrl
                })
                .ToListAsync();

            return Ok(events);
        }

        // POST /api/events - Organiser only
        [RequireRole("Organiser")]
        [HttpPost]
        public async Task<ActionResult<EventDto>> CreateEvent(CreateEventRequest request)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

            var venueExists = await _context.Venues.AnyAsync(v => v.VenueId == request.VenueId);
            if (!venueExists)
            {
                return BadRequest("The specified venue does not exist.");
            }

            var ev = new Event
            {
                EventName = request.EventName,
                Description = request.Description,
                EventDate = request.EventDate,
                EventType = request.EventType,
                Distance = request.Distance,
                VenueId = request.VenueId,
                OrganiserId = organiserId
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            var dto = new EventDto
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                EventType = ev.EventType,
                Distance = ev.Distance,
                OrganiserId = ev.OrganiserId,
                VenueId = ev.VenueId,
                BannerImageUrl = ev.BannerImageUrl
            };

            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, dto);
        }

        // PUT /api/events/{id} - Organiser only, must own the event
        [RequireRole("Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, UpdateEventRequest request)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != organiserId)
            {
                return StatusCode(403, "You do not own this event.");
            }

            ev.EventName = request.EventName;
            ev.Description = request.Description;
            ev.EventDate = request.EventDate;
            ev.EventType = request.EventType;
            ev.Distance = request.Distance;
            ev.VenueId = request.VenueId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/events/{id} - Organiser only, must own the event
        [RequireRole("Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserId != organiserId)
            {
                return StatusCode(403, "You do not own this event.");
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}