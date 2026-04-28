using Dapper;
using Microsoft.Data.Sqlite;
using Models;
using NUnit.Framework;
using Repositories;

namespace api.Tests
{
    [TestFixture]
    public class ReservationRepositoryTests
    {
        private SqliteConnection _db = null!;
        private ReservationRepository _repo = null!;

        [SetUp]
        public async Task SetUp()
        {
            _db = new SqliteConnection("Data Source=:memory:");
            await _db.OpenAsync();

            await _db.ExecuteAsync(@"
                CREATE TABLE Guests (
                    Email TEXT PRIMARY KEY NOT NULL,
                    Name  TEXT NOT NULL,
                    Surname TEXT
                );
                CREATE TABLE Rooms (
                    Number INT PRIMARY KEY NOT NULL,
                    State  INT NOT NULL
                );
                CREATE TABLE Reservations (
                    Id          TEXT PRIMARY KEY NOT NULL,
                    GuestEmail  TEXT NOT NULL,
                    RoomNumber  INT  NOT NULL,
                    Start       TEXT NOT NULL,
                    End         TEXT NOT NULL,
                    CheckedIn   INT  NOT NULL DEFAULT 0,
                    CheckedOut  INT  NOT NULL DEFAULT 0
                );
                INSERT INTO Guests (Email, Name) VALUES ('guest@example.com', 'Test Guest');
                INSERT INTO Rooms  (Number, State) VALUES (101, 0);
                INSERT INTO Rooms  (Number, State) VALUES (202, 0);
            ");

            _repo = new ReservationRepository(_db);
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        private async Task<Reservation> Insert(string room, string start, string end,
            bool checkedIn = false, bool checkedOut = false)
        {
            var id = Guid.NewGuid();
            await _db.ExecuteAsync(
                @"INSERT INTO Reservations (Id, GuestEmail, RoomNumber, Start, End, CheckedIn, CheckedOut)
                  VALUES (@Id, 'guest@example.com', @RoomNumber, @Start, @End, @CheckedIn, @CheckedOut);",
                new
                {
                    Id = id.ToString(),
                    RoomNumber = int.Parse(room),
                    Start = start,
                    End = end,
                    CheckedIn = checkedIn ? 1 : 0,
                    CheckedOut = checkedOut ? 1 : 0
                });
            return await _repo.GetReservation(id);
        }

        // ── GetReservations ──────────────────────────────────────────────────────

        [Test]
        public async Task GetReservations_EmptyTable_ReturnsEmpty()
        {
            var result = await _repo.GetReservations();
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetReservations_MultipleReservations_ReturnsAll()
        {
            await Insert("101", "2025-06-01", "2025-06-05");
            await Insert("202", "2025-07-01", "2025-07-05");

            var result = await _repo.GetReservations();

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        // ── GetUpcomingReservations ──────────────────────────────────────────────

        [Test]
        public async Task GetUpcomingReservations_PastReservation_IsExcluded()
        {
            // A reservation that ended well in the past
            await Insert("101", "2000-01-01", "2000-01-05");

            var result = await _repo.GetUpcomingReservations();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetUpcomingReservations_FutureReservation_IsIncluded()
        {
            // A reservation ending far in the future
            await Insert("101", "2099-01-01", "2099-01-10");

            var result = await _repo.GetUpcomingReservations();

            Assert.That(result.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetUpcomingReservations_MixedReservations_ReturnsOnlyFuture()
        {
            await Insert("101", "2000-01-01", "2000-01-05"); // past
            await Insert("202", "2099-06-01", "2099-06-10"); // future

            var result = await _repo.GetUpcomingReservations();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().RoomNumber, Is.EqualTo("202"));
        }

        [Test]
        public async Task GetUpcomingReservations_ReturnsOrderedByStart()
        {
            await Insert("202", "2099-07-01", "2099-07-10");
            await Insert("101", "2099-06-01", "2099-06-10");

            var result = (await _repo.GetUpcomingReservations()).ToList();

            Assert.That(result[0].RoomNumber, Is.EqualTo("101"));
            Assert.That(result[1].RoomNumber, Is.EqualTo("202"));
        }

        // ── DeleteReservation ────────────────────────────────────────────────────

        [Test]
        public async Task DeleteReservation_ExistingReservation_ReturnsTrue()
        {
            var reservation = await Insert("101", "2025-06-01", "2025-06-05");

            var result = await _repo.DeleteReservation(reservation.Id);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task DeleteReservation_ExistingReservation_IsActuallyDeleted()
        {
            var reservation = await Insert("101", "2025-06-01", "2025-06-05");
            await _repo.DeleteReservation(reservation.Id);

            var all = await _repo.GetReservations();
            Assert.That(all, Is.Empty);
        }

        [Test]
        public async Task DeleteReservation_UnknownId_ReturnsFalse()
        {
            var result = await _repo.DeleteReservation(Guid.NewGuid());

            Assert.That(result, Is.False);
        }
    }
}
