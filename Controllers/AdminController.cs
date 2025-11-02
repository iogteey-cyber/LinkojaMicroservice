using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.INotificationService _notificationService;

        public AdminController(ApplicationDbContext context, Services.INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("businesses/pending")]
        public async Task<IActionResult> GetPendingBusinesses()
        {
            try
            {
                var pendingBusinesses = await _context.Businesses
                    .Include(b => b.Owner)
                    .Where(b => b.Status == "pending")
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                return Ok(pendingBusinesses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching pending businesses", error = ex.Message });
            }
        }

        [HttpPost("businesses/{id}/approve")]
        public async Task<IActionResult> ApproveBusiness(int id, [FromBody] ApproveBusinessRequest request)
        {
            try
            {
                var business = await _context.Businesses.FindAsync(id);
                if (business == null)
                {
                    return NotFound(new { message = "Business not found" });
                }

                if (request.Status != "verified" && request.Status != "rejected")
                {
                    return BadRequest(new { message = "Status must be 'verified' or 'rejected'" });
                }

                business.Status = request.Status;
                business.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification to business owner
                var notificationTitle = request.Status == "verified" ? "Business Approved" : "Business Rejected";
                var notificationMessage = request.Status == "verified"
                    ? $"Congratulations! Your business '{business.Name}' has been verified and is now live."
                    : $"Your business '{business.Name}' was not approved. {request.Reason ?? ""}";

                await _notificationService.CreateNotification(
                    business.OwnerId,
                    "approval",
                    notificationTitle,
                    notificationMessage,
                    id
                );

                return Ok(new { message = $"Business {request.Status} successfully", business });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while approving business", error = ex.Message });
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            try
            {
                var analytics = new BusinessAnalyticsDto
                {
                    TotalBusinesses = await _context.Businesses.CountAsync(),
                    PendingBusinesses = await _context.Businesses.CountAsync(b => b.Status == "pending"),
                    VerifiedBusinesses = await _context.Businesses.CountAsync(b => b.Status == "verified"),
                    RejectedBusinesses = await _context.Businesses.CountAsync(b => b.Status == "rejected"),
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalReviews = await _context.BusinessReviews.CountAsync()
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching analytics", error = ex.Message });
            }
        }

        [HttpGet("businesses")]
        public async Task<IActionResult> GetAllBusinessesAdmin([FromQuery] string status = null)
        {
            try
            {
                var query = _context.Businesses
                    .Include(b => b.Owner)
                    .Include(b => b.Reviews)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(b => b.Status == status);
                }

                var businesses = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
                return Ok(businesses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching businesses", error = ex.Message });
            }
        }

        [HttpDelete("businesses/{id}")]
        public async Task<IActionResult> DeleteBusinessAdmin(int id)
        {
            try
            {
                var business = await _context.Businesses.FindAsync(id);
                if (business == null)
                {
                    return NotFound(new { message = "Business not found" });
                }

                _context.Businesses.Remove(business);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Business deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting business", error = ex.Message });
            }
        }
    }
}
