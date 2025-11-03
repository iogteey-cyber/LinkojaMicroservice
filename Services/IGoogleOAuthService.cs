using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IGoogleOAuthService
    {
        Task<GoogleUserInfo> ValidateTokenAsync(string idToken);
    }

    public class GoogleUserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GoogleId { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string Picture { get; set; } = string.Empty;
    }
}
