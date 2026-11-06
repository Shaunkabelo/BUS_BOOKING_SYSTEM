using Microsoft.AspNetCore.Mvc;

namespace Bus_Booking_System.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Login()
        {
            return RedirectToAction("Login", "Account");
        }
    }
}
