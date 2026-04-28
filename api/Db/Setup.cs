using Dapper;
using Microsoft.Data.Sqlite;
using Models;

namespace Db
{
    public static class Setup
    {
        /// <summary>
        /// Ensures the DB is available and the required tables are made
        /// </summary>
        public static async Task EnsureDb(IServiceScope scope)
        {
            using var db = scope.ServiceProvider.GetRequiredService<SqliteConnection>();

            // SQLite WAL (write-ahead log) go brrrr
            await db.ExecuteAsync("PRAGMA journal_mode = wal;");
            // SQLite does not enforce FKs by default
            await db.ExecuteAsync("PRAGMA foreign_keys = ON;");

            await db.ExecuteAsync(
                $@"
              CREATE TABLE IF NOT EXISTS Guests (
                {nameof(Guest.Email)} TEXT PRIMARY KEY NOT NULL,
                {nameof(Guest.Name)} TEXT NOT NULL,
                {nameof(Guest.Surname)} TEXT
              );
            "
            );

            var columns = await db.QueryAsync<string>("SELECT name FROM pragma_table_info('Guests');");
            if (!columns.Any(c => c == nameof(Guest.Surname)))
            {
                await db.ExecuteAsync(
                    $"ALTER TABLE Guests ADD COLUMN {nameof(Guest.Surname)} TEXT;"
                );
            }

            await db.ExecuteAsync(
                $@"
              CREATE TABLE IF NOT EXISTS Rooms (
                {nameof(Room.Number)} INT PRIMARY KEY NOT NULL,
                {nameof(Room.State)} INT NOT NULL
              );
            "
            );

            await db.ExecuteAsync(
                $@"
              CREATE TABLE IF NOT EXISTS Reservations (
                {nameof(Reservation.Id)} TEXT PRIMARY KEY NOT NULL,
                {nameof(Reservation.GuestEmail)} TEXT NOT NULL,
                {nameof(Reservation.RoomNumber)} INT NOT NULL,
                {nameof(Reservation.Start)} TEXT NOT NULL,
                {nameof(Reservation.End)} TEXT NOT NULL,
                {nameof(Reservation.CheckedIn)} INT NOT NULL DEFAULT FALSE,
                {nameof(Reservation.CheckedOut)} INT NOT NULL DEFAULT FALSE,
                FOREIGN KEY ({nameof(Reservation.GuestEmail)})
                  REFERENCES Guests ({nameof(Guest.Email)}),
                FOREIGN KEY ({nameof(Reservation.RoomNumber)})
                  REFERENCES Rooms ({nameof(Room.Number)})
              );
            "
            );

            // Migration: if Start/End were created as INT (old schema) migrate them to TEXT.
            // SQLite does not support ALTER COLUMN, so we use the standard rename-copy-drop pattern.
            var reservationColumns = await db.QueryAsync<(string name, string type)>(
                "SELECT name, type FROM pragma_table_info('Reservations');"
            );
            var colMap = reservationColumns.ToDictionary(c => c.name, c => c.type, StringComparer.OrdinalIgnoreCase);
            if (colMap.TryGetValue(nameof(Reservation.Start), out var startType) && startType.Equals("INT", StringComparison.OrdinalIgnoreCase))
            {
                await db.ExecuteAsync("PRAGMA foreign_keys = OFF;");
                await db.ExecuteAsync(
                    $@"
                  CREATE TABLE Reservations_new (
                    {nameof(Reservation.Id)} TEXT PRIMARY KEY NOT NULL,
                    {nameof(Reservation.GuestEmail)} TEXT NOT NULL,
                    {nameof(Reservation.RoomNumber)} INT NOT NULL,
                    {nameof(Reservation.Start)} TEXT NOT NULL,
                    {nameof(Reservation.End)} TEXT NOT NULL,
                    {nameof(Reservation.CheckedIn)} INT NOT NULL DEFAULT FALSE,
                    {nameof(Reservation.CheckedOut)} INT NOT NULL DEFAULT FALSE,
                    FOREIGN KEY ({nameof(Reservation.GuestEmail)})
                      REFERENCES Guests ({nameof(Guest.Email)}),
                    FOREIGN KEY ({nameof(Reservation.RoomNumber)})
                      REFERENCES Rooms ({nameof(Room.Number)})
                  );
                "
                );
                // Cast existing INT epoch values (seconds since Unix epoch) to ISO-8601 TEXT
                // so date comparisons remain correct after migration.
                await db.ExecuteAsync(
                    $@"
                  INSERT INTO Reservations_new
                  SELECT
                    {nameof(Reservation.Id)},
                    {nameof(Reservation.GuestEmail)},
                    {nameof(Reservation.RoomNumber)},
                    datetime({nameof(Reservation.Start)}, 'unixepoch') AS {nameof(Reservation.Start)},
                    datetime({nameof(Reservation.End)},   'unixepoch') AS {nameof(Reservation.End)},
                    {nameof(Reservation.CheckedIn)},
                    {nameof(Reservation.CheckedOut)}
                  FROM Reservations;
                "
                );
                await db.ExecuteAsync("DROP TABLE Reservations;");
                await db.ExecuteAsync("ALTER TABLE Reservations_new RENAME TO Reservations;");
                await db.ExecuteAsync("PRAGMA foreign_keys = ON;");
            }
        }
    }
}
