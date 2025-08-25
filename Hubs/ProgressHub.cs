using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace ShipmentFinishGood.Hubs
{
    [Authorize]
    public class ProgressHub : Hub
    {
        private static readonly Dictionary<string, string> _userSessions = new();
        private static readonly Dictionary<string, HashSet<string>> _sessionUsers = new();

        public async Task JoinSessionGroup(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
            
            // Track user-session mapping
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            _userSessions[userName] = sessionId;
            
            if (!_sessionUsers.ContainsKey(sessionId))
                _sessionUsers[sessionId] = new HashSet<string>();
            
            _sessionUsers[sessionId].Add(userName);
            
            // Notify other users about new user joining
            await Clients.Group($"Session_{sessionId}").SendAsync("UserJoinedSession", userName);
        }

        public async Task LeaveSessionGroup(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
            
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            _userSessions.Remove(userName);
            
            if (_sessionUsers.ContainsKey(sessionId))
            {
                _sessionUsers[sessionId].Remove(userName);
                if (!_sessionUsers[sessionId].Any())
                    _sessionUsers.Remove(sessionId);
            }
            
            // Notify other users about user leaving
            await Clients.Group($"Session_{sessionId}").SendAsync("UserLeftSession", userName);
        }

        public async Task NotifyBarcodeScanned(string sessionId, string barcode, string scannedBy)
        {
            await Clients.Group($"Session_{sessionId}").SendAsync("BarcodeScanned", new 
            { 
                sessionId, 
                barcode, 
                scannedBy, 
                timestamp = DateTime.Now 
            });
        }

        public async Task NotifySessionLocked(string sessionId, string lockedBy, string qrIdentity)
        {
            await Clients.Group($"Session_{sessionId}").SendAsync("SessionLocked", new 
            { 
                sessionId, 
                lockedBy, 
                qrIdentity, 
                timestamp = DateTime.Now 
            });
        }

        public async Task NotifySessionUnlocked(string sessionId, string unlockedBy)
        {
            await Clients.Group($"Session_{sessionId}").SendAsync("SessionUnlocked", new 
            { 
                sessionId, 
                unlockedBy, 
                timestamp = DateTime.Now 
            });
        }

        public async Task NotifyProgressUpdate(string sessionId, object progressData)
        {
            await Clients.Group($"Session_{sessionId}").SendAsync("ProgressUpdated", progressData);
        }

        public async Task BroadcastBarcodeUpdate(string sessionId, string barcode, string action, string by)
        {
            await Clients.Group($"Session_{sessionId}").SendAsync("BarcodeStateChanged", new 
            { 
                sessionId, 
                barcode, 
                action, // "scanned", "validated", "invalidated"
                by, 
                timestamp = DateTime.Now 
            });
        }

        public async Task SyncSessionData(string sessionId)
        {
            // Force sync session data for all connected clients
            await Clients.Group($"Session_{sessionId}").SendAsync("ForceSessionSync", sessionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            
            // Clean up user tracking
            if (_userSessions.ContainsKey(userName))
            {
                var sessionId = _userSessions[userName];
                _userSessions.Remove(userName);
                
                if (_sessionUsers.ContainsKey(sessionId))
                {
                    _sessionUsers[sessionId].Remove(userName);
                    if (!_sessionUsers[sessionId].Any())
                        _sessionUsers.Remove(sessionId);
                        
                    // Notify remaining users
                    await Clients.Group($"Session_{sessionId}").SendAsync("UserLeftSession", userName);
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
