namespace ShipmentFinishGood.DTOs
{
    // DTOs terkait PO sudah didefinisikan di ExcelDtos.cs
    // File ini bisa digunakan untuk DTOs PO yang spesifik jika diperlukan
    
    /// <summary>
    /// Request untuk auto-fix missing barcodes
    /// </summary>
    public class AutoFixRequest
    {
        public int SessionId { get; set; }
        public string? Reason { get; set; }
    }
}
