namespace NoteVui.Application.Services.Interfaces;

public interface IMailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}
