using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.Contracts;

namespace RaceDay.API
{
    [Route("api/venues")]
    [ApiController]
    public class VenuesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public VenuesController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<VenueDto>>> GetAll()
        {
            var venues = await _context.Venues
                .OrderBy(v => v.VenueName)
                .Select(v => new VenueDto
                {
                    VenueId = v.VenueId,
                    VenueName = v.VenueName,
                    City = v.City,
                    Province = v.Province
                })
                .ToListAsync();

            return Ok(venues);
        }
    }
}