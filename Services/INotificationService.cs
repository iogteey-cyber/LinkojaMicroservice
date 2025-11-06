using LinkojaMicroservice.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface INotificationService
    {
        Task CreateNotification(int userId, string type, string title, string message, int? relatedBusinessId = null);
        Task<List<Notification>> GetUserNotifications(int userId, bool unreadOnly = false);
        Task<bool> MarkAsRead(int notificationId, int userId);
        Task<bool> MarkAllAsRead(int userId);
        Task<int> GetUnreadCount(int userId);
    }
}
