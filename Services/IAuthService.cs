using LinkojaMicroservice.Models;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IAuthService
    {
        Task<User> Register(string email, string password, string phone, string name);
        Task<User> Login(string email, string password);
        string GenerateJwtToken(User user);
        Task<string> GeneratePasswordResetToken(string email);
        Task<bool> ResetPassword(string token, string newPassword);
        Task<bool> ChangePassword(int userId, string currentPassword, string newPassword);
        Task<User> SocialLogin(string provider, string accessToken, string email, string name, string photoUrl);
    }
}
