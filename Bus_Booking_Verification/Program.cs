using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Bus_Booking_System.Models;
using Bus_Booking_System.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// Integration verification creates and drops ONLY a unique, disposable database.
// Run from the repository root. Never uses the configured application's database.
var root = Directory.GetCurrentDirectory();
var database = "BusBooking_Verification_" + Guid.NewGuid().ToString("N");
var connection = new SqlConnectionStringBuilder {
    DataSource = Environment.GetEnvironmentVariable("BOOKING_TEST_SERVER") ?? @".\SQLEXPRESS",
    IntegratedSecurity = true, TrustServerCertificate = true, InitialCatalog = "master", ConnectTimeout = 5
};
using var master = new SqlConnection(connection.ConnectionString);
await master.OpenAsync();
Process? server = null;
var log = new System.Text.StringBuilder();
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    checks++;
    Console.WriteLine("PASS: " + name);
}
try
{
    var sql = File.ReadAllText(Path.Combine(root, "Database", "Setup.sql")).Replace("BusBooking", database);
    foreach (var batch in Regex.Split(sql, @"^GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        if (!string.IsNullOrWhiteSpace(batch)) await master.ExecuteAsync(batch);
    // Confirm repeatability of schema and sample data.
    foreach (var batch in Regex.Split(sql, @"^GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        if (!string.IsNullOrWhiteSpace(batch)) await master.ExecuteAsync(batch);
    connection.InitialCatalog = database;
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:conn"] = connection.ConnectionString }).Build();
    var store = new TravelStore(config);
    Check(await store.Ready(), "Setup script runs twice without data loss");
    Check((await store.Schedules()).Count == 120, "Sample departures are not duplicated");
    var results = await store.Search(new SearchInput());
    Check(results.Count == 2, "Search returns the two Johannesburg–Durban services");
    var trip = results.First();

    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(root,"Bus_Booking_System"), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
    start.ArgumentList.Add(Path.Combine(root,"Bus_Booking_System","bin","Debug","net8.0","Bus_Booking_System.dll"));
    start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    start.Environment["ConnectionStrings__conn"] = connection.ConnectionString;
    server = Process.Start(start)!;
    server.OutputDataReceived += (_,e) => { lock(log) log.AppendLine(e.Data); };
    server.ErrorDataReceived += (_,e) => { lock(log) log.AppendLine(e.Data); };
    server.BeginOutputReadLine(); server.BeginErrorReadLine();
    HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
    using var client = Client();
    for (var attempt = 0; attempt < 60; attempt++)
    {
        try { using var probe = await client.GetAsync("/"); if (probe.IsSuccessStatusCode) break; }
        catch (HttpRequestException) { }
        if (attempt == 59) throw new Exception("Server failed to start: " + log);
        await Task.Delay(250);
    }
    async Task<string> Page(HttpClient c, string path)
    {
        var response = await c.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception($"GET {path}: {response.StatusCode}\n{html}");
        return html;
    }
    string Field(string html, string name)
    {
        var match = Regex.Match(html, "<input[^>]*name=\"" + Regex.Escape(name) + "\"[^>]*value=\"([^\"]*)\"");
        if (!match.Success) throw new Exception("Missing field " + name);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    async Task<HttpResponseMessage> Post(HttpClient c, string path, string formPage, params (string Key,string Value)[] fields)
    {
        var values = fields.Select(f => new KeyValuePair<string,string>(f.Key,f.Value)).ToList();
        values.Add(new("__RequestVerificationToken",Field(formPage,"__RequestVerificationToken")));
        return await c.PostAsync(path,new FormUrlEncodedContent(values));
    }
    const string email = "verification@example.test";
    const string password = "Verification-Password-2026!";
    var register = await Page(client,"/Account/Register");
    var response = await Post(client,"/Account/Register",register,("Name","Test Traveller"),("Email",email),("Phone","0821234567"),("Password",password),("ConfirmPassword",password));
    Check(response.StatusCode == HttpStatusCode.Redirect,"Registration persists an account");
    var user = (await store.User(email))!;
    Check(user.PasswordHash != password && user.Role == "Customer","Passwords are hashed and registration cannot grant admin");
    var login = await Page(client,"/Account/Login");
    response = await Post(client,"/Account/Login",login,("Email",email),("Password",password));
    Check(response.StatusCode == HttpStatusCode.Redirect,"Login sets an authentication cookie");
    Check((await client.GetAsync("/Admin")).StatusCode == HttpStatusCode.Redirect,"Customers cannot access administration");
    Check((await client.PostAsync("/Booking/Cancel",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode == HttpStatusCode.BadRequest,"Mutation without antiforgery token is rejected");
    var search = await Page(client,$"/Booking/Search?Origin=Johannesburg&Destination=Durban&Date={TravelTime.Now.AddDays(1):yyyy-MM-dd}&Passengers=2");
    Check(search.Contains("Choose seats"),"Search page renders available services");
    var checkout = await Page(client,$"/Booking/Checkout/{trip.Id}?passengers=2");
    var request = Field(checkout,"RequestId");
    var fields = new (string,string)[] { ("TripId",trip.Id.ToString()),("Passengers","2"),("RequestId",request),("Seats","1"),("Seats","2"),("Travellers[0].Name","Test One"),("Travellers[1].Name","Test Two"),("AcceptTerms","true") };
    response = await Post(client,"/Booking/Checkout",checkout,fields);
    if (response.StatusCode != HttpStatusCode.Redirect) throw new Exception("Checkout failed: " + await response.Content.ReadAsStringAsync());
    var ticketUrl = response.Headers.Location!.ToString();
    Check((await Page(client,ticketUrl)).Contains("Test One"),"Checkout creates a printable ticket with passenger names");
    var reservation = (await store.Reservations(user.Id)).Single();
    Check(reservation.Total == trip.Fare*2 && (await store.Trip(trip.Id))!.AvailableSeats == trip.TotalSeats-2,"Server calculates fare and reduces trip capacity");
    response = await Post(client,"/Booking/Checkout",checkout,fields);
    Check(response.Headers.Location?.ToString() == ticketUrl && (await store.Reservations(user.Id)).Count == 1,"Duplicate checkout is idempotent");
    using var stranger = Client();
    Check((await stranger.GetAsync(ticketUrl)).StatusCode == HttpStatusCode.Redirect,"Anonymous visitors cannot read tickets");
    var second = new RegisterInput {Name="Second User",Email="other@example.test",Phone="0821234567",Password=password};
    await store.Register(second,new Microsoft.AspNetCore.Identity.PasswordHasher<RegisterInput>().HashPassword(second,password));
    var other = (await store.User(second.Email))!;
    login = await Page(stranger,"/Account/Login");
    await Post(stranger,"/Account/Login",login,("Email",second.Email),("Password",password));
    Check((await stranger.GetAsync(ticketUrl)).StatusCode == HttpStatusCode.NotFound,"Another signed-in user cannot read your ticket");
    Check(!await store.Cancel(reservation.Reference,other.Id),"Another user cannot cancel your booking");
    async Task<bool> TryBook(int seat)
    {
        try { await store.Book(user.Id,new CheckoutInput {TripId=trip.Id,Passengers=1,Seats=[seat],Travellers=[new() {Name="Concurrent Traveller"}],RequestId=Guid.NewGuid(),AcceptTerms=true}); return true; }
        catch (InvalidOperationException) { return false; }
        catch (SqlException ex) when(ex.Number is 1205 or 2601 or 2627) { return false; }
    }
    var attempts = await Task.WhenAll(TryBook(3),TryBook(3));
    Check(attempts.Count(x=>x)==1,"Concurrent bookings cannot reserve the same seat twice");
    Check(!await TryBook(99),"Out-of-range seats are rejected");
    var history = await Page(client,"/Booking/History");
    response = await Post(client,"/Booking/Cancel",history,("reference",reservation.Reference.ToString()));
    Check(response.StatusCode == HttpStatusCode.Redirect && !(await store.Taken(trip.Id)).Contains(1),"Cancellation releases booked seats");
    Check((await store.Reservation(reservation.Reference,user.Id))!.Status == "Cancelled","Cancelled ticket retains its history");
    using var db = new SqlConnection(connection.ConnectionString);
    await db.ExecuteAsync("UPDATE travel.Users SET Role='Admin' WHERE Id=@Id",new {user.Id});
    login = await Page(client,"/Account/Login");
    await Post(client,"/Account/Login",login,("Email",email),("Password",password));
    Check((await Page(client,"/Admin")).Contains("All bookings"),"Promoted admin can view all bookings");
    Check((await Page(client,"/Admin/BusManagement")).Contains("DEMO-GP-01"),"Fleet administration renders persisted buses");
    var addBus = await Page(client,"/Admin/AddBus");
    response = await Post(client,"/Admin/EditBus",addBus,("Id","0"),("BusNumber","VERIFY-01"),("Company","Verification"),("BusType","Luxury"),("TotalSeats","40"));
    Check(response.StatusCode == HttpStatusCode.Redirect,"Admin creates a bus through the form");
    var bus = (await store.Fleet()).Single(x=>x.BusNumber=="VERIFY-01");
    var schedule = new ScheduleInput {BusId=bus.Id,Origin="Pretoria",Destination="Polokwane",BoardingPoint="Station A",ArrivalPoint="Station B",Departure=TravelTime.Now.AddDays(2),Arrival=TravelTime.Now.AddDays(2).AddHours(4),Fare=200};
    await store.AddSchedule(schedule);
    var overlapBlocked = false;
    try { await store.AddSchedule(schedule); } catch (InvalidOperationException) { overlapBlocked=true; }
    Check(overlapBlocked,"Overlapping schedules for the same bus are rejected");
    Check(!await store.DeleteBus(bus.Id),"Buses with schedules are protected from deletion");
    Check((await Page(client,"/Admin/Schedules")).Contains("Polokwane"),"Schedule administration shows new routes");
    Console.WriteLine($"Completed {checks} integration checks.");
}
finally
{
    if (server is { HasExited:false }) { server.Kill(entireProcessTree:true); await server.WaitForExitAsync(); }
    server?.Dispose();
    SqlConnection.ClearAllPools();
    // The identifier is generated above, never supplied by the caller.
    await master.ChangeDatabaseAsync("master");
    await master.ExecuteAsync($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];");
    Console.WriteLine("Removed disposable verification database.");
}
