using Bus_Booking_Library.Models.Domain;
using Bus_Booking_Library.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Bus_Booking_System.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepo;

        public AdminController(IAdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
        }

        // View all buses
        public IActionResult Index()
        {
            return View();
        }

        // Display the form for adding a new bus
        [HttpGet]
        public async Task<IActionResult> AddBus()
        {
            var buses = await _adminRepo.GetAllBuses(); // Fetch all buses
            var newBus = new Bus(); // Empty bus instance for the form

            var model = Tuple.Create(buses, newBus);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddBus(Bus newBus)
        {
            if (ModelState.IsValid)
            {
                await _adminRepo.InsertBus(newBus.BusID, newBus.BusNumber, newBus.Company, newBus.BusType, newBus.TotalSeats, newBus.AvailableSeats);
                return RedirectToAction("AddBus"); // Refresh page after adding
            }

            var buses = await _adminRepo.GetAllBuses();
            var model = Tuple.Create(buses, newBus);
            return View(model);
        }





        [HttpGet]
        public async Task<IActionResult> BusManagement()
        {
            var buses = await _adminRepo.GetAllBuses();
            return View(buses);
        }


        
        // Display the form for updating a bus
        public IActionResult EditBus(int id)
        {
            return View(new Bus { BusID = id }); // Dummy data for now
        }

        // Handle bus update
        [HttpPost]
        public async Task<IActionResult> EditBus(Bus model)
        {
            if (ModelState.IsValid)
            {
                await _adminRepo.UpdateBus(model.BusID, model.BusNumber, model.BusType, model.TotalSeats, model.AvailableSeats);
                return RedirectToAction("Index");
            }
            return View(model);
        }
    }
}
