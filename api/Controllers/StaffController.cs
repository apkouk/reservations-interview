using Microsoft.AspNetCore.Mvc;
using Models;
using Repositories;

namespace Controllers
{
    [Tags("Staff"), Route("staff")]
    public class StaffController : Controller
    {
        private IConfiguration Config { get; set; }
        private ReservationRepository _reservations { get; set; }

        public StaffController(IConfiguration config, ReservationRepository reservations)
        {
            Config = config;
            _reservations = reservations;
        }

        /// <summary>
        /// Checks if the request is from a staff member, if not returns true and a 403 result
        /// </summary>
        /// <param name="request"></param>
        private static bool IsNotStaff(HttpRequest request, out IActionResult? result)
            => StaffAuth.IsNotStaff(request, out result);

        [HttpGet, Route("login")]
        public IActionResult CheckCode([FromHeader(Name = "X-Staff-Code")] string accessCode)
        {
            var configuredSecret = Config.GetValue<string>("staffAccessCode");
            if (configuredSecret != accessCode)
            {
                return StatusCode(403);
            }
            Response.Cookies.Append(
                "access",
                "1",
                new CookieOptions
                {
                    IsEssential = true,
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = true,
                    Secure = false
                }
            );
            return NoContent();
        }

        [HttpGet, Route("check")]
        public IActionResult CheckCookie()
        {
            if (IsNotStaff(Request, out IActionResult? result))
            {
                return result!;
            }

            return Ok("Authorized");
        }

        [HttpGet, Produces("application/json"), Route("reservations")]
        public async Task<IActionResult> GetReservations()
        {
            if (IsNotStaff(Request, out IActionResult? result))
            {
                return result!;
            }

            var reservations = await _reservations.GetUpcomingReservations();
            return Json(reservations);
        }
    }
}
