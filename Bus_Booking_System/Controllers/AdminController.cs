using Bus_Booking_System.Models;
using Bus_Booking_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Bus_Booking_System.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(TravelStore store) : Controller
{
    public async Task<IActionResult> Index() => View(await store.Reservations(null));
    public async Task<IActionResult> BusManagement() => View(await store.Fleet());
    public IActionResult AddBus() => View("EditBus", new FleetInput());
    public async Task<IActionResult> EditBus(int id)
    {
        var bus = (await store.Fleet()).SingleOrDefault(b => b.Id == id);
        return bus == null ? NotFound() : View(bus);
    }
    [HttpPost]
    public async Task<IActionResult> EditBus(FleetInput input)
    {
        if (ModelState.IsValid)
        {
            try { await store.SaveBus(input); TempData["Message"] = "Bus saved."; return RedirectToAction(nameof(BusManagement)); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            catch (SqlException ex) when (ex.Number is 2601 or 2627) { ModelState.AddModelError("", "That bus number is already registered."); }
        }
        return View(input);
    }
    [HttpPost]
    public async Task<IActionResult> DeleteBus(int id)
    {
        try { TempData["Message"] = await store.DeleteBus(id) ? "Bus deleted." : "A bus with schedules cannot be deleted."; }
        catch (SqlException ex) when (ex.Number == 547) { TempData["Message"] = "A bus with schedules cannot be deleted."; }
        return RedirectToAction(nameof(BusManagement));
    }
    public async Task<IActionResult> Schedules() => View(await store.Schedules());
    public async Task<IActionResult> AddSchedule() => View(new ScheduleInput { Buses = await store.Fleet() });
    [HttpPost]
    public async Task<IActionResult> AddSchedule(ScheduleInput input)
    {
        if (ModelState.IsValid)
        {
            try { await store.AddSchedule(input); TempData["Message"] = "Departure added."; return RedirectToAction(nameof(Schedules)); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        input.Buses = await store.Fleet();
        return View(input);
    }
    [HttpPost]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        try { TempData["Message"] = await store.DeleteSchedule(id) ? "Departure deleted." : "Departures with booking history cannot be deleted."; }
        catch (SqlException ex) when (ex.Number == 547) { TempData["Message"] = "Departures with booking history cannot be deleted."; }
        return RedirectToAction(nameof(Schedules));
    }
}
