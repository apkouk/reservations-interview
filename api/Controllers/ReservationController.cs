using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.Errors;
using Repositories;
using Validators;

namespace Controllers
{
    [Tags("Reservations"), Route("reservation")]
    public class ReservationController : Controller
    {
        private ReservationRepository _repo { get; set; }
        private RoomRepository _roomRepo { get; set; }
        private GuestRepository _guestRepo { get; set; }

        public ReservationController(ReservationRepository reservationRepository, RoomRepository roomRepository, GuestRepository guestRepository)
        {
            _repo = reservationRepository;
            _roomRepo = roomRepository;
            _guestRepo = guestRepository;
        }

        [HttpGet, Produces("application/json"), Route(""), Authorize(Policy = "StaffOnly")]
        public async Task<ActionResult<Reservation>> GetReservations()
        {
            var reservations = await _repo.GetReservations();

            return Json(reservations);
        }

        [HttpGet, Produces("application/json"), Route("{reservationId}"), Authorize(Policy = "StaffOnly")]
        public async Task<ActionResult<Reservation>> GetRoom(Guid reservationId)
        {
            try
            {
                var reservation = await _repo.GetReservation(reservationId);
                return Json(reservation);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Create a new reservation, to generate the GUID ID on the server, send an Empty GUID (all 0s)
        /// </summary>
        /// <param name="newBooking"></param>
        /// <returns></returns>
        [HttpPost, Produces("application/json"), Route(""), AllowAnonymous]
        public async Task<ActionResult<Reservation>> BookReservation(
            [FromBody] Reservation? newBooking
        )
        {
            if (newBooking is null)
            {
                return BadRequest("Invalid reservation payload.");
            }

            // Provide a real ID if one is not provided
            if (newBooking.Id == Guid.Empty)
            {
                newBooking.Id = Guid.NewGuid();
            }

            try
            {
                BookingValidator.Validate(newBooking);

                // Ensure the guest record exists, creating one on-demand if this is
                // their first booking (the UI only supplies an email, not a full profile).
                await _guestRepo.GetOrCreateGuest(newBooking.GuestEmail);

                // Verify the room exists
                await _roomRepo.GetRoom(newBooking.RoomNumber);

                var createdReservation = await _repo.CreateReservation(newBooking);

                // CreatedAtAction delegates URL generation to the routing infrastructure,
                // so it automatically prepends whatever PathBase (e.g. /api) the host or reverse proxy has configured.
                // The Location header will now always be a correctly-rooted URL that resolves to GET /reservation/{id}
                // regardless of where the app is mounted.
                return CreatedAtAction(nameof(GetRoom), new { reservationId = createdReservation.Id }, createdReservation);
            }
            catch (ConflictException ex)
            {
                return Conflict(ex.Message);
            }
            catch (InvalidBooking ex)
            {
                return BadRequest(ex.Message);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured when trying to book a reservation:");
                Console.WriteLine(ex.ToString());

                return BadRequest("Invalid reservation");
            }
        }


        [HttpDelete, Produces("application/json"), Route("{reservationId}"), Authorize(Policy = "StaffOnly")]
        public async Task<IActionResult> DeleteReservation(Guid reservationId)
        {
            var result = await _repo.DeleteReservation(reservationId);

            return result ? NoContent() : NotFound();
        }

        [HttpPost, Produces("application/json"), Route("{reservationId}/checkin"), Authorize(Policy = "StaffOnly")]
        public async Task<ActionResult<Reservation>> CheckIn(Guid reservationId, [FromBody] CheckInRequest? request)
        {
            if (request is null || string.IsNullOrEmpty(request.GuestEmail))
            {
                return BadRequest("Guest email is required.");
            }

            try
            {
                var reservation = await _repo.GetReservation(reservationId);
                var room = await _roomRepo.GetRoom(reservation.RoomNumber);

                if (room.State != Models.State.Ready)
                {
                    return BadRequest("Cannot check in: room is not ready for check-in.");
                }

                var checkedIn = await _repo.CheckIn(reservationId, request.GuestEmail);
                await _roomRepo.SetRoomState(checkedIn.RoomNumber, Models.State.Occupied);
                return Json(checkedIn);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public record CheckInRequest(string GuestEmail);
}
