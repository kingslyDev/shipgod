using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Controllers;

public class AuthController : Controller
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var model = new LoginRequest { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var result = await _userService.AuthenticateAsync(request.Username, request.Password);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(request);
        }

        var user = result.Value!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, AuthConstants.CookieScheme);
        await HttpContext.SignInAsync(AuthConstants.CookieScheme, new ClaimsPrincipal(identity));

        TempData["Success"] = "Anda telah berhasil login";

        if (!string.IsNullOrEmpty(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
            return Redirect(request.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthConstants.CookieScheme);
        TempData["Success"] = "Anda telah berhasil logout";
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Denied() => View();
}
