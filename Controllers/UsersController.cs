using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.ViewModels;

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
        int page = 1;
        int pageSize = 10;
        string? search = Request.Query["search"].FirstOrDefault();
        int.TryParse(Request.Query["page"], out page);
        int.TryParse(Request.Query["pageSize"], out pageSize);
        if(page <=0) page = 1;
        if(pageSize <=0) pageSize = 10;

        var paged = await _userService.GetPagedAsync(page,pageSize,search);
        if(!paged.IsSuccess)
        {
            TempData["Error"] = paged.Error;
            return View(new ViewModels.PagedResultViewModel<UserDto>());
        }
        var (users,total,p,ps,s) = paged.Value;
        var vm = new ViewModels.PagedResultViewModel<UserDto>{ Items = users, Page = p, PageSize = ps, Total = total, Search = s };
        return View(vm);
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
        var model = new EditUserViewModel
        {
            Id = user.UserId,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var updateReq = new UpdateUserRequest
        {
            Name = model.Name,
            Role = model.Role,
            Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password
        };

        var result = await _userService.UpdateAsync(id, updateReq);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
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
