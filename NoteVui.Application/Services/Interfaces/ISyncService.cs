using NoteVui.Application.DTOs.Sync;

namespace NoteVui.Application.Services.Interfaces;

/// <summary>
/// Interface for synchronization service operations.
/// Handles offline-first sync between mobile clients and the server.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Performs a bidirectional sync operation.
    /// PUSH: Processes client changes and applies them to the server.
    /// PULL: Returns server changes that occurred since last sync.
    /// Uses "Last Write Wins" conflict resolution based on UpdatedAt timestamps.
    /// </summary>
    /// <param name="request">The sync request containing client changes and last sync time.</param>
    /// <returns>The sync response with server changes and current server time.</returns>
    Task<SyncResponse> SyncAsync(SyncRequest request);
}
