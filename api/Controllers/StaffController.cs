using Microsoft.AspNetCore.Authorization;
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
                    Secure = Request.IsHttps
                }
            );
            return NoContent();
        }

       
        [HttpGet, Produces("application/json"), Route("reservations"), Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> GetReservations()
        {         
            var reservations = await _reservations.GetUpcomingReservations();
            return Json(reservations);
        }
    }
}
