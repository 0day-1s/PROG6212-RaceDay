using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Filters;
using RaceDay.Contracts;

namespace RaceDay.API
{
    [Route("api/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ProfileController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET /api/profile/me - any logged-in user, their own profile
        [RequireRole]
        [HttpGet("me")]
        public async Task<ActionResult<ProfileDto>> GetMyProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var dto = new ProfileDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber
            };

            return Ok(dto);
        }

        // PUT /api/profile/me - any logged-in user, updates their own profile only
        [RequireRole]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}