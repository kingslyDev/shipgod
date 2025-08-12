using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using System.Threading.Tasks;

namespace ShipmentFinishGood.Controllers
{
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public class ModelConfigurationsController : Controller
    {
        private readonly IModelConfigurationService _service;
        public ModelConfigurationsController(IModelConfigurationService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var configs = await _service.GetAllAsync();
            return View(configs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var config = await _service.GetByIdAsync(id);
            if (config == null) return NotFound();
            return View(config);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ModelConfiguration model)
        {
            if (ModelState.IsValid)
            {
                await _service.AddAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var config = await _service.GetByIdAsync(id);
            if (config == null) return NotFound();
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ModelConfiguration model)
        {
            if (id != model.ConfigId) return BadRequest();
            if (ModelState.IsValid)
            {
                await _service.UpdateAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var config = await _service.GetByIdAsync(id);
            if (config == null) return NotFound();
            return View(config);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
