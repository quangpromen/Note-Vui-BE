using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using NoteVui.API.Extensions;
using NoteVui.Application.DTOs.Notes;
using NoteVui.Application.Services.Interfaces;

namespace NoteVui.API.Controllers;

/// <summary>
/// API Controller for Note operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.ApiLimiter)]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    /// <summary>
    /// Gets all notes with optional search and pagination.
    /// </summary>
    /// <param name="query">Query parameters for search and pagination.</param>
    /// <returns>Paginated list of notes.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] NoteQueryDto query)
    {
        var result = await _noteService.GetAllAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific note by ID.
    /// </summary>
    /// <param name="id">The note ID.</param>
    /// <returns>The note or 404 if not found.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var note = await _noteService.GetByIdAsync(id);
        if (note == null)
            return NotFound(new { message = $"Note with ID {id} not found." });

        return Ok(note);
    }

    /// <summary>
    /// Creates a new note.
    /// </summary>
    /// <param name="dto">The note creation data.</param>
    /// <returns>The created note.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // userId is handled by Service from ICurrentUserService
        var note = await _noteService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = note.NoteId }, note);
    }

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    /// <param name="id">The note ID to update.</param>
    /// <param name="dto">The updated note data.</param>
    /// <returns>The updated note or 404 if not found.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNoteDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var note = await _noteService.UpdateAsync(id, dto);
        if (note == null)
            return NotFound(new { message = $"Note with ID {id} not found." });

        return Ok(note);
    }

    /// <summary>
    /// Soft deletes a note.
    /// </summary>
    /// <param name="id">The note ID to delete.</param>
    /// <returns>204 No Content if deleted, 404 if not found.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _noteService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = $"Note with ID {id} not found or already deleted." });

        return NoContent();
    }

    /// <summary>
    /// Restores a soft-deleted note.
    /// </summary>
    /// <param name="id">The note ID to restore.</param>
    /// <returns>200 OK with message if restored, 404 if not found.</returns>
    [HttpPatch("{id}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var result = await _noteService.RestoreAsync(id);
        if (!result)
            return NotFound(new { message = $"Note with ID {id} not found or not deleted." });

        return Ok(new { message = $"Note with ID {id} has been restored." });
    }
}
