using LinkojaMicroservice.Models;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IAuthService
    {
        Task<User> Register(string email, string password, string phone, string name);
        Task<User> Login(string email, string password);
        string GenerateJwtToken(User user);
    }
}
