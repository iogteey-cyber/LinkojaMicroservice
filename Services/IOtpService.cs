using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IOtpService
    {
        Task<bool> SendOtp(string phoneNumber);
        Task<bool> VerifyOtp(string phoneNumber, string otpCode);
        Task<bool> ResendOtp(string phoneNumber);
    }
}
