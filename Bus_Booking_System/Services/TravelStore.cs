using System.Data;
using Bus_Booking_System.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bus_Booking_System.Services;

public class TravelStore(IConfiguration configuration)
{
    private SqlConnection Connection() => new(configuration.GetConnectionString("conn"));
    private const string TripSql = """
        SELECT t.*, b.Company, b.BusType, b.TotalSeats,
          b.TotalSeats - (SELECT COUNT(*) FROM travel.Seats s WHERE s.TripId=t.Id AND s.IsActive=1) AvailableSeats
        FROM travel.Trips t JOIN travel.Buses b ON b.Id=t.BusId
        """;
    public async Task<bool> Ready()
    {
        try { using var c = Connection(); return await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sys.tables WHERE object_id=OBJECT_ID('travel.Users')") == 1; }
        catch (SqlException) { return false; }
    }
    public async Task<TravelUser?> User(string email)
    {
        using var c = Connection();
        return await c.QuerySingleOrDefaultAsync<TravelUser>("SELECT * FROM travel.Users WHERE Email=@email", new { email = email.Trim().ToLowerInvariant() });
    }
    public async Task Register(RegisterInput input, string hash)
    {
        using var c = Connection();
        await c.ExecuteAsync("INSERT travel.Users(Name,Email,Phone,PasswordHash) VALUES(@Name,@Email,@Phone,@hash)",
            new { Name = input.Name.Trim(), Email = input.Email.Trim().ToLowerInvariant(), Phone = input.Phone.Trim(), hash });
    }
    public async Task<List<string>> Cities()
    {
        using var c = Connection();
        return (await c.QueryAsync<string>("SELECT Origin FROM travel.Trips UNION SELECT Destination FROM travel.Trips ORDER BY Origin")).ToList();
    }
    public async Task<List<Trip>> Search(SearchInput input)
    {
        using var c = Connection();
        var trips = await c.QueryAsync<Trip>(TripSql + " WHERE t.Origin=@Origin AND t.Destination=@Destination AND t.Departure>=@start AND t.Departure<@end AND t.Departure>@now ORDER BY t.Departure,t.Fare",
            new { input.Origin, input.Destination, start = input.Date.Date, end = input.Date.Date.AddDays(1), now = TravelTime.Now });
        return trips.Where(t => t.AvailableSeats >= input.Passengers).ToList();
    }
    public async Task<Trip?> Trip(int id)
    {
        using var c = Connection();
        return await c.QuerySingleOrDefaultAsync<Trip>(TripSql + " WHERE t.Id=@id", new { id });
    }
    public async Task<List<int>> Taken(int id)
    {
        using var c = Connection();
        return (await c.QueryAsync<int>("SELECT SeatNumber FROM travel.Seats WHERE TripId=@id AND IsActive=1", new { id })).ToList();
    }
    public async Task<Guid> Book(int userId, CheckoutInput input)
    {
        using var c = Connection();
        await c.OpenAsync();
        using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        // Serialize repeated submissions by request id before looking up the previous result.
        var lockResult = await c.ExecuteScalarAsync<int>("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;",
            new { key = "booking:" + input.RequestId }, tx);
        if (lockResult < 0) throw new InvalidOperationException("Booking is busy. Please try again.");
        var previous = await c.QuerySingleOrDefaultAsync<Reservation>("SELECT * FROM travel.Reservations WHERE RequestId=@RequestId", new { input.RequestId }, tx);
        if (previous != null)
        {
            if (previous.UserId != userId) throw new InvalidOperationException("Please restart your booking.");
            tx.Commit();
            return previous.Reference;
        }
        // Lock the bus and trip while checking capacity; database unique index is the final seat guard.
        var trip = await c.QuerySingleOrDefaultAsync<Trip>("""
            SELECT t.*, b.TotalSeats FROM travel.Trips t WITH (UPDLOCK,HOLDLOCK)
            JOIN travel.Buses b WITH (UPDLOCK,HOLDLOCK) ON b.Id=t.BusId WHERE t.Id=@TripId
            """, new { input.TripId }, tx);
        if (trip == null || trip.Departure <= TravelTime.Now) throw new InvalidOperationException("This trip is no longer available.");
        if (input.RequestId == Guid.Empty || input.Passengers is < 1 or > 6 || input.Seats.Length != input.Passengers ||
            input.Seats.Distinct().Count() != input.Passengers || input.Seats.Any(s => s < 1 || s > trip.TotalSeats) ||
            input.Travellers.Count != input.Passengers || input.Travellers.Any(p => string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 100) || !input.AcceptTerms)
            throw new InvalidOperationException("Select one different seat and enter a name for each passenger, then accept the terms.");
        if (await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM travel.Seats WHERE TripId=@TripId AND IsActive=1 AND SeatNumber IN @Seats", new { input.TripId, input.Seats }, tx) > 0)
            throw new InvalidOperationException("One of those seats has just been booked. Please choose another seat.");
        var reference = Guid.NewGuid();
        var id = await c.ExecuteScalarAsync<int>("""
            INSERT travel.Reservations(Reference,RequestId,UserId,TripId,Total,Status)
            OUTPUT INSERTED.Id VALUES(@reference,@RequestId,@userId,@TripId,@total,'DemoConfirmed')
            """, new { reference, input.RequestId, userId, input.TripId, total = trip.Fare * input.Passengers }, tx);
        Array.Sort(input.Seats);
        for (var i = 0; i < input.Seats.Length; i++)
            await c.ExecuteAsync("INSERT travel.Seats(ReservationId,TripId,SeatNumber,PassengerName) VALUES(@id,@TripId,@seat,@name)",
                new { id, input.TripId, seat = input.Seats[i], name = input.Travellers[i].Name.Trim() }, tx);
        tx.Commit();
        return reference;
    }
    private const string ReservationSql = """
        SELECT r.*,u.Name CustomerName,u.Email,t.Origin,t.Destination,t.Departure
        FROM travel.Reservations r JOIN travel.Users u ON u.Id=r.UserId JOIN travel.Trips t ON t.Id=r.TripId
        """;
    public async Task<List<Reservation>> Reservations(int? userId)
    {
        using var c = Connection();
        return (await c.QueryAsync<Reservation>(ReservationSql + " WHERE (@userId IS NULL OR r.UserId=@userId) ORDER BY r.CreatedUtc DESC", new { userId })).ToList();
    }
    public async Task<Reservation?> Reservation(Guid reference, int userId)
    {
        using var c = Connection();
        var r = await c.QuerySingleOrDefaultAsync<Reservation>(ReservationSql + " WHERE r.Reference=@reference AND r.UserId=@userId", new { reference, userId });
        if (r == null) return null;
        r.Trip = await Trip(r.TripId);
        r.Seats = (await c.QueryAsync<ReservedSeat>("SELECT SeatNumber,PassengerName FROM travel.Seats WHERE ReservationId=@Id ORDER BY SeatNumber", new { r.Id })).ToList();
        return r;
    }
    public async Task<bool> Cancel(Guid reference, int userId)
    {
        using var c = Connection();
        await c.OpenAsync();
        using var tx = c.BeginTransaction();
        var id = await c.QuerySingleOrDefaultAsync<int?>("""
            UPDATE r SET Status='Cancelled' OUTPUT INSERTED.Id
            FROM travel.Reservations r JOIN travel.Trips t ON t.Id=r.TripId
            WHERE r.Reference=@reference AND r.UserId=@userId AND r.Status='DemoConfirmed' AND t.Departure>@now
            """, new { reference, userId, now = TravelTime.Now }, tx);
        if (id == null) return false;
        await c.ExecuteAsync("UPDATE travel.Seats SET IsActive=0 WHERE ReservationId=@id", new { id }, tx);
        tx.Commit();
        return true;
    }
    public async Task<List<FleetInput>> Fleet()
    {
        using var c = Connection();
        return (await c.QueryAsync<FleetInput>("SELECT * FROM travel.Buses ORDER BY Company,BusNumber")).ToList();
    }
    public async Task SaveBus(FleetInput input)
    {
        using var c = Connection();
        await c.OpenAsync();
        using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (input.Id == 0)
            await c.ExecuteAsync("INSERT travel.Buses(BusNumber,Company,BusType,TotalSeats) VALUES(@BusNumber,@Company,@BusType,@TotalSeats)", input, tx);
        else
        {
            var existing = await c.QuerySingleOrDefaultAsync<FleetInput>("SELECT * FROM travel.Buses WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id", input, tx);
            if (existing == null) throw new InvalidOperationException("Bus no longer exists.");
            if (existing.TotalSeats != input.TotalSeats && await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM travel.Trips WHERE BusId=@Id", input, tx) > 0)
                throw new InvalidOperationException("Seat capacity cannot change once this bus has schedules. Add a new bus for a different seat layout.");
            await c.ExecuteAsync("UPDATE travel.Buses SET BusNumber=@BusNumber,Company=@Company,BusType=@BusType,TotalSeats=@TotalSeats WHERE Id=@Id", input, tx);
        }
        tx.Commit();
    }
    public async Task<bool> DeleteBus(int id)
    {
        using var c = Connection();
        return await c.ExecuteAsync("DELETE FROM travel.Buses WHERE Id=@id AND NOT EXISTS(SELECT 1 FROM travel.Trips WHERE BusId=@id)", new { id }) > 0;
    }
    public async Task<List<Trip>> Schedules()
    {
        using var c = Connection();
        return (await c.QueryAsync<Trip>(TripSql + " WHERE t.Departure>@now ORDER BY t.Departure", new { now = TravelTime.Now })).ToList();
    }
    public async Task AddSchedule(ScheduleInput input)
    {
        using var c = Connection();
        await c.OpenAsync();
        using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (await c.ExecuteScalarAsync<int?>("SELECT Id FROM travel.Buses WITH(UPDLOCK,HOLDLOCK) WHERE Id=@BusId", input, tx) == null)
            throw new InvalidOperationException("Choose an existing bus.");
        if (await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM travel.Trips WHERE BusId=@BusId AND Departure<@Arrival AND Arrival>@Departure", input, tx) > 0)
            throw new InvalidOperationException("This bus already has a trip during those times.");
        await c.ExecuteAsync("INSERT travel.Trips(BusId,Origin,Destination,BoardingPoint,ArrivalPoint,Departure,Arrival,Fare) VALUES(@BusId,@Origin,@Destination,@BoardingPoint,@ArrivalPoint,@Departure,@Arrival,@Fare)", input, tx);
        tx.Commit();
    }
    public async Task<bool> DeleteSchedule(int id)
    {
        using var c = Connection();
        return await c.ExecuteAsync("DELETE FROM travel.Trips WHERE Id=@id AND NOT EXISTS(SELECT 1 FROM travel.Reservations WHERE TripId=@id)", new { id }) > 0;
    }
}
