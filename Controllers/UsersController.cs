using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Controllers;

[Authorize(Policy = PolicyNames.RequireAdmin)]
public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    public async Task<IActionResult> Index()
    {
        var result = await _userService.GetAllAsync();
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(Enumerable.Empty<UserDto>());
        }
        return View(result.Value);
    }

    public IActionResult Create() => View(new CreateUserRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var result = await _userService.CreateUserAsync(request);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(request);
        }

        TempData["Success"] = "User created successfully";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _userService.GetAsync(id);
        if (!result.IsSuccess)
            return NotFound();

        var user = result.Value!;
        var model = new UpdateUserRequest
        {
            Name = user.Name,
            Role = user.Role
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var result = await _userService.UpdateAsync(id, request);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(request);
        }

        TempData["Success"] = "User updated successfully";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.GetAsync(id);
        if (!result.IsSuccess)
            return NotFound();

        return View(result.Value);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "User deleted successfully";
        return RedirectToAction(nameof(Index));
    }
}
