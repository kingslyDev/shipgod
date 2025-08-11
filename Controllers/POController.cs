using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShipmentFinishGood.Data;
using ShipmentFinishGood.Repositories; // for AppDbContext
using System.Collections.Generic;

namespace ShipmentFinishGood.Controllers;

 [Authorize(Policy = "RequireInputer")]
 public class POController : Controller
 {
     private static readonly string[] AllowedMethods = new[] { "LOOSE", "PALLET" };
     private readonly IGroupingService _groupingService;
     private readonly AppDbContext _ctx;
     private readonly IModelConfigService _modelConfigService;

     public POController(IGroupingService groupingService, AppDbContext ctx, IModelConfigService modelConfigService)
     {
         _groupingService = groupingService;
         _ctx = ctx;
         _modelConfigService = modelConfigService;
     }

     [HttpGet]
     public async Task<IActionResult> Create(int? sessionId = null)
     {
         var vm = new POUploadViewModel { SessionId = sessionId };
         if (sessionId.HasValue)
         {
             // Preload preview + items for initial render (AJAX optional later)
             var preview = await _groupingService.LoadSessionPreviewAsync(sessionId.Value);
             var sessionItems = await _ctx.ExcelUploadItems
                 .Where(i => i.SessionId == sessionId.Value)
                 .OrderBy(i => i.RowNumber)
                 .ToListAsync();
             ViewBag.Preview = preview;
             ViewBag.SessionItems = sessionItems; // anonymous objects list OK for view consumption
         }
         return View(vm);
     }

    [HttpGet]
    public IActionResult Template()
    {
        using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("DEST-A");
    ws.Cell(1,1).Value = "NoPO";
    ws.Cell(1,2).Value = "Model";
    ws.Cell(1,3).Value = "Qty";
    ws.Cell(2,1).Value = "4502195897";
    ws.Cell(2,2).Value = "RF-2400DEG-K";
    ws.Cell(2,3).Value = 3486;
    ws.Cell(3,1).Value = ""; // model tambahan PO sama
    ws.Cell(3,2).Value = "RF-D10EG-K";
    ws.Cell(3,3).Value = 108;
    var ws2 = wb.Worksheets.Add("DEST-B");
    ws2.Cell(1,1).Value = "NoPO";
    ws2.Cell(1,2).Value = "Model";
    ws2.Cell(1,3).Value = "Qty";
    ws2.Cell(2,1).Value = "4502195898";
    ws2.Cell(2,2).Value = "RF-2400DEG-K";
    ws2.Cell(2,3).Value = 561;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(ms.ToArray(), contentType, "TemplatePO.xlsx");
    }

     [HttpPost]
     [ValidateAntiForgeryToken]
     public async Task<IActionResult> Create(POUploadViewModel vm, string? submit)
     {
         // Only supported submit now: preview (upload new file)
         if (submit != "preview")
         {
             return RedirectToAction(nameof(Create), new { sessionId = vm.SessionId });
         }

         if (vm.File == null || vm.File.Length == 0)
         {
             ModelState.AddModelError("File", "File wajib diupload.");
             return View(vm);
         }

         using var ms = new MemoryStream();
         await vm.File.CopyToAsync(ms);
         ms.Position = 0;
         var sessionId = await _groupingService.ParseAndStoreAsync(ms, vm.File.FileName, User.Identity?.Name ?? "system");
         return RedirectToAction(nameof(Create), new { sessionId });
     }

     // Provide partial refresh endpoint (AJAX optional)
     [HttpGet]
     public async Task<IActionResult> PreviewPartial(int sessionId)
     {
         var preview = await _groupingService.LoadSessionPreviewAsync(sessionId);
         var sessionItems = await _ctx.ExcelUploadItems
             .Where(i => i.SessionId == sessionId)
             .OrderBy(i => i.RowNumber)
             .ToListAsync();
         return PartialView("_PreviewTable", (preview, sessionItems));
     }

    [HttpGet]
    public async Task<IActionResult> Queue(string? status, string? search, int? sessionId, int page = 1, int pageSize = 50)
    {
        var q = _ctx.ExcelUploadItems
            .Select(i => new {
                i.ItemId, i.RowNumber, i.NoPO, i.ModelProduk, i.Destination, i.QtyTotal, i.IsProcessed, i.SessionId,
                i.Session.FileName, i.Session.CreatedDate, i.MethodPlanned, i.SheetName
            });
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("pending", StringComparison.OrdinalIgnoreCase)) q = q.Where(x => !x.IsProcessed);
            else if (status.Equals("processed", StringComparison.OrdinalIgnoreCase)) q = q.Where(x => x.IsProcessed);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            q = q.Where(x => x.NoPO.Contains(search) || x.ModelProduk.Contains(search) || x.Destination.Contains(search));
        }
        if (sessionId.HasValue)
        {
            q = q.Where(x => x.SessionId == sessionId.Value);
        }
        int total = await q.CountAsync();
        var items = await q
            .OrderByDescending(x => x.CreatedDate)
            .ThenBy(x => x.RowNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vmQueue = new POQueueViewModel
        {
            Items = items.Select(x => new POQueueItemViewModel
            {
                ItemId = x.ItemId,
                RowNumber = x.RowNumber,
                NoPO = x.NoPO,
                ModelCode = x.ModelProduk,
                Destination = x.Destination,
                Quantity = x.QtyTotal,
                IsProcessed = x.IsProcessed,
                MethodPlanned = x.MethodPlanned,
                SessionId = x.SessionId,
                FileName = x.FileName,
                UploadedAt = x.CreatedDate,
                SheetName = x.SheetName
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            StatusFilter = status,
            Search = search,
            SessionFilter = sessionId
        };
        return View(vmQueue);
    }

     [HttpPost]
     [ValidateAntiForgeryToken]
     public async Task<IActionResult> SetRowMethod(int itemId, string method)
     {
         if (itemId <= 0) return JsonFail("ItemId tidak valid.");

         method = method?.Trim().ToUpperInvariant() ?? string.Empty;
         if (!AllowedMethods.Contains(method)) return JsonFail("Method harus LOOSE atau PALLET.");

         var item = await _ctx.ExcelUploadItems.FindAsync(itemId);
         if (item == null) return JsonFail("Item tidak ditemukan.");
         if (item.IsProcessed) return JsonFail("Item sudah diproses sebelumnya.");

         var supported = await _modelConfigService.ValidateModelSupportAsync(item.ModelProduk, method);
         if (!supported) return JsonFail($"Model {item.ModelProduk} tidak mendukung method {method}.");

         item.MethodPlanned = method;
         await _ctx.SaveChangesAsync();
         return JsonOk(new { method }, $"Method {method} diset.");
     }

     [HttpPost]
     [ValidateAntiForgeryToken]
     public async Task<IActionResult> CommitSingleRow(int itemId)
     {
         var item = await _ctx.ExcelUploadItems.FindAsync(itemId);
         if (item == null) return JsonFail("Item tidak ditemukan.");
         if (item.IsProcessed) return JsonFail("Item sudah diproses sebelumnya.");
         if (string.IsNullOrEmpty(item.MethodPlanned)) return JsonFail("Method belum dipilih untuk item ini.");

         try
         {
             var preview = new POUploadPreviewViewModel
             {
                 FileName = item.Session?.FileName ?? "Single Row", // ensure consistent signature grouping
                 RawRows = new List<RawRow>
                 {
                     new RawRow
                     {
                         RowId = item.RowNumber,
                         SheetName = item.SheetName,
                         ModelCode = item.ModelProduk,
                         NoPO = item.NoPO,
                         Destination = item.Destination,
                         Quantity = item.QtyTotal,
                         Processed = true
                     }
                 }
             };

             await _groupingService.ApplyMethodAndGroupAsync(preview, item.MethodPlanned);
             var committed = await _groupingService.CommitAsync(preview, User.Identity?.Name ?? "system");
             if (!committed) return JsonFail("Gagal memproses item ke database.");

             item.IsProcessed = true;
             await _ctx.SaveChangesAsync();
             return JsonOk(null, "Item berhasil diproses ke database.");
         }
         catch (System.Exception ex)
         {
             return JsonFail($"Error: {ex.Message}");
         }
     }

    // (Removed duplicated legacy StoreAllToDatabase with Guid and corrupted trailing text)

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StoreAllToDatabase(int sessionId)
     {
         try
         {
             var items = await _ctx.ExcelUploadItems
                 .Where(i => i.SessionId == sessionId && !i.IsProcessed && i.MethodPlanned != null)
                 .OrderBy(i => i.RowNumber)
                 .ToListAsync();
             if (!items.Any()) return JsonFail("Tidak ada item yang siap untuk disimpan ke database.");

             // Validate methods
             foreach (var grp in items.GroupBy(i => i.MethodPlanned))
             {
                 var method = grp.Key!;
                 if (!AllowedMethods.Contains(method)) return JsonFail($"Method {method} tidak dikenal.");
                 foreach (var m in grp.Select(i => i.ModelProduk).Distinct())
                 {
                     if (!await _modelConfigService.ValidateModelSupportAsync(m, method))
                         return JsonFail($"Model {m} tidak mendukung method {method}.");
                 }
             }

             int processed = 0;
             foreach (var methodGroup in items.GroupBy(i => i.MethodPlanned))
             {
                 var method = methodGroup.Key!;
                 var rawRows = methodGroup.Select(i => new RawRow
                 {
                     RowId = i.RowNumber,
                     NoPO = i.NoPO,
                     ModelCode = i.ModelProduk,
                     Destination = i.Destination,
                     Quantity = i.QtyTotal,
                     SheetName = i.SheetName,
                     Processed = true
                 }).ToList();

                 var preview = new POUploadPreviewViewModel
                 {
                     FileName = methodGroup.First().Session?.FileName ?? $"Batch {method}",
                     RawRows = rawRows
                 };

                 await _groupingService.ApplyMethodAndGroupAsync(preview, method);
                 var ok = await _groupingService.CommitAsync(preview, User.Identity?.Name ?? "system");
                 if (!ok) return JsonFail($"Gagal memproses batch method {method}.");

                 foreach (var itm in methodGroup) itm.IsProcessed = true;
                 processed += methodGroup.Count();
             }

             await _ctx.SaveChangesAsync();
             return JsonOk(new { processed, total = items.Count }, $"{processed} dari {items.Count} item berhasil disimpan.");
         }
         catch (System.Exception ex)
         {
             return JsonFail($"Error: {ex.Message}");
         }
     }

     // Queue dan Template tetap dipertahankan & sudah ada di file (Template di atas, Queue di bawah)

     private JsonResult JsonOk(object? data = null, string? message = null) => Json(new { success = true, message, data });
     private JsonResult JsonFail(string message) => Json(new { success = false, message });
 }
