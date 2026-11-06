using System.Security.Claims;
using Bus_Booking_System.Models;
using Bus_Booking_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;

namespace Bus_Booking_System.Controllers;

[EnableRateLimiting("account")]
public class AccountController(TravelStore store) : Controller
{
    public IActionResult Register() => View(new RegisterInput());
    [HttpPost]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!await store.Ready()) return View("Setup");
        if (ModelState.IsValid)
        {
            try
            {
                var hash = new PasswordHasher<RegisterInput>().HashPassword(input, input.Password);
                await store.Register(input, hash);
                TempData["Message"] = "Account created. Sign in to book your first trip.";
                return RedirectToAction(nameof(Login));
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627) { ModelState.AddModelError("", "An account with that email already exists."); }
        }
        return View(input);
    }
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginInput());
    }
    [HttpPost]
    public async Task<IActionResult> Login(LoginInput input, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!await store.Ready()) return View("Setup");
        if (ModelState.IsValid)
        {
            var user = await store.User(input.Email);
            if (user != null && new PasswordHasher<TravelUser>().VerifyHashedPassword(user, user.PasswordHash, input.Password) != PasswordVerificationResult.Failed)
            {
                var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name), new Claim(ClaimTypes.Role, user.Role) }, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Email or password is incorrect.");
        }
        return View(input);
    }
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }
}
