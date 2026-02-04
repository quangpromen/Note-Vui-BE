using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteVui.Application.DTOs.Sync;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Application.Exceptions;

namespace NoteVui.API.Controllers;

/// <summary>
/// API controller for offline-first synchronization operations.
/// Handles bidirectional sync between mobile clients and the server.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Performs a bidirectional sync operation for notes.
    /// </summary>
    /// <remarks>
    /// This endpoint handles both PUSH (client to server) and PULL (server to client) operations.
    /// 
    /// **Request Body:**
    /// - `LastSyncTime`: The last time the client successfully synced (null for initial/full sync)
    /// - `Changes`: List of notes created or modified by the client since last sync
    /// 
    /// **Response:**
    /// - `Upserts`: List of notes that changed on server since LastSyncTime (including deleted notes)
    /// - `ServerTime`: Current server time - store this as LastSyncTime for next sync
    /// - `Stats`: Summary of sync operation (inserted, updated, conflicts)
    /// 
    /// **Conflict Resolution:**
    /// Uses "Last Write Wins" - compares UpdatedAt timestamps to determine which version to keep.
    /// 
    /// **Important Notes:**
    /// - ClientId (GUID) is the primary identifier for sync operations
    /// - Deleted notes are included in the response so clients can remove them locally
    /// - All timestamps should be in UTC
    /// </remarks>
    /// <param name="request">The sync request containing client changes and last sync time.</param>
    /// <returns>The sync response with server changes and current server time.</returns>
    /// <response code="200">Sync completed successfully.</response>
    /// <response code="400">Invalid request (e.g., empty ClientId).</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncResponse>> Sync([FromBody] SyncRequest request)
    {
        // Validate request
        if (request.Changes.Any(c => c.ClientId == Guid.Empty))
        {
            return BadRequest(new { message = "All changes must have a non-empty ClientId." });
        }

        try
        {
            _logger.LogInformation(
                "Sync request received. LastSyncTime: {LastSyncTime}, Changes: {ChangeCount}",
                request.LastSyncTime,
                request.Changes.Count);

            var response = await _syncService.SyncAsync(request);

            _logger.LogInformation(
                "Sync completed. Inserted: {Inserted}, Updated: {Updated}, Conflicts: {Conflicts}, Returned: {Returned}",
                response.Stats.Inserted,
                response.Stats.Updated,
                response.Stats.Conflicts,
                response.Stats.ServerChangesReturned);

            return Ok(response);
        }
        catch (NoteLimitExceededException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync operation");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred during synchronization." });
        }
    }
}
