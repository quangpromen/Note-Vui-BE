using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoteVui.Application.DTOs.Common;
using NoteVui.Application.DTOs.Subscription;
using NoteVui.Application.Interfaces;
using NoteVui.Application.Services.Interfaces;
using NoteVui.Domain.Entities.Identity;
using NoteVui.Domain.Entities.Membership;
using NoteVui.Domain.Enums;

namespace NoteVui.Application.Services;

/// <summary>
/// Service implementation for managing subscription upgrade requests.
/// Handles User-side (create/view/cancel) and Admin-side (list/approve/reject) operations.
/// </summary>
public class SubscriptionRequestService : ISubscriptionRequestService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMailService _mailService;

    public SubscriptionRequestService(
        IApplicationDbContext context,
        UserManager<AppUser> userManager,
        IMailService mailService)
    {
        _context = context;
        _userManager = userManager;
        _mailService = mailService;
    }

    // ==========================================
    // USER OPERATIONS
    // ==========================================

    /// <inheritdoc />
    public async Task<SubscriptionRequestResponseDto> CreateRequestAsync(string userId, CreateSubscriptionRequestDto request)
    {
        // Validate: User must not have a pending request already
        var hasPending = await _context.SubscriptionRequests
            .AnyAsync(r => r.UserId == userId && r.Status == RequestStatus.Pending);

        if (hasPending)
        {
            throw new InvalidOperationException("Bạn đã có một yêu cầu nâng cấp đang chờ xử lý. Vui lòng đợi Admin phản hồi trước khi gửi yêu cầu mới.");
        }

        // Validate: PlanType must be a premium plan (not Free)
        if (request.PlanType == PlanType.Free)
        {
            throw new InvalidOperationException("Không thể yêu cầu nâng cấp lên gói Free.");
        }

        var subscriptionRequest = new SubscriptionRequest
        {
            UserId = userId,
            PlanType = request.PlanType,
            Status = RequestStatus.Pending,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionRequests.Add(subscriptionRequest);
        await _context.SaveChangesAsync();

        // Send confirmation email to user & notification to admins
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await SendRequestConfirmationEmailAsync(user, request.PlanType);
            await SendNewRequestNotificationToAdminsAsync(user, request.PlanType, request.Note);
        }

        return MapToUserResponse(subscriptionRequest);
    }

    /// <inheritdoc />
    public async Task<List<SubscriptionRequestResponseDto>> GetUserRequestsAsync(string userId)
    {
        var requests = await _context.SubscriptionRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToUserResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> CancelRequestAsync(string userId, int requestId)
    {
        var request = await _context.SubscriptionRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId);

        if (request == null)
        {
            throw new InvalidOperationException("Không tìm thấy yêu cầu nâng cấp.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ có thể hủy yêu cầu đang ở trạng thái chờ xử lý.");
        }

        request.Status = RequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    // ==========================================
    // ADMIN OPERATIONS
    // ==========================================

    /// <inheritdoc />
    public async Task<PagedResultDto<AdminSubscriptionRequestDto>> GetAdminRequestsAsync(
        RequestStatus? status, string? search, int page, int pageSize)
    {
        var query = _context.SubscriptionRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.ProcessedByUser)
            .AsQueryable();

        // Filter by status
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        // Search by user email or name
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(r =>
                (r.User.Email != null && r.User.Email.ToLower().Contains(searchLower)) ||
                r.User.FullName.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminSubscriptionRequestDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserEmail = r.User.Email ?? string.Empty,
                UserFullName = r.User.FullName,
                UserAvatarUrl = r.User.AvatarUrl,
                PlanType = r.PlanType.ToString(),
                PlanName = GetPlanDisplayName(r.PlanType),
                Status = r.Status.ToString(),
                Note = r.Note,
                AdminNote = r.AdminNote,
                ProcessedByUserName = r.ProcessedByUser != null ? r.ProcessedByUser.FullName : null,
                ProcessedAt = r.ProcessedAt,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<AdminSubscriptionRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<AdminSubscriptionRequestDto> ApproveRequestAsync(int requestId, string adminUserId)
    {
        // Use a transaction to ensure atomicity
        var request = await _context.SubscriptionRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            throw new InvalidOperationException("Không tìm thấy yêu cầu nâng cấp.");
        }

        // Concurrency check: Only process if still Pending
        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó.");
        }

        var now = DateTime.UtcNow;

        // 1. Update the request status
        request.Status = RequestStatus.Approved;
        request.ProcessedByUserId = adminUserId;
        request.ProcessedAt = now;
        request.UpdatedAt = now;

        // 2. Calculate subscription end date based on plan type
        var endDate = request.PlanType switch
        {
            PlanType.PremiumMonthly => now.AddMonths(1),
            PlanType.PremiumYearly => now.AddYears(1),
            _ => now.AddMonths(1)
        };

        // 3. Update or create UserSubscription
        var subscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == request.UserId);

        if (subscription == null)
        {
            subscription = new UserSubscription
            {
                UserId = request.UserId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                EndDate = endDate,
                IsAutoRenew = false,
                CreatedAt = now
            };
            _context.UserSubscriptions.Add(subscription);
        }
        else
        {
            subscription.PlanType = request.PlanType;
            subscription.Status = SubscriptionStatus.Active;
            subscription.StartDate = now;
            subscription.EndDate = endDate;
            subscription.UpdatedAt = now;
        }

        // 4. Save all changes in one transaction (EF Core SaveChanges is transactional)
        await _context.SaveChangesAsync();

        // 5. Send email notification to user
        await SendApprovalEmailAsync(request.User, request.PlanType, now, endDate);

        // 6. Return updated request info
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        return MapToAdminResponse(request, adminUser);
    }

    /// <inheritdoc />
    public async Task<AdminSubscriptionRequestDto> RejectRequestAsync(int requestId, string adminUserId, string? reason)
    {
        var request = await _context.SubscriptionRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            throw new InvalidOperationException("Không tìm thấy yêu cầu nâng cấp.");
        }

        // Concurrency check: Only process if still Pending
        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó.");
        }

        var now = DateTime.UtcNow;

        request.Status = RequestStatus.Rejected;
        request.AdminNote = reason;
        request.ProcessedByUserId = adminUserId;
        request.ProcessedAt = now;
        request.UpdatedAt = now;

        await _context.SaveChangesAsync();

        // Send rejection email to user
        await SendRejectionEmailAsync(request.User, request.PlanType, reason);

        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        return MapToAdminResponse(request, adminUser);
    }

    // ==========================================
    // PRIVATE HELPERS
    // ==========================================

    private static string GetPlanDisplayName(PlanType planType) => planType switch
    {
        PlanType.Free => "Free",
        PlanType.PremiumMonthly => "Premium (Tháng)",
        PlanType.PremiumYearly => "Premium (Năm)",
        _ => "Free"
    };

    private static SubscriptionRequestResponseDto MapToUserResponse(SubscriptionRequest request)
    {
        return new SubscriptionRequestResponseDto
        {
            Id = request.Id,
            PlanType = request.PlanType.ToString(),
            PlanName = GetPlanDisplayName(request.PlanType),
            Status = request.Status.ToString(),
            Note = request.Note,
            AdminNote = request.AdminNote,
            CreatedAt = request.CreatedAt,
            ProcessedAt = request.ProcessedAt
        };
    }

    private static AdminSubscriptionRequestDto MapToAdminResponse(SubscriptionRequest request, AppUser? adminUser)
    {
        return new AdminSubscriptionRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            UserEmail = request.User?.Email ?? string.Empty,
            UserFullName = request.User?.FullName ?? string.Empty,
            UserAvatarUrl = request.User?.AvatarUrl,
            PlanType = request.PlanType.ToString(),
            PlanName = GetPlanDisplayName(request.PlanType),
            Status = request.Status.ToString(),
            Note = request.Note,
            AdminNote = request.AdminNote,
            ProcessedByUserName = adminUser?.FullName,
            ProcessedAt = request.ProcessedAt,
            CreatedAt = request.CreatedAt
        };
    }

    private async Task SendApprovalEmailAsync(AppUser user, PlanType planType, DateTime startDate, DateTime endDate)
    {
        if (user.Email == null) return;

        var planNameStr = GetPlanDisplayName(planType);
        var emailSubject = "🎉 Yêu cầu nâng cấp VIP đã được phê duyệt - NoteVui";
        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #4CAF50; padding: 20px; text-align: center; color: white;'>
                    <h2 style='margin: 0;'>🎉 Yêu cầu nâng cấp VIP đã được duyệt!</h2>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Xin chào <strong>{user.FullName}</strong>,</p>
                    <p>Chúc mừng bạn! Yêu cầu nâng cấp gói <strong>{planNameStr}</strong> của bạn đã được quản trị viên phê duyệt thành công.</p>
                    <table style='width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 20px;'>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Loại gói:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{planNameStr}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Ngày hiệu lực:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{startDate.AddHours(7):dd/MM/yyyy HH:mm} (Giờ VN)</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Ngày hết hạn:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{endDate.AddHours(7):dd/MM/yyyy HH:mm} (Giờ VN)</td>
                        </tr>
                    </table>
                    <p>Bạn đã có thể tận hưởng toàn bộ các quyền lợi cao cấp của NoteVui ngay bây giờ!</p>
                    <p>Cảm ơn bạn đã tin tưởng và sử dụng NoteVui!</p>
                    <br/>
                    <p>Trân trọng,<br/><strong>Đội ngũ NoteVui</strong></p>
                </div>
                <div style='background-color: #f9f9f9; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
                    <p style='margin: 0;'>Đây là email tự động từ hệ thống NoteVui. Vui lòng không trả lời qua email này.</p>
                </div>
            </div>";

        try
        {
            await _mailService.SendEmailAsync(user.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi gửi email phê duyệt VIP: {ex.Message}");
        }
    }

    private async Task SendRejectionEmailAsync(AppUser user, PlanType planType, string? reason)
    {
        if (user.Email == null) return;

        var planNameStr = GetPlanDisplayName(planType);
        var reasonHtml = !string.IsNullOrWhiteSpace(reason)
            ? $"<p><strong>Lý do:</strong> {reason}</p>"
            : "<p><strong>Lý do:</strong> Không có lý do cụ thể được cung cấp.</p>";

        var emailSubject = "❌ Yêu cầu nâng cấp VIP không được phê duyệt - NoteVui";
        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #F44336; padding: 20px; text-align: center; color: white;'>
                    <h2 style='margin: 0;'>Yêu cầu nâng cấp VIP không được duyệt</h2>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Xin chào <strong>{user.FullName}</strong>,</p>
                    <p>Rất tiếc, yêu cầu nâng cấp gói <strong>{planNameStr}</strong> của bạn đã không được phê duyệt.</p>
                    {reasonHtml}
                    <p>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với đội ngũ hỗ trợ của chúng tôi để được giải đáp.</p>
                    <p>Bạn có thể gửi lại yêu cầu nâng cấp bất cứ lúc nào sau khi đã kiểm tra lại thông tin.</p>
                    <br/>
                    <p>Trân trọng,<br/><strong>Đội ngũ NoteVui</strong></p>
                </div>
                <div style='background-color: #f9f9f9; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
                    <p style='margin: 0;'>Đây là email tự động từ hệ thống NoteVui. Vui lòng không trả lời qua email này.</p>
                </div>
            </div>";

        try
        {
            await _mailService.SendEmailAsync(user.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi gửi email từ chối VIP: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a confirmation email to the User after they create a subscription request.
    /// </summary>
    private async Task SendRequestConfirmationEmailAsync(AppUser user, PlanType planType)
    {
        if (user.Email == null) return;

        var planNameStr = GetPlanDisplayName(planType);
        var emailSubject = "📝 Yêu cầu nâng cấp VIP đã được ghi nhận - NoteVui";
        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; text-align: center; color: white;'>
                    <h2 style='margin: 0;'>📝 Yêu cầu nâng cấp đã được ghi nhận</h2>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Xin chào <strong>{user.FullName}</strong>,</p>
                    <p>Chúng tôi đã nhận được yêu cầu nâng cấp lên gói <strong>{planNameStr}</strong> của bạn.</p>
                    <div style='background-color: #FFF3E0; border-left: 4px solid #FF9800; padding: 12px; margin: 15px 0; border-radius: 4px;'>
                        <p style='margin: 0;'><strong>⏳ Trạng thái:</strong> Đang chờ phê duyệt</p>
                        <p style='margin: 5px 0 0 0;'>Quản trị viên sẽ xem xét và phản hồi yêu cầu của bạn trong thời gian sớm nhất.</p>
                    </div>
                    <p>Bạn có thể theo dõi trạng thái yêu cầu trong ứng dụng NoteVui bất cứ lúc nào.</p>
                    <p>Nếu có thắc mắc, đừng ngần ngại liên hệ với đội ngũ hỗ trợ của chúng tôi.</p>
                    <br/>
                    <p>Trân trọng,<br/><strong>Đội ngũ NoteVui</strong></p>
                </div>
                <div style='background-color: #f9f9f9; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
                    <p style='margin: 0;'>Đây là email tự động từ hệ thống NoteVui. Vui lòng không trả lời qua email này.</p>
                </div>
            </div>";

        try
        {
            await _mailService.SendEmailAsync(user.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi gửi email xác nhận yêu cầu: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a notification email to all Admin users when a new subscription request is created.
    /// </summary>
    private async Task SendNewRequestNotificationToAdminsAsync(AppUser requestUser, PlanType planType, string? note)
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        if (admins == null || admins.Count == 0) return;

        var planNameStr = GetPlanDisplayName(planType);
        var noteHtml = !string.IsNullOrWhiteSpace(note)
            ? $"<p><strong>Ghi chú của người dùng:</strong> {note}</p>"
            : "";
        var now = DateTime.UtcNow;

        var emailSubject = $"🔔 Yêu cầu nâng cấp VIP mới từ {requestUser.FullName} - NoteVui";
        var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background: linear-gradient(135deg, #FF6B35 0%, #F7C948 100%); padding: 20px; text-align: center; color: white;'>
                    <h2 style='margin: 0;'>🔔 Có yêu cầu nâng cấp VIP mới!</h2>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Xin chào Admin,</p>
                    <p>Có một yêu cầu nâng cấp VIP mới cần được xem xét:</p>
                    <table style='width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 20px;'>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Người dùng:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{requestUser.FullName} ({requestUser.Email})</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Gói yêu cầu:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{planNameStr}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Thời gian gửi:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{now.AddHours(7):dd/MM/yyyy HH:mm} (Giờ VN)</td>
                        </tr>
                    </table>
                    {noteHtml}
                    <div style='background-color: #E3F2FD; border-left: 4px solid #2196F3; padding: 12px; margin: 15px 0; border-radius: 4px;'>
                        <p style='margin: 0;'>Vui lòng đăng nhập vào Admin Portal để phê duyệt hoặc từ chối yêu cầu này.</p>
                    </div>
                    <br/>
                    <p>Trân trọng,<br/><strong>Hệ thống NoteVui</strong></p>
                </div>
                <div style='background-color: #f9f9f9; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
                    <p style='margin: 0;'>Đây là email tự động từ hệ thống NoteVui. Vui lòng không trả lời qua email này.</p>
                </div>
            </div>";

        // Send to all admins in parallel
        var emailTasks = admins
            .Where(a => !string.IsNullOrEmpty(a.Email))
            .Select(admin => Task.Run(async () =>
            {
                try
                {
                    await _mailService.SendEmailAsync(admin.Email!, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi gửi email thông báo Admin ({admin.Email}): {ex.Message}");
                }
            }));

        await Task.WhenAll(emailTasks);
    }
}
