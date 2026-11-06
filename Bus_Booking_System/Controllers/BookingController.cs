using System.Security.Claims;
using Bus_Booking_System.Models;
using Bus_Booking_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Bus_Booking_System.Controllers;

[Authorize]
public class BookingController(TravelStore store) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [AllowAnonymous]
    public async Task<IActionResult> Search(SearchInput input)
    {
        if (!await store.Ready()) return View("Setup");
        input.Cities = await store.Cities();
        if (ModelState.IsValid) input.Results = await store.Search(input);
        return View(input);
    }
    public async Task<IActionResult> Checkout(int id, int passengers = 1)
    {
        if (passengers is < 1 or > 6) return BadRequest();
        var trip = await store.Trip(id);
        if (trip == null || trip.Departure <= TravelTime.Now) return NotFound();
        return View(new CheckoutInput { TripId = id, Passengers = passengers, Trip = trip,
            RequestId = Guid.NewGuid(), TakenSeats = await store.Taken(id),
            Travellers = Enumerable.Range(0, passengers).Select(_ => new PassengerInput()).ToList() });
    }
    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutInput input)
    {
        if (input.Passengers is < 1 or > 6) return BadRequest();
        if (input.Travellers.Count != input.Passengers) ModelState.AddModelError("", "Enter details for every passenger.");
        if (ModelState.IsValid)
        {
            try { return RedirectToAction(nameof(Ticket), new { reference = await store.Book(UserId, input) }); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            catch (SqlException ex) when (ex.Number is 2601 or 2627 or 1205) { ModelState.AddModelError("", "Availability changed while booking. Please check your seats and try again."); }
        }
        input.Trip = await store.Trip(input.TripId);
        if (input.Trip == null) return NotFound();
        input.TakenSeats = await store.Taken(input.TripId);
        input.Travellers = input.Travellers.Take(input.Passengers).ToList();
        while (input.Travellers.Count < input.Passengers) input.Travellers.Add(new PassengerInput());
        return View(input);
    }
    public async Task<IActionResult> History() => View(await store.Reservations(UserId));
    public async Task<IActionResult> Ticket(Guid reference)
    {
        var reservation = await store.Reservation(reference, UserId);
        return reservation == null ? NotFound() : View(reservation);
    }
    [HttpPost]
    public async Task<IActionResult> Cancel(Guid reference)
    {
        TempData["Message"] = await store.Cancel(reference, UserId) ? "Demo booking cancelled. Your seats have been released." : "This booking cannot be cancelled. Only upcoming confirmed bookings can be cancelled.";
        return RedirectToAction(nameof(History));
    }
}
