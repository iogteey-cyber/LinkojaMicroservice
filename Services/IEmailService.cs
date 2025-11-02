using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
        Task SendOtpEmailAsync(string toEmail, string otpCode);
        Task SendBusinessApprovalEmailAsync(string toEmail, string businessName, string status, string reason = null);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
    }
}
