using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Filters;
using RaceDay.API.Models;
using RaceDay.Contracts;

namespace RaceDay.API
{
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // POST /api/results - Organiser only, must own the event tied to the enrolment
        [RequireRole("Organiser")]
        [HttpPost("api/results")]
        public async Task<ActionResult<ResultDto>> CreateResult(CreateResultRequest request)
        {
            var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

            var enrolment = await _context.Enrolments
                .Include(en => en.Event)
                .FirstOrDefaultAsync(en => en.EnrolmentId == request.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound("Enrolment not found.");
            }

            if (enrolment.Event.OrganiserId != organiserId)
            {
                return StatusCode(403, "You do not own the event this enrolment belongs to.");
            }

            var alreadyHasResult = await _context.Results
                .AnyAsync(r => r.EnrolmentId == request.EnrolmentId);
            if (alreadyHasResult)
            {
                return Conflict("A result has already been captured for this enrolment.");
            }

            var result = new Result
            {
                EnrolmentId = request.EnrolmentId,
                FinishTime = request.FinishTime,
                FinishPosition = request.FinishPosition,
                TotalFinishers = request.TotalFinishers,
                CapturedBy = organiserId
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var dto = new ResultDto
            {
                ResultId = result.ResultId,
                EnrolmentId = result.EnrolmentId,
                FinishTime = result.FinishTime,
                FinishPosition = result.FinishPosition,
                TotalFinishers = result.TotalFinishers,
                CapturedBy = result.CapturedBy
            };

            return CreatedAtAction(nameof(GetMyResults), null, dto);
        }

        // GET /api/results/mine - Participant only, their own results
        [RequireRole("Participant")]
        [HttpGet("api/results/mine")]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetMyResults()
        {
            var participantId = HttpContext.Session.GetInt32("UserId")!.Value;

            var results = await _context.Results
                .Include(r => r.Enrolment)
                .Where(r => r.Enrolment.ParticipantId == participantId)
                .Select(r => new ResultDto
                {
                    ResultId = r.ResultId,
                    EnrolmentId = r.EnrolmentId,
                    FinishTime = r.FinishTime,
                    FinishPosition = r.FinishPosition,
                    TotalFinishers = r.TotalFinishers,
                    CapturedBy = r.CapturedBy
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET /api/results/event/{eventId} - Organiser only, must own the event
        [RequireRole("Organiser")]
        [HttpGet("api/results/event/{eventId}")]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetResultsForEvent(int eventId)
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

            var results = await _context.Results
                .Include(r => r.Enrolment)
                .Where(r => r.Enrolment.EventId == eventId)
                .Select(r => new ResultDto
                {
                    ResultId = r.ResultId,
                    EnrolmentId = r.EnrolmentId,
                    FinishTime = r.FinishTime,
                    FinishPosition = r.FinishPosition,
                    TotalFinishers = r.TotalFinishers,
                    CapturedBy = r.CapturedBy
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}