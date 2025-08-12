namespace ShipmentFinishGood.DTOs
{
    public class ExcelRowData
    {
        public string? NoPO { get; set; }
        public string? Model { get; set; }
        public int Qty { get; set; }
        public int RowIndex { get; set; }
    }

    public class ProcessedPOData
    {
        public string NoPO { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public int QtyPallet { get; set; }
        public int QtyBox { get; set; }
        public int QtyPcs { get; set; }
        public string? Container { get; set; }
        public string? NoInvoice { get; set; }
        public string? ShipmentDetail { get; set; }
        public bool CanEdit { get; set; } = true;
    }

    public class UploadPreviewDto
    {
        public int SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public List<ExcelRowData> RawData { get; set; } = new();
        public List<ProcessedPOData> ProcessedData { get; set; } = new();
        public string? ShipmentType { get; set; }
        public DateTime? ShipmentDate { get; set; }
        public string? PalletPrefix { get; set; }
        public string? PcsPrefix { get; set; }
    }

    public class CalculationRequest
    {
        public int SessionId { get; set; }
        public string ShipmentType { get; set; } = string.Empty; // LOOSE or PALLET
        public DateTime ShipmentDate { get; set; }
        public string? PalletPrefix { get; set; }
        public string? PcsPrefix { get; set; }
        public List<ProcessedPOData> ProcessedData { get; set; } = new();
    }
}
