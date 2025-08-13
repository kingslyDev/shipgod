using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Services
{
    public class ScanningService : IScanningService
    {
        private readonly AppDbContext _context;

        public ScanningService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ScanSessionSummaryDto>> GetActiveSessionsAsync()
        {
            // First, try to find sessions with QR_GENERATED status
            var sessions = await _context.UploadSessions
                .Include(s => s.POMasters)
                .Where(s => s.Status == "QR_GENERATED" && !string.IsNullOrEmpty(s.IdentityQRCode))
                .OrderByDescending(s => s.UploadDate)
                .ToListAsync();

            // If no QR_GENERATED sessions, look for VALIDATED or any session with QR Identity
            if (!sessions.Any())
            {
                sessions = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => !string.IsNullOrEmpty(s.IdentityQRCode) && s.POMasters.Any())
                    .OrderByDescending(s => s.UploadDate)
                    .ToListAsync();
            }

            // If still no sessions with QR, check for VALIDATED sessions and auto-generate QR if needed
            if (!sessions.Any())
            {
                var validatedSessions = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => s.Status == "VALIDATED" && s.POMasters.Any())
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
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Re-fetch sessions after generating QR codes
                sessions = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => s.Status == "QR_GENERATED" && !string.IsNullOrEmpty(s.IdentityQRCode))
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
            }

            var barcodes = GenerateBarcodeList(session);
            var scannedBarcodes = await GetScannedBarcodesAsync(sessionId);
            
            return new ScanSessionDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                QRIdentity = session.IdentityQRCode,
                ShipmentType = session.ShipmentType ?? "LOOSE",
                ShipmentDate = session.ShipmentDate,
                TotalBoxes = session.TotalBoxes,
                TotalBarcodes = barcodes.Count,
                ScannedCount = scannedBarcodes.Count,
                BarcodeList = barcodes,
                ScannedBarcodes = scannedBarcodes,
                IsMasterScanned = await IsMasterQRScannedAsync(sessionId),
                CanComplete = await CanCompleteScanAsync(sessionId)
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

            // Check if barcode already scanned
            var existingScan = await _context.ScanningActivities
                .FirstOrDefaultAsync(sa => sa.BarcodeValue == barcode && sa.Action == "SCAN_BOX");

            if (existingScan != null)
                return Result<ScanResultDto>.Failure("Box already scanned");

            // Validate if barcode exists in our generated list
            var validBarcodes = GenerateBarcodesForSession(session);
            if (!validBarcodes.Contains(barcode))
                return Result<ScanResultDto>.Failure("Invalid barcode");

            // Record the scan
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

            var totalBarcodes = GenerateBarcodesForSession(session).Count;
            var scannedCount = await _context.ScanningActivities
                .CountAsync(sa => sa.BarcodeValue.StartsWith(session.IdentityQRCode!) && sa.Action == "SCAN_BOX");

            var masterScanned = await IsMasterQRScannedAsync(sessionId);

            return new ScanProgressDto
            {
                SessionId = sessionId,
                TotalBarcodes = totalBarcodes,
                ScannedCount = scannedCount,
                ProgressPercentage = totalBarcodes > 0 ? (double)scannedCount / totalBarcodes * 100 : 0,
                IsMasterScanned = masterScanned,
                LastScanTime = await GetLastScanTimeAsync(sessionId),
                CanComplete = scannedCount == totalBarcodes && masterScanned
            };
        }

        public async Task<List<BarcodeItemDto>> GetBarcodeListAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return new List<BarcodeItemDto>();

            var barcodes = GenerateBarcodeList(session);
            var scannedBarcodes = await GetScannedBarcodesAsync(sessionId);

            return barcodes.Select(b => new BarcodeItemDto
            {
                BarcodeValue = b.BarcodeValue,
                ModelProduct = b.ModelProduct,
                BoxNumber = b.BoxNumber,
                IsScanned = scannedBarcodes.Contains(b.BarcodeValue),
                ScannedTime = GetScanTime(b.BarcodeValue, scannedBarcodes)
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
    }
}
