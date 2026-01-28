using Microsoft.EntityFrameworkCore;
using NoteVui.Application.DTOs.Sync;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities;

namespace NoteVui.Application.Services;

/// <summary>
/// Service for handling offline-first synchronization between mobile clients and the server.
/// Implements bidirectional sync with "Last Write Wins" conflict resolution.
/// </summary>
public class SyncService : ISyncService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SyncService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc/>
    public async Task<SyncResponse> SyncAsync(SyncRequest request)
    {
        var userId = GetCurrentUserId();
        var syncTime = DateTime.UtcNow;
        var stats = new SyncStats
        {
            ClientChangesReceived = request.Changes.Count
        };

        // PUSH: Process client changes
        await ProcessPushAsync(userId, request.Changes, stats);

        // PULL: Get server changes since last sync
        var serverChanges = await GetPullChangesAsync(userId, request.LastSyncTime);
        stats.ServerChangesReturned = serverChanges.Count;

        return new SyncResponse
        {
            Upserts = serverChanges,
            ServerTime = syncTime,
            Stats = stats
        };
    }

    /// <summary>
    /// Processes client changes (PUSH operation).
    /// Handles inserts and updates using "Last Write Wins" conflict resolution.
    /// </summary>
    private async Task ProcessPushAsync(string userId, List<NoteSyncDto> clientChanges, SyncStats stats)
    {
        if (clientChanges.Count == 0)
            return;

        // Get all ClientIds from the request
        var clientIds = clientChanges
            .Where(c => c.ClientId != Guid.Empty)
            .Select(c => c.ClientId)
            .ToList();

        // Fetch existing notes for this user with matching ClientIds in one query
        var existingNotes = await _context.Notes
            .Include(n => n.NoteContent)
            .Where(n => n.UserId == userId && clientIds.Contains(n.ClientId))
            .ToDictionaryAsync(n => n.ClientId);

        foreach (var clientNote in clientChanges)
        {
            // Validate ClientId
            if (clientNote.ClientId == Guid.Empty)
            {
                // Skip invalid entries
                continue;
            }

            if (existingNotes.TryGetValue(clientNote.ClientId, out var serverNote))
            {
                // Case B: Note exists - apply "Last Write Wins"
                if (clientNote.UpdatedAt > serverNote.UpdatedAt)
                {
                    // Client version is newer - update server
                    UpdateNoteFromDto(serverNote, clientNote);
                    stats.Updated++;
                }
                else
                {
                    // Server version is newer - ignore client change
                    stats.Conflicts++;
                }
            }
            else
            {
                // Case A: New note created offline - insert
                var newNote = CreateNoteFromDto(userId, clientNote);
                _context.Notes.Add(newNote);
                stats.Inserted++;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets server changes since last sync (PULL operation).
    /// Includes deleted notes so clients can remove them locally.
    /// </summary>
    private async Task<List<NoteSyncDto>> GetPullChangesAsync(string userId, DateTime? lastSyncTime)
    {
        var query = _context.Notes
            .AsNoTracking()
            .Include(n => n.NoteContent)
            .Where(n => n.UserId == userId);

        // If lastSyncTime is provided, only get changes since then
        // Otherwise, return all notes (full sync)
        if (lastSyncTime.HasValue)
        {
            query = query.Where(n => n.UpdatedAt > lastSyncTime.Value);
        }

        // IMPORTANT: Do NOT filter out IsDeleted - we need to send deleted notes
        // so the client knows to remove them locally

        var notes = await query
            .OrderBy(n => n.UpdatedAt)
            .ToListAsync();

        return notes.Select(MapToSyncDto).ToList();
    }

    /// <summary>
    /// Creates a new Note entity from a sync DTO.
    /// </summary>
    private static Note CreateNoteFromDto(string userId, NoteSyncDto dto)
    {
        var note = new Note
        {
            ClientId = dto.ClientId,
            UserId = userId,
            Title = dto.Title,
            ShortPreview = dto.ShortPreview,
            IsPinned = dto.IsPinned,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.IsDeleted ? DateTime.UtcNow : null,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };

        // Add NoteContent if provided
        if (!string.IsNullOrWhiteSpace(dto.FullContent))
        {
            note.NoteContent = new NoteContent
            {
                FullContent = dto.FullContent
            };
        }

        return note;
    }

    /// <summary>
    /// Updates an existing Note entity from a sync DTO.
    /// </summary>
    private static void UpdateNoteFromDto(Note note, NoteSyncDto dto)
    {
        note.Title = dto.Title;
        note.ShortPreview = dto.ShortPreview;
        note.IsPinned = dto.IsPinned;
        note.IsDeleted = dto.IsDeleted;
        note.DeletedAt = dto.IsDeleted ? (note.DeletedAt ?? DateTime.UtcNow) : null;
        note.UpdatedAt = dto.UpdatedAt;

        // Update NoteContent
        if (note.NoteContent != null)
        {
            note.NoteContent.FullContent = dto.FullContent;
        }
        else if (!string.IsNullOrWhiteSpace(dto.FullContent))
        {
            // Ensure navigation property is linked correctly
            note.NoteContent = new NoteContent
            {
                NoteId = note.NoteId,
                FullContent = dto.FullContent
            };
        }
    }

    /// <summary>
    /// Maps a Note entity to a sync DTO.
    /// </summary>
    private static NoteSyncDto MapToSyncDto(Note note)
    {
        return new NoteSyncDto
        {
            ClientId = note.ClientId,
            NoteId = note.NoteId,
            Title = note.Title,
            ShortPreview = note.ShortPreview,
            FullContent = note.NoteContent?.FullContent,
            IsPinned = note.IsPinned,
            IsDeleted = note.IsDeleted,
            CreatedAt = note.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = note.UpdatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    private string GetCurrentUserId()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return userId;
    }
}
