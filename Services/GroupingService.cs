using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.ViewModels;
using System.Text.Json;

namespace ShipmentFinishGood.Services;

public class GroupingService : IGroupingService
{
    private readonly AppDbContext _ctx;
    private readonly IModelConfigService _configService;
    public GroupingService(AppDbContext ctx, IModelConfigService configService)
    {
        _ctx = ctx; _configService = configService;
    }

    public Task<POUploadPreviewViewModel> BuildPreviewAsync(Stream fileStream, string fileName)
    {
        var preview = new POUploadPreviewViewModel { FileName = fileName };
        try
        {
            using var wb = new XLWorkbook(fileStream);
            var sheetNames = new List<string>();
            int nextRowId = 1;
            foreach (var ws in wb.Worksheets)
            {
                var headerMap = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
                var firstRow = ws.FirstRowUsed();
                if (firstRow == null) { preview.Messages.Add($"INFO: Sheet '{ws.Name}' kosong, dilewati."); continue; }
                sheetNames.Add(ws.Name);
                int colCount = firstRow.CellCount();
                for (int c=1;c<=colCount;c++)
                {
                    var val = firstRow.Cell(c).GetString().Trim();
                    if (!string.IsNullOrEmpty(val) && !headerMap.ContainsKey(val)) headerMap[val]=c;
                }
                string sheetDest = ws.Name; // gunakan nama sheet sebagai destination default
                string[] fullHeaders = {"NoPO","Model","Destination","Qty"};
                bool allHeaders = fullHeaders.All(r => headerMap.ContainsKey(r));
                // Header ringan tanpa Destination
                bool lightHeaders = !allHeaders && new[]{"NoPO","Model","Qty"}.All(h=>headerMap.ContainsKey(h));
                int beforeCount = preview.RawRows.Count;

                if (allHeaders || lightHeaders)
                {
                    string currentPO = string.Empty; // Track current PO for empty cells
                    foreach (var row in ws.RowsUsed().Skip(1))
                    {
                        string noPo = row.Cell(headerMap["NoPO"]).GetString().Trim();
                        string model = row.Cell(headerMap["Model"]).GetString().Trim().ToUpperInvariant();
                        string dest = allHeaders ? row.Cell(headerMap["Destination"]).GetString().Trim() : sheetDest;
                        string qtyStr = row.Cell(headerMap["Qty"]).GetString().Trim();
                        
                        // BUSINESS RULE: Jika NoPO kosong, gunakan PO dari row sebelumnya
                        if (!string.IsNullOrWhiteSpace(noPo))
                        {
                            currentPO = noPo; // Update current PO
                        }
                        else if (!string.IsNullOrWhiteSpace(currentPO))
                        {
                            noPo = currentPO; // Use previous PO
                        }
                        
                        if (string.IsNullOrWhiteSpace(noPo) && string.IsNullOrWhiteSpace(model)) continue;
                        if (!TryParseQty(qtyStr, out int qty)) { preview.Messages.Add($"ERROR: Qty invalid untuk PO {noPo} di sheet {ws.Name}."); continue; }
                        if (string.IsNullOrWhiteSpace(dest)) dest = sheetDest;
                        preview.RawRows.Add(new RawRow{ RowId = nextRowId++, NoPO=noPo, ModelCode=model, Destination=dest, Quantity=qty, SheetName=ws.Name });
                    }
                }
                else
                {
                    // Tanpa header lengkap: mode simple (PO, Model, Qty)
                    string currentPo = string.Empty;
                    int rIndex = 0;
                    foreach (var row in ws.RowsUsed())
                    {
                        rIndex++;
                        string poCell = row.Cell(1).GetString().Trim();
                        string modelCell = row.Cell(2).GetString().Trim();
                        string qtyCell = row.Cell(3).GetString().Trim();
                        // Jika baris pertama isinya persis header ringan, skip
                        if (rIndex == 1 && poCell.Equals("NoPO",StringComparison.OrdinalIgnoreCase) && modelCell.Equals("Model",StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.IsNullOrWhiteSpace(poCell) && string.IsNullOrWhiteSpace(modelCell)) continue;
                        if (!string.IsNullOrWhiteSpace(poCell)) currentPo = poCell;
                        if (string.IsNullOrWhiteSpace(currentPo) || string.IsNullOrWhiteSpace(modelCell)) continue;
                        if (!TryParseQty(qtyCell, out int qty)) { preview.Messages.Add($"ERROR: Qty invalid untuk PO {currentPo} model {modelCell} di sheet {ws.Name}."); continue; }
                        preview.RawRows.Add(new RawRow{ RowId = nextRowId++, NoPO=currentPo, ModelCode=modelCell.ToUpperInvariant(), Destination=sheetDest, Quantity=qty, SheetName=ws.Name });
                    }
                }
                int added = preview.RawRows.Count - beforeCount;
                preview.Messages.Add(added>0 ? $"INFO: Sheet '{ws.Name}' -> {added} baris valid." : $"INFO: Sheet '{ws.Name}' tidak menghasilkan baris valid.");
            }

            preview.SheetNames = sheetNames.Distinct().OrderBy(x=>x).ToList();

            if (!preview.RawRows.Any()) preview.Messages.Add("ERROR: Tidak ada baris valid.");
        }
        catch (Exception ex)
        {
            preview.Messages.Add("ERROR: Gagal parse file: "+ex.Message);
        }
    return Task.FromResult(preview);
    }

    // Parse excel dan simpan langsung ke tabel session + items. Return SessionId
    public async Task<int> ParseAndStoreAsync(Stream fileStream, string fileName, string createdBy)
    {
        var preview = await BuildPreviewAsync(fileStream, fileName);
        // Buat session baru
        var session = new ExcelUploadSession
        {
            FileName = fileName,
            Status = "PREVIEW",
            TotalItems = preview.RawRows.Count,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        _ctx.ExcelUploadSessions.Add(session);
        await _ctx.SaveChangesAsync();
        // Simpan items dengan SheetName yang benar
        foreach (var rr in preview.RawRows)
        {
            _ctx.ExcelUploadItems.Add(new ExcelUploadItem
            {
                SessionId = session.SessionId,
                NoPO = rr.NoPO,
                ModelProduk = rr.ModelCode,
                QtyTotal = rr.Quantity,
                Destination = rr.Destination,
                SheetName = rr.SheetName, // Simpan sheet name yang benar
                RowNumber = rr.RowId,
                IsProcessed = rr.Processed,
                MethodPlanned = null // Akan diset saat user pilih method
            });
        }
        await _ctx.SaveChangesAsync();
        return session.SessionId;
    }

    // Load preview (RawRows) dari DB berdasarkan SessionId
    public async Task<POUploadPreviewViewModel> LoadSessionPreviewAsync(int sessionId)
    {
        var session = await _ctx.ExcelUploadSessions.Include(s=>s.Items).FirstOrDefaultAsync(s=>s.SessionId==sessionId);
        if (session == null)
        {
            return new POUploadPreviewViewModel { FileName = "(Session tidak ditemukan)", Messages = { "ERROR: Session tidak ditemukan." } };
        }
        var preview = new POUploadPreviewViewModel
        {
            FileName = session.FileName,
            RawRows = session.Items.Select(i => new RawRow
            {
                RowId = i.RowNumber,
                NoPO = i.NoPO,
                ModelCode = i.ModelProduk,
                Destination = i.Destination,
                Quantity = i.QtyTotal,
                SheetName = i.SheetName, // Gunakan SheetName yang tersimpan
                Processed = i.IsProcessed
            }).OrderBy(r=>r.RowId).ToList()
        };
        preview.SheetNames = preview.RawRows.Select(r=>r.SheetName).Distinct().OrderBy(x=>x).ToList();
        
        // Hitung statistik
        var processedCount = session.Items.Count(i => i.IsProcessed);
        var pendingCount = session.TotalItems - processedCount;
        preview.Messages.Add($"INFO: Session {sessionId} dimuat. {preview.RawRows.Count} baris total ({processedCount} selesai, {pendingCount} pending).");
        
        return preview;
    }

    public async Task<bool> CommitAsync(POUploadPreviewViewModel preview, string performedBy)
    {
        try
        {
            Console.WriteLine($"CommitAsync started. Items count: {preview.Items.Count}, RawRows count: {preview.RawRows.Count}");
            
            // Debug: Check for processed rows
            var processedRows = preview.RawRows.Where(r => r.Processed).ToList();
            Console.WriteLine($"Processed rows count: {processedRows.Count}");
            
            // 1. Find or create ExcelUploadSession for this file/user
            var session = await _ctx.ExcelUploadSessions
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.FileName == preview.FileName && s.Status != "COMPLETED");
            if (session == null)
            {
                Console.WriteLine($"Creating new session for file: {preview.FileName}");
                session = new ExcelUploadSession
                {
                    FileName = preview.FileName,
                    Status = "PREVIEW",
                    TotalItems = preview.RawRows.Count,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = performedBy
                };
                _ctx.ExcelUploadSessions.Add(session);
                await _ctx.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine($"Found existing session ID: {session.SessionId}");
            }

            // 2. Ensure ExcelUploadItems exist for all RawRows (by RowId)
            foreach (var rr in preview.RawRows)
            {
                var item = session.Items.FirstOrDefault(i => i.RowNumber == rr.RowId);
                if (item == null)
                {
                    item = new ExcelUploadItem
                    {
                        SessionId = session.SessionId,
                        NoPO = rr.NoPO,
                        ModelProduk = rr.ModelCode,
                        QtyTotal = rr.Quantity,
                        Destination = rr.Destination,
                        RowNumber = rr.RowId,
                        IsSelected = rr.Processed // match dengan status processed
                    };
                    session.Items.Add(item);
                }
            }
            await _ctx.SaveChangesAsync();

            int processedCount = 0;
            Console.WriteLine($"Processing {preview.Items.Count} groups");
            
            // 3. For each group, persist as before, and mark ExcelUploadItems as processed
            foreach (var grp in preview.Items)
            {
                Console.WriteLine($"Processing group: {grp.ModelCode} - {grp.NoPO} - Qty: {grp.Quantity}");
                
                var poNos = grp.NoPO!.Split('+', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
                var poEntities = new List<PO>();
                foreach (var no in poNos)
                {
                    var existing = await _ctx.POs.FirstOrDefaultAsync(p => p.NoPO == no);
                    if (existing == null)
                    {
                        existing = new PO { NoPO = no, Destination = grp.Destination ?? string.Empty };
                        _ctx.POs.Add(existing);
                        await _ctx.SaveChangesAsync(); // Save to get PO ID
                        Console.WriteLine($"Created new PO: {no}");
                    }
                    else
                    {
                        Console.WriteLine($"Found existing PO: {no}");
                    }
                    poEntities.Add(existing);
                }
                
                var method = grp.Method ?? preview.SelectedMethod ?? "";
                string signature = BuildSignature(method, grp.ModelCode!, grp.Destination!, poNos);
                bool already = await _ctx.ShipmentGroups.AnyAsync(g => g.GroupSignature == signature);
                if (already) 
                {
                    Console.WriteLine($"Skipping duplicate group with signature: {signature}");
                    continue; // skip duplicates
                }
                
                var sg = new ShipmentGroup
                {
                    ShipmentMethod = method,
                    ModelCode = grp.ModelCode!,
                    Destination = grp.Destination ?? string.Empty,
                    MixedPO = poNos.Length > 1,
                    DisplayNoPO = string.Join(" + ", poNos),
                    GroupSignature = signature,
                    Status = "Calculated",
                    CreatedBy = performedBy
                };
                
                var breakdown = new GroupBreakdown
                {
                    ShipmentGroup = sg,
                    QtyTotal = grp.Quantity,
                    QtyPallet = grp.QtyPallet,
                    QtyBox = grp.QtyBox,
                    QtyPcs = grp.QtyPcs,
                    PcsPerPallet = grp.QtyPallet==0?0: (grp.Quantity / (grp.QtyPallet==0?1:grp.QtyPallet)),
                    PcsPerBox = grp.QtyBox==0?0: (grp.Quantity / (grp.QtyBox==0?1:grp.QtyBox)),
                    IntegrityOk = grp.IntegrityOk
                };
                
                _ctx.ShipmentGroups.Add(sg);
                _ctx.GroupBreakdowns.Add(breakdown);
                await _ctx.SaveChangesAsync();
                Console.WriteLine($"Created ShipmentGroup ID: {sg.Id}");
                
                foreach (var po in poEntities)
                {
                    _ctx.ShipmentGroupPOs.Add(new ShipmentGroupPO{ ShipmentGroupId = sg.Id, POId = po.Id, QtyContribution = grp.Quantity });
                }

                // Mark ExcelUploadItems as processed and link to PO
                var matchingRows = preview.RawRows.Where(r =>
                    r.Processed && // hanya baris yang dipilih user pada step ini
                    r.ModelCode == grp.ModelCode &&
                    r.Destination == grp.Destination &&
                    poNos.Contains(r.NoPO)
                ).ToList();
                
                Console.WriteLine($"Found {matchingRows.Count} matching rows for this group");
                
                foreach (var row in matchingRows)
                {
                    var item = session.Items.FirstOrDefault(i => i.RowNumber == row.RowId);
                    if (item != null && !item.IsProcessed)
                    {
                        item.IsProcessed = true;
                        item.ProcessedPOId = poEntities.FirstOrDefault()?.Id;
                        processedCount++;
                        Console.WriteLine($"Marked item {item.RowNumber} as processed");
                    }
                }
                await _ctx.SaveChangesAsync();
            }

            // 4. Update session status dan processed count/date (akumulatif)
            session.ProcessedItems = session.Items.Count(i => i.IsProcessed); // hitung ulang dari DB
            if (session.ProcessedItems >= session.TotalItems)
            {
                session.Status = "COMPLETED";
                session.ProcessedDate = DateTime.UtcNow;
            }
            else if (session.ProcessedItems > 0)
            {
                session.Status = "PARTIAL";
            }
            await _ctx.SaveChangesAsync();
            
            Console.WriteLine($"CommitAsync completed successfully. Processed {processedCount} items.");
            return true;
        }
        catch (Exception ex)
        {
            // Log error untuk debugging
            Console.WriteLine($"CommitAsync Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<POUploadPreviewViewModel> ApplyMethodAndGroupAsync(POUploadPreviewViewModel preview, string method)
    {
        preview.SelectedMethod = method;
        preview.Items.Clear();
        if (!preview.RawRows.Any()) return preview;
        method = method.ToUpperInvariant();
        
        // Validate all models support the selected method
        var modelValidation = new List<string>();
        foreach (var model in preview.RawRows.Select(r => r.ModelCode).Distinct())
        {
            var isSupported = await _configService.ValidateModelSupportAsync(model, method);
            if (!isSupported)
            {
                modelValidation.Add($"ERROR: Model {model} tidak mendukung method {method}");
            }
        }
        
        if (modelValidation.Any())
        {
            preview.Messages.AddRange(modelValidation);
            return preview;
        }

        if (method == "LOOSE")
        {
            // LOOSE: Group by Model + Destination (ignore PO for merging)
            var byModelDest = preview.RawRows.GroupBy(r => new { r.ModelCode, r.Destination });
            foreach (var g in byModelDest)
            {
                var qtyTotal = g.Sum(x => x.Quantity);
                var poNos = g.Select(r => r.NoPO).Distinct().OrderBy(x => x).ToList();
                
                try
                {
                    var (pcsPerPallet, pcsPerBox) = await _configService.GetCapacityAsync(g.Key.ModelCode, method);
                    CalcBreakdown(qtyTotal, pcsPerPallet, pcsPerBox, out int qPal, out int qBox, out int qPcs);
                    
                    preview.Items.Add(new POUploadItemViewModel
                    {
                        NoPO = string.Join(" + ", poNos), // Show merged POs clearly
                        ModelCode = g.Key.ModelCode,
                        Destination = g.Key.Destination,
                        Method = method,
                        Quantity = qtyTotal,
                        MixedPO = poNos.Count > 1, // Flag when multiple POs merged
                        QtyPallet = qPal,
                        QtyBox = qBox,
                        QtyPcs = qPcs,
                        IntegrityOk = (qPal * pcsPerPallet + qBox * pcsPerBox + qPcs) == qtyTotal,
                        Notes = poNos.Count > 1 ? $"Merged from POs: {string.Join(", ", poNos)}" : null
                    });
                }
                catch (Exception ex)
                {
                    preview.Messages.Add($"ERROR: {ex.Message}");
                }
            }
        }
        else if (method == "PALLET")
        {
            // PALLET: Group by PO + Model + Destination (no merging across different PO numbers)
            foreach (var g in preview.RawRows.GroupBy(r => new { r.NoPO, r.ModelCode, r.Destination }))
            {
                int qtyTotal = g.Sum(x => x.Quantity);
                
                try
                {
                    var (pcsPerPallet, pcsPerBox) = await _configService.GetCapacityAsync(g.Key.ModelCode, method);
                    CalcBreakdown(qtyTotal, pcsPerPallet, pcsPerBox, out int qPal, out int qBox, out int qPcs);
                    
                    preview.Items.Add(new POUploadItemViewModel
                    {
                        NoPO = g.Key.NoPO,
                        ModelCode = g.Key.ModelCode,
                        Destination = g.Key.Destination,
                        Method = method,
                        Quantity = qtyTotal,
                        MixedPO = false, // PALLET never mixes POs
                        QtyPallet = qPal,
                        QtyBox = qBox,
                        QtyPcs = qPcs,
                        IntegrityOk = (qPal * pcsPerPallet + qBox * pcsPerBox + qPcs) == qtyTotal
                    });
                }
                catch (Exception ex)
                {
                    preview.Messages.Add($"ERROR: {ex.Message}");
                }
            }
        }
        else
        {
            preview.Messages.Add($"ERROR: Method {method} tidak dikenal. Gunakan LOOSE atau PALLET.");
        }
        return preview;
    }

    private static void CalcBreakdown(int qtyTotal, int pcsPerPallet, int pcsPerBox, out int qPal, out int qBox, out int qPcs)
    {
        qPal = qBox = 0; qPcs = qtyTotal;
        if (pcsPerPallet > 0)
        {
            qPal = qtyTotal / pcsPerPallet;
            qPcs = qtyTotal % pcsPerPallet;
        }
        if (pcsPerBox > 0)
        {
            qBox = qPcs / pcsPerBox;
            qPcs = qPcs % pcsPerBox;
        }
    }

    private static string BuildSignature(string method,string model,string destination,IEnumerable<string> poNos)
    {
        return string.Join('|', new[]{method, model, destination, string.Join('+', poNos.OrderBy(x=>x))});
    }

    // Simpan pilihan method per row ke database
    public async Task<bool> SaveRowMethodsAsync(int sessionId, Dictionary<int, string> rowMethods)
    {
        try
        {
            var items = await _ctx.ExcelUploadItems
                .Where(i => i.SessionId == sessionId && !i.IsProcessed)
                .ToListAsync();
            
            foreach (var item in items)
            {
                if (rowMethods.TryGetValue(item.RowNumber, out var method))
                {
                    item.MethodPlanned = method;
                }
            }
            
            await _ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving row methods: {ex.Message}");
            return false;
        }
    }

    // Parsing angka: hilangkan pemisah ribuan (.,), abaikan teks non digit di akhir (misal "Sets")
    private static bool TryParseQty(string raw, out int qty)
    {
        qty = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim();
        // Pisahkan pertama kali jika ada spasi (misal "3.486 Sets")
        var firstToken = raw.Split(' ', '\t')[0];
        // Hilangkan pemisah ribuan umum
        firstToken = firstToken.Replace(".", string.Empty).Replace(",", string.Empty);
        return int.TryParse(firstToken, out qty);
    }
}
