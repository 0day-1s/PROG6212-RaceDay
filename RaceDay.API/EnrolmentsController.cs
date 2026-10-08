using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Filters;
using RaceDay.API.Models;
using RaceDay.Contracts;

namespace RaceDay.API
{
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // POST /api/events/{eventId}/enrolments - Participant only
        [RequireRole("Participant")]
        [HttpPost("api/events/{eventId}/enrolments")]
        public async Task<ActionResult<EnrolmentDto>> Enrol(int eventId, CreateEnrolmentRequest request)
        {
            var participantId = HttpContext.Session.GetInt32("UserId")!.Value;

            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
            {
                return NotFound("Event not found.");
            }

            var categoryValid = await _context.Categories
                .AnyAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
            if (!categoryValid)
            {
                return BadRequest("The selected category does not belong to this event.");
            }

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(en => en.ParticipantId == participantId && en.EventId == eventId);
            if (alreadyEnrolled)
            {
                return Conflict("You are already enrolled in this event.");
            }

            var enrolment = new Enrolment
            {
                ParticipantId = participantId,
                EventId = eventId,
                CategoryId = request.CategoryId,
                EnrolmentDate = DateTime.UtcNow,
                EnrolmentStatus = "Pending"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            var dto = new EnrolmentDto
            {
                EnrolmentId = enrolment.EnrolmentId,
                ParticipantId = enrolment.ParticipantId,
                EventId = enrolment.EventId,
                CategoryId = enrolment.CategoryId,
                EnrolmentDate = enrolment.EnrolmentDate,
                EnrolmentStatus = enrolment.EnrolmentStatus
            };

            return CreatedAtAction(nameof(GetMyEnrolments), null, dto);
        }

        // GET /api/enrolments/mine - Participant only, their own enrolments
        [RequireRole("Participant")]
        [HttpGet("api/enrolments/mine")]
        public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMyEnrolments()
        {
            var participantId = HttpContext.Session.GetInt32("UserId")!.Value;

            var enrolments = await _context.Enrolments
                .Where(en => en.ParticipantId == participantId)
                .Select(en => new EnrolmentDto
                {
                    EnrolmentId = en.EnrolmentId,
                    ParticipantId = en.ParticipantId,
                    EventId = en.EventId,
                    CategoryId = en.CategoryId,
                    EnrolmentDate = en.EnrolmentDate,
                    EnrolmentStatus = en.EnrolmentStatus
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET /api/enrolments/event/{eventId} - Organiser only, must own the event
        [RequireRole("Organiser")]
        [HttpGet("api/enrolments/event/{eventId}")]
        public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetEnrolmentsForEvent(int eventId)
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

            var enrolments = await _context.Enrolments
                .Where(en => en.EventId == eventId)
                .Select(en => new EnrolmentDto
                {
                    EnrolmentId = en.EnrolmentId,
                    ParticipantId = en.ParticipantId,
                    EventId = en.EventId,
                    CategoryId = en.CategoryId,
                    EnrolmentDate = en.EnrolmentDate,
                    EnrolmentStatus = en.EnrolmentStatus
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // PUT /api/enrolments/{id}/status - Organiser only, must own the parent event
        [RequireRole("Organiser")]
        [HttpPut("api/enrolments/{id}/status")]
        public async Task<IActionResult> UpdateEnrolmentStatus(int id, UpdateEnrolmentStatusRequest request)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

            var enrolment = await _context.Enrolments
                .Include(en => en.Event)
                .FirstOrDefaultAsync(en => en.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound();
            }

            if (enrolment.Event.OrganiserId != organiserId)
            {
                return StatusCode(403, "You do not own the event this enrolment belongs to.");
            }

            if (request.EnrolmentStatus != "Pending" && request.EnrolmentStatus != "Confirmed")
            {
                return BadRequest("Status must be 'Pending' or 'Confirmed'.");
            }

            enrolment.EnrolmentStatus = request.EnrolmentStatus;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}