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

        [HttpGet, Produces("application/json"), Route("")]
        public async Task<ActionResult<Reservation>> GetReservations()
        {
            var reservations = await _repo.GetReservations();

            return Json(reservations);
        }

        [HttpGet, Produces("application/json"), Route("{reservationId}")]
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
        [HttpPost, Produces("application/json"), Route("")]
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

                // Verify the guest exists
                await _guestRepo.GetGuestByEmail(newBooking.GuestEmail);

                // Verify the room exists
                await _roomRepo.GetRoom(newBooking.RoomNumber);

                var createdReservation = await _repo.CreateReservation(newBooking);
                return Created($"/reservation/{createdReservation.Id}", createdReservation);
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
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured when trying to book a reservation:");
                Console.WriteLine(ex.ToString());

                return BadRequest("Invalid reservation");
            }
        }


        [HttpDelete, Produces("application/json"), Route("{reservationId}")]
        public async Task<IActionResult> DeleteReservation(Guid reservationId)
        {
            var result = await _repo.DeleteReservation(reservationId);

            return result ? NoContent() : NotFound();
        }

        [HttpPost, Produces("application/json"), Route("{reservationId}/checkin")]
        public async Task<ActionResult<Reservation>> CheckIn(Guid reservationId, [FromBody] CheckInRequest? request)
        {
            if (request is null || string.IsNullOrEmpty(request.GuestEmail))
            {
                return BadRequest("Guest email is required.");
            }

            try
            {
                var reservation = await _repo.CheckIn(reservationId, request.GuestEmail);
                await _roomRepo.SetRoomState(reservation.RoomNumber, Models.State.Occupied);
                return Json(reservation);
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
