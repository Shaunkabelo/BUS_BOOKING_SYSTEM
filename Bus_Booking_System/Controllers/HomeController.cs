using Bus_Booking_System.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bus_Booking_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index([FromServices] Bus_Booking_System.Services.TravelStore store)
        {
            if (!await store.Ready()) return View("Setup");
            return View(new SearchInput { Cities = await store.Cities() });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
