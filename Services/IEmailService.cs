using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IEmailService
    {
        // Each method returns true if the email was handed off to the SMTP server
        // successfully, and false if sending failed (errors are logged, not thrown),
        // so callers can decide whether to fall back to another delivery channel.
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
        Task<bool> SendOtpEmailAsync(string toEmail, string otpCode);
        Task<bool> SendBusinessApprovalEmailAsync(string toEmail, string businessName, string status, string reason = null);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
    }
}
