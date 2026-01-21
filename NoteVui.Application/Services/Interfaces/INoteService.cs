using NoteVui.Application.DTOs.Common;
using NoteVui.Application.DTOs.Notes;

namespace NoteVui.Application.Services.Interfaces;

/// <summary>
/// Interface for Note service operations.
/// </summary>
public interface INoteService
{
    /// <summary>
    /// Gets all non-deleted notes with optional search and pagination.
    /// </summary>
    /// <param name="query">Query parameters for search and pagination.</param>
    /// <returns>Paginated list of notes.</returns>
    Task<PagedResultDto<NoteDto>> GetAllAsync(NoteQueryDto query);

    /// <summary>
    /// Gets a note by its ID.
    /// </summary>
    /// <param name="id">The note ID.</param>
    /// <returns>The note DTO or null if not found.</returns>
    Task<NoteDto?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new note.
    /// </summary>
    /// <param name="userId">The user ID creating the note.</param>
    /// <param name="dto">The note creation data.</param>
    /// <returns>The created note DTO.</returns>
    Task<NoteDto> CreateAsync(CreateNoteDto dto);

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    /// <param name="id">The note ID to update.</param>
    /// <param name="dto">The updated note data.</param>
    /// <returns>The updated note DTO or null if not found.</returns>
    Task<NoteDto?> UpdateAsync(int id, UpdateNoteDto dto);

    /// <summary>
    /// Soft deletes a note by setting IsDeleted to true.
    /// </summary>
    /// <param name="id">The note ID to delete.</param>
    /// <returns>True if the note was found and deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Restores a soft-deleted note by setting IsDeleted to false.
    /// </summary>
    /// <param name="id">The note ID to restore.</param>
    /// <returns>True if the note was found and restored, false otherwise.</returns>
    Task<bool> RestoreAsync(int id);
}
