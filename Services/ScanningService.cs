using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Hubs;

namespace ShipmentFinishGood.Services
{
    public class ScanningService : IScanningService
    {
        private readonly AppDbContext _context;
        private readonly IBarcodeService _barcodeService;
        private readonly IHubContext<ProgressHub> _hubContext;

        public ScanningService(AppDbContext context, IBarcodeService barcodeService, IHubContext<ProgressHub> hubContext)
        {
            _context = context;
            _barcodeService = barcodeService;
            _hubContext = hubContext;
        }

        public async Task<List<ScanSessionSummaryDto>> GetActiveSessionsAsync()
        {
            // Get all sessions that have QR Identity and PO data, regardless of status
            var sessions = await _context.UploadSessions
                .Include(s => s.POMasters)
                .Where(s => !string.IsNullOrEmpty(s.IdentityQRCode) && s.POMasters.Any())
                .OrderByDescending(s => s.UploadDate)
                .ToListAsync();

            // If no sessions found, try to auto-generate QR for VALIDATED sessions
            if (!sessions.Any())
            {
                var validatedSessions = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => (s.Status == "VALIDATED" || s.Status == "PROCESSED") && s.POMasters.Any())
                    .OrderByDescending(s => s.UploadDate)
                    .ToListAsync();

                foreach (var session in validatedSessions)
                {
                    if (string.IsNullOrEmpty(session.IdentityQRCode))
                    {
                        // Auto-generate QR Identity if missing
                        session.IdentityQRCode = $"QR_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
                        session.Status = "QR_GENERATED";
                        
                        // Update TotalBoxes
                        session.TotalBoxes = session.POMasters.Sum(p => p.QtyBox);
                        
                        _context.UploadSessions.Update(session);

                        // Generate barcodes for the session
                        await _barcodeService.GenerateBarcodesForSessionAsync(session.SessionId, "System");
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Re-fetch sessions after generating QR codes
                sessions = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => !string.IsNullOrEmpty(s.IdentityQRCode) && s.POMasters.Any())
                    .OrderByDescending(s => s.UploadDate)
                    .ToListAsync();
            }

            return sessions.Select(session => new ScanSessionSummaryDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                QRIdentity = session.IdentityQRCode!,
                ShipmentType = session.ShipmentType ?? "LOOSE",
                ShipmentDate = session.ShipmentDate,
                TotalBoxes = session.TotalBoxes > 0 ? session.TotalBoxes : session.POMasters.Sum(p => p.QtyBox),
                TotalPOs = session.POMasters.Count,
                CreatedDate = session.UploadDate,
                CreatedBy = session.UploadedBy,
                Status = GetScanStatus(session.SessionId)
            }).ToList();
        }

        public async Task<ScanSessionDto?> GetScanSessionAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return null;

            // Auto-generate QR Identity if missing
            if (string.IsNullOrEmpty(session.IdentityQRCode))
            {
                session.IdentityQRCode = $"QR_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
                session.Status = "QR_GENERATED";
                _context.UploadSessions.Update(session);
                await _context.SaveChangesAsync();

                // Generate barcodes after QR Identity is created
                await _barcodeService.GenerateBarcodesForSessionAsync(sessionId, "System");
            }

            // Check if barcodes exist, if not generate them
            var totalBarcodes = await _barcodeService.GetTotalBarcodeCountAsync(sessionId);
            if (totalBarcodes == 0)
            {
                await _barcodeService.GenerateBarcodesForSessionAsync(sessionId, "System");
            }

            var barcodes = await _barcodeService.GetBarcodeListForSessionAsync(sessionId);
            var scannedBarcodes = await _barcodeService.GetScannedBarcodesAsync(sessionId);
            var scannedCount = await _barcodeService.GetScannedBarcodeCountAsync(sessionId);
            
            return new ScanSessionDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                QRIdentity = session.IdentityQRCode,
                ShipmentType = session.ShipmentType ?? "LOOSE",
                ShipmentDate = session.ShipmentDate,
                TotalBoxes = session.TotalBoxes,
                TotalBarcodes = barcodes.Count,
                ScannedCount = scannedCount,
                BarcodeList = barcodes.Select(b => new BarcodeDto 
                { 
                    BarcodeValue = b.BarcodeValue, 
                    ModelProduct = b.ModelProduct, 
                    BoxNumber = b.BoxNumber 
                }).ToList(),
                ScannedBarcodes = scannedBarcodes,
                IsMasterScanned = await IsMasterQRScannedAsync(sessionId),
                CanComplete = await _barcodeService.IsSessionCompleteAsync(sessionId)
            };
        }

        public async Task<Result<ScanResultDto>> ScanMasterQRAsync(int sessionId, string qrCode, string scannedBy)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null)
                return Result<ScanResultDto>.Failure("Session not found");

            if (session.Status == "SCAN_COMPLETED")
                return Result<ScanResultDto>.Failure("Session has been completed. No further scans allowed.");

            if (session.IdentityQRCode != qrCode)
                return Result<ScanResultDto>.Failure("Invalid QR code for this session");

            // Check if user is already locked to another session
            var userLockCheck = await CheckUserLockAsync(scannedBy);
            if (!userLockCheck.IsSuccess)
            {
                var lockedSessionQR = await GetUserLockedSessionAsync(scannedBy);
                if (lockedSessionQR.IsSuccess && lockedSessionQR.Value != qrCode)
                    return Result<ScanResultDto>.Failure($"You are locked to session {lockedSessionQR.Value}. Complete that session first.");
            }

            // Check if already scanned
            var existingScan = await _context.ScanningActivities
                .FirstOrDefaultAsync(sa => sa.BarcodeValue == qrCode && sa.Action == "SCAN_MASTER");

            if (existingScan != null)
                return Result<ScanResultDto>.Failure("Master QR already scanned");

            // Mark the master QR barcode as scanned in BarcodeRegistry
            var markMasterResult = await _barcodeService.MarkBarcodeAsScannedAsync(qrCode, scannedBy);
            if (!markMasterResult.IsSuccess)
                return Result<ScanResultDto>.Failure($"Failed to mark master QR as scanned: {markMasterResult.Error}");

            // Record the scan - this locks the user to this session
            var scanActivity = new ScanningActivity
            {
                BarcodeValue = qrCode,
                Action = "SCAN_MASTER",
                UserId = scannedBy,
                Timestamp = DateTime.Now,
                Result = "SUCCESS"
            };

            _context.ScanningActivities.Add(scanActivity);
            await _context.SaveChangesAsync();

            var result = new ScanResultDto
            {
                BarcodeValue = qrCode,
                ScanType = "MASTER_QR",
                Message = "Master QR scanned successfully. You are now locked to this session.",
                Timestamp = DateTime.Now,
                ScannedBy = scannedBy
            };

            return Result<ScanResultDto>.Success(result);
        }

        public async Task<Result<ScanResultDto>> ScanBoxBarcodeAsync(int sessionId, string barcode, string scannedBy)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null)
                return Result<ScanResultDto>.Failure("Session not found");

            if (session.Status == "SCAN_COMPLETED")
                return Result<ScanResultDto>.Failure("Session has been completed. No further scans allowed.");

            // Validate barcode format
            if (!barcode.StartsWith(session.IdentityQRCode!))
                return Result<ScanResultDto>.Failure("Invalid barcode for this session");

            // Check if user is locked to this session
            var userLockResult = await CheckUserLockAsync(scannedBy);
            if (!userLockResult.IsSuccess)
                return Result<ScanResultDto>.Failure("You must scan Master QR first to lock to a session");

            var lockedSessionResult = await GetUserLockedSessionAsync(scannedBy);
            if (!lockedSessionResult.IsSuccess || lockedSessionResult.Value != session.IdentityQRCode)
                return Result<ScanResultDto>.Failure("You can only scan boxes from your locked session");

            // Check if master QR was scanned first
            var masterScanned = await IsMasterQRScannedAsync(sessionId);
            if (!masterScanned)
                return Result<ScanResultDto>.Failure("Please scan Master QR first");

            // Check if barcode already scanned using BarcodeService
            var isAlreadyScanned = await _barcodeService.IsBarcodeScannedAsync(barcode);
            if (isAlreadyScanned)
                return Result<ScanResultDto>.Failure("Box already scanned");

            // Validate if barcode exists and is valid using BarcodeService
            var isValidBarcode = await _barcodeService.IsBarcodeValidAsync(barcode, sessionId);
            if (!isValidBarcode)
                return Result<ScanResultDto>.Failure("Invalid barcode");

            // Mark barcode as scanned using BarcodeService
            var markResult = await _barcodeService.MarkBarcodeAsScannedAsync(barcode, scannedBy);
            if (!markResult.IsSuccess)
                return Result<ScanResultDto>.Failure(markResult.Error ?? "Failed to mark barcode as scanned");

            // Record the scan in ScanningActivities for audit trail
            var scanActivity = new ScanningActivity
            {
                BarcodeValue = barcode,
                Action = "SCAN_BOX",
                UserId = scannedBy,
                Timestamp = DateTime.Now,
                Result = "SUCCESS"
            };

            _context.ScanningActivities.Add(scanActivity);
            await _context.SaveChangesAsync();

            // Send realtime update via SignalR
            await SendProgressUpdateAsync(sessionId, barcode);

            var result = new ScanResultDto
            {
                BarcodeValue = barcode,
                ScanType = "BOX_BARCODE",
                Message = "Box scanned successfully",
                Timestamp = DateTime.Now,
                ScannedBy = scannedBy
            };

            return Result<ScanResultDto>.Success(result);
        }



        public async Task<ScanProgressDto> GetScanProgressAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return new ScanProgressDto();

            var totalBarcodes = await _barcodeService.GetTotalBarcodeCountAsync(sessionId);
            var scannedCount = await _barcodeService.GetScannedBarcodeCountAsync(sessionId);
            var masterScanned = await IsMasterQRScannedAsync(sessionId);

            return new ScanProgressDto
            {
                SessionId = sessionId,
                TotalBarcodes = totalBarcodes,
                ScannedCount = scannedCount,
                ProgressPercentage = totalBarcodes > 0 ? (double)scannedCount / totalBarcodes * 100 : 0,
                IsMasterScanned = masterScanned,
                LastScanTime = await GetLastScanTimeAsync(sessionId),
                CanComplete = await _barcodeService.IsSessionCompleteAsync(sessionId)
            };
        }

        public async Task<List<BarcodeItemDto>> GetBarcodeListAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return new List<BarcodeItemDto>();

            var barcodes = await _barcodeService.GetBarcodesForSessionAsync(sessionId);
            
            return barcodes.Select(b => new BarcodeItemDto
            {
                BarcodeValue = b.BarcodeValue,
                ModelProduct = b.ModelProduct,
                BoxNumber = b.BoxNumber,
                IsScanned = b.ScannedDate.HasValue,
                ScannedTime = b.ScannedDate
            }).ToList();
        }

        public async Task<List<ScanHistoryDto>> GetScanHistoryAsync()
        {
            var activities = await _context.ScanningActivities
                .Where(sa => sa.Action != "AREA_ASSIGN") // Exclude area assignments from history
                .OrderByDescending(sa => sa.Timestamp)
                .Take(100)
                .ToListAsync();

            return activities.Select(a => new ScanHistoryDto
            {
                ActivityId = a.ActivityId,
                BarcodeValue = a.BarcodeValue,
                Action = a.Action,
                UserId = a.UserId,
                Timestamp = a.Timestamp,
                Result = a.Result,
                ErrorMessage = a.ErrorMessage
            }).ToList();
        }

        public async Task<ScanSessionDetailDto?> GetSessionDetailAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return null;

            var activities = await _context.ScanningActivities
                .Where(sa => sa.BarcodeValue.StartsWith(session.IdentityQRCode!) || 
                           sa.BarcodeValue == session.IdentityQRCode)
                .Where(sa => sa.Action != "AREA_ASSIGN") // Exclude area assignments
                .OrderByDescending(sa => sa.Timestamp)
                .ToListAsync();

            return new ScanSessionDetailDto
            {
                SessionId = sessionId,
                FileName = session.FileName,
                QRIdentity = session.IdentityQRCode!,
                ShipmentType = session.ShipmentType ?? "LOOSE",
                TotalBoxes = session.TotalBoxes,
                ScanActivities = activities.Select(a => new ScanHistoryDto
                {
                    ActivityId = a.ActivityId,
                    BarcodeValue = a.BarcodeValue,
                    Action = a.Action,
                    UserId = a.UserId,
                    Timestamp = a.Timestamp,
                    Result = a.Result,
                    ErrorMessage = a.ErrorMessage
                }).ToList()
            };
        }

        public async Task<Result<bool>> CompleteScanAsync(int sessionId, string completedBy)
        {
            var canComplete = await CanCompleteScanAsync(sessionId);
            if (!canComplete)
                return Result<bool>.Failure("Cannot complete scan. Not all boxes are scanned or master QR not scanned.");

            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null)
                return Result<bool>.Failure("Session not found");

            session.Status = "SCAN_COMPLETED";
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CheckUserLockAsync(string userId)
        {
            // Check if user has scanned a master QR for any active (not completed) session
            var userMasterScan = await _context.ScanningActivities
                .Where(sa => sa.UserId == userId && sa.Action == "SCAN_MASTER")
                .Join(_context.UploadSessions,
                    sa => sa.BarcodeValue,
                    us => us.IdentityQRCode,
                    (sa, us) => new { sa, us })
                .Where(joined => joined.us.Status != "SCAN_COMPLETED")
                .OrderByDescending(joined => joined.sa.Timestamp)
                .FirstOrDefaultAsync();

            return userMasterScan != null 
                ? Result<bool>.Success(true)
                : Result<bool>.Failure("User is not locked to any session");
        }

        public async Task<Result<string>> GetUserLockedSessionAsync(string userId)
        {
            var userMasterScan = await _context.ScanningActivities
                .Where(sa => sa.UserId == userId && sa.Action == "SCAN_MASTER")
                .Join(_context.UploadSessions,
                    sa => sa.BarcodeValue,
                    us => us.IdentityQRCode,
                    (sa, us) => new { sa, us })
                .Where(joined => joined.us.Status != "SCAN_COMPLETED")
                .OrderByDescending(joined => joined.sa.Timestamp)
                .Select(joined => joined.sa.BarcodeValue)
                .FirstOrDefaultAsync();

            return userMasterScan != null 
                ? Result<string>.Success(userMasterScan)
                : Result<string>.Failure("User is not locked to any session");
        }

        // Helper methods
        private List<BarcodeDto> GenerateBarcodeList(UploadSession session)
        {
            var barcodes = new List<BarcodeDto>();
            
            foreach (var poMaster in session.POMasters)
            {
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    var barcode = $"{session.IdentityQRCode}_BOX_{poMaster.ModelProduk}_{i:D3}";
                    barcodes.Add(new BarcodeDto
                    {
                        BarcodeValue = barcode,
                        ModelProduct = poMaster.ModelProduk,
                        BoxNumber = i
                    });
                }
            }

            return barcodes;
        }

        private List<string> GenerateBarcodesForSession(UploadSession session)
        {
            var barcodes = new List<string>();
            
            foreach (var poMaster in session.POMasters)
            {
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    barcodes.Add($"{session.IdentityQRCode}_BOX_{poMaster.ModelProduk}_{i:D3}");
                }
            }

            return barcodes;
        }

        private async Task<List<string>> GetScannedBarcodesAsync(int sessionId)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null) return new List<string>();

            return await _context.ScanningActivities
                .Where(sa => sa.BarcodeValue.StartsWith(session.IdentityQRCode!) && sa.Action == "SCAN_BOX")
                .Select(sa => sa.BarcodeValue)
                .ToListAsync();
        }

        private async Task<bool> IsMasterQRScannedAsync(int sessionId)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null) return false;

            return await _context.ScanningActivities
                .AnyAsync(sa => sa.BarcodeValue == session.IdentityQRCode && sa.Action == "SCAN_MASTER");
        }

        private string GetScanStatus(int sessionId)
        {
            var masterScanned = IsMasterQRScannedAsync(sessionId).Result;
            var progress = GetScanProgressAsync(sessionId).Result;

            if (progress.CanComplete) return "Completed";
            if (progress.ScannedCount > 0) return "In Progress";
            if (masterScanned) return "Ready";
            return "Pending";
        }



        private async Task<DateTime?> GetLastScanTimeAsync(int sessionId)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null) return null;

            return await _context.ScanningActivities
                .Where(sa => sa.BarcodeValue.StartsWith(session.IdentityQRCode!))
                .OrderByDescending(sa => sa.Timestamp)
                .Select(sa => (DateTime?)sa.Timestamp)
                .FirstOrDefaultAsync();
        }

        private DateTime? GetScanTime(string barcode, List<string> scannedBarcodes)
        {
            if (!scannedBarcodes.Contains(barcode)) return null;

            return _context.ScanningActivities
                .Where(sa => sa.BarcodeValue == barcode && sa.Action == "SCAN_BOX")
                .Select(sa => (DateTime?)sa.Timestamp)
                .FirstOrDefault();
        }

        private async Task<bool> CanCompleteScanAsync(int sessionId)
        {
            var progress = await GetScanProgressAsync(sessionId);
            return progress.IsMasterScanned && progress.ScannedCount == progress.TotalBarcodes;
        }

        // Helper method to force QR generation for testing
        public async Task<Result<string>> GenerateQRForSessionAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return Result<string>.Failure("Session not found");

            if (!session.POMasters.Any())
                return Result<string>.Failure("Session has no PO data to scan");

            if (string.IsNullOrEmpty(session.IdentityQRCode))
            {
                session.IdentityQRCode = $"QR_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
                session.Status = "QR_GENERATED";
                _context.UploadSessions.Update(session);
                await _context.SaveChangesAsync();
            }

            return Result<string>.Success(session.IdentityQRCode);
        }

        private async Task SendProgressUpdateAsync(int sessionId, string scannedBarcode)
        {
            try
            {
                // Extract PO information from barcode to get the specific PO that was updated
                var barcodeRegistry = await _context.BarcodeRegistries
                    .Include(b => b.POMaster)
                    .FirstOrDefaultAsync(b => b.BarcodeValue == scannedBarcode && b.SessionId == sessionId);

                if (barcodeRegistry?.POId != null)
                {
                    // Get updated progress for the specific PO
                    var poProgress = await _barcodeService.GetProgressByPOIdAsync(sessionId, barcodeRegistry.POId.Value);
                    
                    if (poProgress != null)
                    {
                        // Send update to all clients in the session group
                        await _hubContext.Clients.Group($"Session_{sessionId}")
                            .SendAsync("ProgressUpdate", new
                            {
                                sessionId = sessionId,
                                poId = barcodeRegistry.POId.Value,
                                progress = poProgress,
                                lastScannedBarcode = scannedBarcode
                            });
                    }
                }

                // Also send overall session progress
                var overallProgress = await _barcodeService.GetProgressByPOAsync(sessionId);
                await _hubContext.Clients.Group($"Session_{sessionId}")
                    .SendAsync("SessionProgressUpdate", new
                    {
                        sessionId = sessionId,
                        allProgress = overallProgress,
                        lastScannedBarcode = scannedBarcode
                    });
            }
            catch (Exception ex)
            {
                // Log error but don't fail the scan operation
                Console.WriteLine($"Error sending progress update: {ex.Message}");
            }
        }
    }
}
