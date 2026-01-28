using System.ComponentModel.DataAnnotations;

namespace NoteVui.Application.DTOs.Sync;

/// <summary>
/// DTO representing a note for synchronization purposes.
/// Uses ClientId as the primary identifier instead of server-side NoteId.
/// </summary>
public class NoteSyncDto
{
    /// <summary>
    /// Client-generated unique identifier for the note.
    /// This is the primary key for sync operations.
    /// </summary>
    [Required]
    public Guid ClientId { get; set; }

    /// <summary>
    /// Server-side note ID. Optional - only included in server responses.
    /// </summary>
    public int? NoteId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortPreview { get; set; }

    /// <summary>
    /// Full content of the note.
    /// </summary>
    public string? FullContent { get; set; }

    public bool IsPinned { get; set; }

    /// <summary>
    /// Indicates if the note is soft-deleted.
    /// Must be included in sync to propagate deletions to clients.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the note was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the note was last updated.
    /// Used for "Last Write Wins" conflict resolution.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for sync operations.
/// Contains the last sync time and list of changes from the client.
/// </summary>
public class SyncRequest
{
    /// <summary>
    /// The last time the client successfully synced.
    /// Null for initial sync (full sync).
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// List of notes created or modified by the client since last sync.
    /// </summary>
    public List<NoteSyncDto> Changes { get; set; } = new();
}

/// <summary>
/// Response DTO for sync operations.
/// Contains server changes that need to be applied to the client.
/// </summary>
public class SyncResponse
{
    /// <summary>
    /// List of notes that were updated/created on server since last sync.
    /// Includes deleted notes so client can remove them locally.
    /// </summary>
    public List<NoteSyncDto> Upserts { get; set; } = new();

    /// <summary>
    /// Current server time in UTC.
    /// Client should store this as the LastSyncTime for the next sync request.
    /// </summary>
    public DateTime ServerTime { get; set; }

    /// <summary>
    /// Summary statistics for the sync operation.
    /// </summary>
    public SyncStats Stats { get; set; } = new();
}

/// <summary>
/// Statistics about the sync operation for debugging/logging.
/// </summary>
public class SyncStats
{
    /// <summary>
    /// Number of notes received from client (push).
    /// </summary>
    public int ClientChangesReceived { get; set; }

    /// <summary>
    /// Number of notes inserted on server.
    /// </summary>
    public int Inserted { get; set; }

    /// <summary>
    /// Number of notes updated on server.
    /// </summary>
    public int Updated { get; set; }

    /// <summary>
    /// Number of client changes ignored due to server having newer version.
    /// </summary>
    public int Conflicts { get; set; }

    /// <summary>
    /// Number of notes sent back to client (pull).
    /// </summary>
    public int ServerChangesReturned { get; set; }
}
