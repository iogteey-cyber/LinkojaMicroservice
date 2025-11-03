using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }
}
