using Microsoft.EntityFrameworkCore;
using NoteVui.Application.DTOs.Common;
using NoteVui.Application.DTOs.Notes;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities;

namespace NoteVui.Application.Services;

public class NoteService : INoteService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public NoteService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private string GetCurrentUserId()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return userId;
    }

    public async Task<PagedResultDto<NoteDto>> GetAllAsync(NoteQueryDto query)
    {
        var userId = GetCurrentUserId();

        var queryable = _context.Notes
            .Include(n => n.NoteContent)
            .Where(n => !n.IsDeleted && n.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.ToLower();
            queryable = queryable.Where(n =>
                n.Title.ToLower().Contains(searchTerm) ||
                (n.ShortPreview != null && n.ShortPreview.ToLower().Contains(searchTerm)) ||
                (n.NoteContent != null && n.NoteContent.FullContent != null && 
                 n.NoteContent.FullContent.ToLower().Contains(searchTerm)));
        }

        var totalCount = await queryable.CountAsync();

        var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var notes = await queryable
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.UpdatedAt ?? n.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = notes.Select(MapToDto).ToList();

        return new PagedResultDto<NoteDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<NoteDto?> GetByIdAsync(int id)
    {
        var userId = GetCurrentUserId();
        
        var note = await _context.Notes
            .Include(n => n.NoteContent)
            .FirstOrDefaultAsync(n => n.NoteId == id && !n.IsDeleted && n.UserId == userId);

        return note == null ? null : MapToDto(note);
    }

    public async Task<NoteDto> CreateAsync(CreateNoteDto dto)
    {
        var userId = GetCurrentUserId();

        var note = new Note
        {
            UserId = userId,
            Title = dto.Title,
            ShortPreview = dto.ShortPreview,
            IsPinned = dto.IsPinned,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(dto.FullContent))
        {
            var noteContent = new NoteContent
            {
                NoteId = note.NoteId,
                FullContent = dto.FullContent
            };
            _context.NoteContents.Add(noteContent);
            await _context.SaveChangesAsync();
            note.NoteContent = noteContent;
        }

        return MapToDto(note);
    }

    public async Task<NoteDto?> UpdateAsync(int id, UpdateNoteDto dto)
    {
        var userId = GetCurrentUserId();

        var note = await _context.Notes
            .Include(n => n.NoteContent)
            .FirstOrDefaultAsync(n => n.NoteId == id && !n.IsDeleted && n.UserId == userId);

        if (note == null)
            return null; // Not found or access denied

        note.Title = dto.Title;
        note.ShortPreview = dto.ShortPreview;
        note.IsPinned = dto.IsPinned;
        note.UpdatedAt = DateTime.UtcNow;

        if (note.NoteContent != null)
        {
            note.NoteContent.FullContent = dto.FullContent;
        }
        else if (!string.IsNullOrWhiteSpace(dto.FullContent))
        {
            var noteContent = new NoteContent
            {
                NoteId = note.NoteId,
                FullContent = dto.FullContent
            };
            _context.NoteContents.Add(noteContent);
            note.NoteContent = noteContent;
        }

        await _context.SaveChangesAsync();
        return MapToDto(note);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var userId = GetCurrentUserId();
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == userId);

        if (note == null || note.IsDeleted)
            return false;

        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var userId = GetCurrentUserId();
        var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == userId);

        if (note == null || !note.IsDeleted)
            return false;

        note.IsDeleted = false;
        note.DeletedAt = null;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static NoteDto MapToDto(Note note)
    {
        return new NoteDto
        {
            NoteId = note.NoteId,
            UserId = note.UserId,
            Title = note.Title,
            ShortPreview = note.ShortPreview,
            FullContent = note.NoteContent?.FullContent,
            IsPinned = note.IsPinned,
            IsDeleted = note.IsDeleted,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}
