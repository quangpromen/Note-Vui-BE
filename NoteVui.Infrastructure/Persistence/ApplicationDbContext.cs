using Microsoft.EntityFrameworkCore;
using NoteVui.Domain.Entities.Common;

namespace NoteVui.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuration will be added here once entities are defined
    }
}
