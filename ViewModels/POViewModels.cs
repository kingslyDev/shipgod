using System.Text.Json.Serialization;
namespace ShipmentFinishGood.ViewModels;

public class POUploadViewModel
{
    public IFormFile? File { get; set; }
    public string? SelectedMethod { get; set; } // LOOSE / PALLET (chosen after parsing)
    public string? SelectedSheet { get; set; } // Filter sheet (ALL / specific)
    public Dictionary<string,string>? SheetMethods { get; set; } = new(); // per sheet method selection
    public string? ActionType { get; set; } // preview, chooseMethod, commit
    public string? RawJson { get; set; } // serialized raw rows to avoid re-reading file
    public int? SessionId { get; set; } // ID session upload tersimpan di DB
}

public class POUploadItemViewModel
{
    public string? NoPO { get; set; }
    public string? ModelCode { get; set; }
    public string? Destination { get; set; }
    public string? Method { get; set; }
    public int Quantity { get; set; }
    public int QtyPallet { get; set; }
    public int QtyBox { get; set; }
    public int QtyPcs { get; set; }
    public bool MixedPO { get; set; }
    public bool IntegrityOk { get; set; }
    public string? Notes { get; set; }
}

public class POUploadPreviewViewModel
{
    public string FileName { get; set; } = string.Empty;
    public List<POUploadItemViewModel> Items { get; set; } = new();
    public List<string> Messages { get; set; } = new();
    public string? SelectedMethod { get; set; }
    public string? SelectedSheet { get; set; }
    public bool Parsed => RawRows.Any();
    public List<RawRow> RawRows { get; set; } = new();
    public bool CanCommit => Items.Any() && Messages.All(m => !m.StartsWith("ERROR"));
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalItems => Items.Count;
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);
    public List<string> SheetNames { get; set; } = new();
    public Dictionary<string,string> SheetMethods { get; set; } = new();
    public Dictionary<int,string> RowMethods { get; set; } = new(); // RowId -> method
    [JsonIgnore]
    public IEnumerable<POUploadItemViewModel> PageItems => Items.Skip((Page - 1) * PageSize).Take(PageSize);
}

public class RawRow
{
    public int RowId { get; set; }
    public string NoPO { get; set; } = string.Empty;
    public string ModelCode { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty; // asal sheet
    public int Quantity { get; set; }
    public bool Processed { get; set; } = false; // sudah di-commit
}

// Compact persisted state (without calculated grouping Items) to minimize payload
public class POUploadState
{
    public string FileName { get; set; } = string.Empty;
    public List<RawRow> RawRows { get; set; } = new();
    public string? SelectedMethod { get; set; }
    public string? SelectedSheet { get; set; }
    public Dictionary<string,string> SheetMethods { get; set; } = new();
    public Dictionary<int,string> RowMethods { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class POQueueItemViewModel
{
    public int ItemId { get; set; }
    public int RowNumber { get; set; }
    public string NoPO { get; set; } = string.Empty;
    public string ModelCode { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsProcessed { get; set; }
    public string? MethodPlanned { get; set; }
    public int SessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string SheetName { get; set; } = string.Empty;
}

public class POQueueViewModel
{
    public List<POQueueItemViewModel> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);
    public string? StatusFilter { get; set; }
    public string? Search { get; set; }
    public int? SessionFilter { get; set; }
}
