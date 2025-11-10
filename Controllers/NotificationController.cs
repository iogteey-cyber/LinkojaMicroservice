using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }
            return userId;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
        {
            try
            {
                var userId = GetUserId();
                var notifications = await _notificationService.GetUserNotifications(userId, unreadOnly);

                var notificationDtos = notifications.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    RelatedBusinessId = n.RelatedBusinessId,
                    RelatedBusinessName = n.RelatedBusiness?.Name,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Notifications fetched successfully", notificationDtos, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching notifications", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetUserId();
                var count = await _notificationService.GetUnreadCount(userId);
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Unread count fetched successfully", new { count }, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching unread count", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _notificationService.MarkAsRead(id, userId);

                if (result)
                {
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Notification marked as read", null, true);
                    return Ok(response);
                }
                else
                {
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Notification not found", null, false);
                    return NotFound(response);
                }
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetUserId();
                await _notificationService.MarkAllAsRead(userId);
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "All notifications marked as read", null, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }
    }
}
