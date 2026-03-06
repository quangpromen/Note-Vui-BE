using Microsoft.EntityFrameworkCore;
using NoteVui.Application.Interfaces;
using NoteVui.Domain.Entities;
using NoteVui.Domain.Entities.Identity;
using NoteVui.Domain.Entities.Membership;
using NoteVui.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace NoteVui.Infrastructure.Persistence;

/// <summary>
/// The main database context for the NoteVui application.
/// Implements Code First approach with Fluent API configuration.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<AppUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    #region DbSets

    public DbSet<Plan> Plans { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<NoteContent> NoteContents { get; set; } = null!;
    public DbSet<AiProvider> AiProviders { get; set; } = null!;
    public DbSet<AiModel> AiModels { get; set; } = null!;
    public DbSet<AiUsageLog> AiUsageLogs { get; set; } = null!;

    // Membership System
    public DbSet<UserSubscription> UserSubscriptions { get; set; } = null!;
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
    public DbSet<SubscriptionRequest> SubscriptionRequests { get; set; } = null!;

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customize Identity table names (remove AspNet prefix)
        modelBuilder.Entity<AppUser>().ToTable("Users");
        modelBuilder.Entity<IdentityRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        // Apply configurations
        ConfigurePlan(modelBuilder);
        ConfigureSubscription(modelBuilder);
        ConfigureNote(modelBuilder);
        ConfigureNoteContent(modelBuilder);
        ConfigureAiProvider(modelBuilder);
        ConfigureAiModel(modelBuilder);
        ConfigureAiUsageLog(modelBuilder);

        // Membership System
        ConfigureUserSubscription(modelBuilder);
        ConfigurePaymentTransaction(modelBuilder);
        ConfigureSubscriptionRequest(modelBuilder);

        // Seed data
        // SeedUsers(modelBuilder); // Removed for security
        SeedPlans(modelBuilder);
        SeedAiProviders(modelBuilder);
        SeedAiModels(modelBuilder);
    }

    #region Entity Configurations

    private static void ConfigurePlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("plans");
            entity.HasKey(e => e.PlanId);
            entity.Property(e => e.PlanCode).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.PlanName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MaxNotes).HasDefaultValue(50);
            entity.Property(e => e.DailyAiLimit).HasDefaultValue(5);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)").HasDefaultValue(0m);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.PlanCode, "IX_Plans_PlanCode").IsUnique();
        });
    }

    private static void ConfigureSubscription(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(e => e.SubId);
            entity.Property(e => e.StartDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.HasOne(e => e.User).WithMany(u => u.Subscriptions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Plan).WithMany(p => p.Subscriptions).HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId, "IX_Subscriptions_User");
        });
    }

    private static void ConfigureNote(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("notes");
            entity.HasKey(e => e.NoteId);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ShortPreview).HasMaxLength(500);
            entity.Property(e => e.IsPinned).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasOne(e => e.User).WithMany(u => u.Notes).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.IsDeleted }, "IX_Notes_User_Active");
            entity.HasIndex(e => new { e.UserId, e.IsPinned }, "IX_Notes_User_Pinned");
            // Index for sync: lookup by ClientId per user
            entity.HasIndex(e => new { e.UserId, e.ClientId }, "IX_Notes_User_ClientId");
            // Index for sync pull: query by UpdatedAt per user
            entity.HasIndex(e => new { e.UserId, e.UpdatedAt }, "IX_Notes_User_UpdatedAt");
        });
    }

    private static void ConfigureNoteContent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NoteContent>(entity =>
        {
            entity.ToTable("note_contents");
            entity.HasKey(e => e.NoteId);
            entity.Property(e => e.FullContent).HasColumnType("nvarchar(max)");
            entity.HasOne(e => e.Note).WithOne(n => n.NoteContent).HasForeignKey<NoteContent>(e => e.NoteId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAiProvider(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiProvider>(entity =>
        {
            entity.ToTable("ai_providers");
            entity.HasKey(e => e.ProviderId);
            entity.Property(e => e.ProviderCode).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.ProviderName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.ProviderCode, "IX_AiProviders_ProviderCode").IsUnique();
        });
    }

    private static void ConfigureAiModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiModel>(entity =>
        {
            entity.ToTable("ai_models");
            entity.HasKey(e => e.ModelId);
            entity.Property(e => e.ModelCode).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.CostInput).HasColumnType("decimal(18, 10)");
            entity.Property(e => e.CostOutput).HasColumnType("decimal(18, 10)");
            entity.HasOne(e => e.Provider).WithMany(p => p.AiModels).HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ProviderId, "IX_AiModels_Provider");
            entity.HasIndex(e => new { e.ProviderId, e.ModelCode }, "IX_AiModels_Provider_ModelCode").IsUnique();
        });
    }

    private static void ConfigureAiUsageLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiUsageLog>(entity =>
        {
            entity.ToTable("ai_usage_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.ActionType).HasConversion<int>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Index for quota queries: count by UserId and CreatedAt (for daily limits)
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_AiUsageLogs_User_CreatedAt");
            // Index for looking up by NoteId
            entity.HasIndex(e => e.NoteId, "IX_AiUsageLogs_NoteId").HasFilter("[NoteId] IS NOT NULL");
        });
    }

    private static void ConfigureUserSubscription(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.ToTable("user_subscriptions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PlanType).HasConversion<int>().HasDefaultValue(PlanType.Free);
            entity.Property(e => e.Status).HasConversion<int>().HasDefaultValue(SubscriptionStatus.Active);
            entity.Property(e => e.StartDate).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.IsAutoRenew).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // One-to-One relationship: AppUser has one UserSubscription
            entity.HasOne(e => e.User)
                  .WithOne(u => u.UserSubscription)
                  .HasForeignKey<UserSubscription>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId, "IX_UserSubscriptions_UserId").IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Status, e.EndDate }, "IX_UserSubscriptions_User_Active");
        });
    }

    private static void ConfigurePaymentTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("payment_transactions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(e => e.TransactionCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<int>().HasDefaultValue(TransactionStatus.Pending);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // One-to-Many relationship: AppUser has many PaymentTransactions
            entity.HasOne(e => e.User)
                  .WithMany(u => u.PaymentTransactions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TransactionCode, "IX_PaymentTransactions_TransactionCode").IsUnique();
            entity.HasIndex(e => e.UserId, "IX_PaymentTransactions_UserId");
            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_PaymentTransactions_User_Status");
        });
    }

    private static void ConfigureSubscriptionRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionRequest>(entity =>
        {
            entity.ToTable("subscription_requests");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PlanType).HasConversion<int>().HasDefaultValue(PlanType.Free);
            entity.Property(e => e.Status).HasConversion<int>().HasDefaultValue(RequestStatus.Pending);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.AdminNote).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProcessedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ProcessedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.UserId, "IX_SubscriptionRequests_UserId");
            entity.HasIndex(e => e.Status, "IX_SubscriptionRequests_Status");
            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_SubscriptionRequests_User_Status");
        });
    }

    #endregion

    #region Data Seeding

    /* 
    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        var hasher = new PasswordHasher<AppUser>();
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = "11111111-1111-1111-1111-111111111111",
                UserName = "test@notevui.com",
                NormalizedUserName = "TEST@NOTEVUI.COM",
                Email = "test@notevui.com",
                NormalizedEmail = "TEST@NOTEVUI.COM",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null!, "Test@123"),
                SecurityStamp = "2E2B8BB1-8BE4-4E40-8C8E-8E8E8E8E8E8E", // Consistent stamp
                FullName = "Test User"
            }
        );
    }
    */

    private static void SeedPlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>().HasData(
            new Plan { PlanId = 1, PlanCode = "free", PlanName = "Free Plan", MaxNotes = 50, DailyAiLimit = 5, Price = 0m, IsActive = true },
            new Plan { PlanId = 2, PlanCode = "pro", PlanName = "Pro Plan", MaxNotes = 500, DailyAiLimit = 50, Price = 9.99m, IsActive = true },
            new Plan { PlanId = 3, PlanCode = "premium", PlanName = "Premium Plan", MaxNotes = -1, DailyAiLimit = -1, Price = 19.99m, IsActive = true }
        );
    }

    private static void SeedAiProviders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiProvider>().HasData(
            new AiProvider { ProviderId = 1, ProviderCode = "openai", ProviderName = "OpenAI", IsActive = true },
            new AiProvider { ProviderId = 2, ProviderCode = "google", ProviderName = "Google AI", IsActive = true },
            new AiProvider { ProviderId = 3, ProviderCode = "anthropic", ProviderName = "Anthropic", IsActive = true }
        );
    }

    private static void SeedAiModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiModel>().HasData(
            new AiModel { ModelId = 1, ProviderId = 1, ModelCode = "gpt-4o", CostInput = 0.0000025m, CostOutput = 0.00001m },
            new AiModel { ModelId = 2, ProviderId = 1, ModelCode = "gpt-4o-mini", CostInput = 0.00000015m, CostOutput = 0.0000006m },
            new AiModel { ModelId = 3, ProviderId = 1, ModelCode = "gpt-3.5-turbo", CostInput = 0.0000005m, CostOutput = 0.0000015m },
            new AiModel { ModelId = 4, ProviderId = 2, ModelCode = "gemini-1.5-pro", CostInput = 0.00000125m, CostOutput = 0.000005m },
            new AiModel { ModelId = 5, ProviderId = 2, ModelCode = "gemini-1.5-flash", CostInput = 0.000000075m, CostOutput = 0.0000003m },
            new AiModel { ModelId = 6, ProviderId = 3, ModelCode = "claude-3-5-sonnet", CostInput = 0.000003m, CostOutput = 0.000015m },
            new AiModel { ModelId = 7, ProviderId = 3, ModelCode = "claude-3-haiku", CostInput = 0.00000025m, CostOutput = 0.00000125m }
        );
    }

    #endregion
}
