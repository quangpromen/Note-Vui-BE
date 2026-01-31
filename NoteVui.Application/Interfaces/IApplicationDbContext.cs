using Microsoft.EntityFrameworkCore;
using NoteVui.Domain.Entities;

namespace NoteVui.Application.Interfaces;

/// <summary>
/// Interface for the application database context.
/// This allows the Application layer to depend on an abstraction rather than the concrete Infrastructure implementation.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Note> Notes { get; set; }
    DbSet<NoteContent> NoteContents { get; set; }
    DbSet<Plan> Plans { get; set; }
    DbSet<Subscription> Subscriptions { get; set; }
    DbSet<AiProvider> AiProviders { get; set; }
    DbSet<AiModel> AiModels { get; set; }
    DbSet<AiUsageLog> AiUsageLogs { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
