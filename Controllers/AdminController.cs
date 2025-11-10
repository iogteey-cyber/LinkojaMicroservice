using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
        private readonly Services.IEmailService _emailService;

        public AdminController(ApplicationDbContext context, Services.INotificationService notificationService, Services.IEmailService emailService)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
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

                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Pending businesses fetched successfully", pendingBusinesses, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching pending businesses", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Business not found", null, false);
                    return NotFound(notFound);
                }

                if (request.Status != "verified" && request.Status != "rejected")
                {
                    var bad = ResponseStatus<object>.Create<BasicResponse<object>>("01", "Status must be 'verified' or 'rejected'", null, false);
                    return BadRequest(bad);
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

                // Send email notification to business owner
                try
                {
                    var owner = await _context.Users.FindAsync(business.OwnerId);
                    if (owner != null && !string.IsNullOrEmpty(owner.Email))
                    {
                        await _emailService.SendBusinessApprovalEmailAsync(
                            owner.Email,
                            business.Name,
                            request.Status,
                            request.Reason
                        );
                    }
                }
                catch
                {
                    // Don't fail if email fails
                }

                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", $"Business {request.Status} successfully", business, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while approving business", new { error = ex.Message }, false);
                return StatusCode(500, response);
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

                var response = ResponseStatus<BusinessAnalyticsDto>.Create<BasicResponse<BusinessAnalyticsDto>>("00", "Analytics fetched successfully", analytics, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching analytics", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Businesses fetched successfully", businesses, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching businesses", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Business not found", null, false);
                    return NotFound(notFound);
                }

                _context.Businesses.Remove(business);
                await _context.SaveChangesAsync();

                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Business deleted successfully", null, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while deleting business", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpGet("reports/reviews")]
        public async Task<IActionResult> GetReviewReports([FromQuery] string status = null)
        {
            try
            {
                var query = _context.ReviewReports
                    .Include(r => r.Review)
                        .ThenInclude(rev => rev.Business)
                    .Include(r => r.ReportedBy)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

                var reportDtos = reports.Select(r => new ReviewReportDto
                {
                    Id = r.Id,
                    ReviewId = r.ReviewId,
                    ReportedByUserId = r.ReportedByUserId,
                    ReportedByName = r.ReportedBy?.Name,
                    Reason = r.Reason,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                }).ToList();

                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Review reports fetched successfully", reportDtos, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching reports", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPut("reports/reviews/{reportId}/resolve")]
        public async Task<IActionResult> ResolveReviewReport(int reportId, [FromQuery] string action = "dismiss")
        {
            try
            {
                var report = await _context.ReviewReports
                    .Include(r => r.Review)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null)
                {
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Report not found", null, false);
                    return NotFound(notFound);
                }

                if (action == "delete-review")
                {
                    // Delete the reported review
                    _context.BusinessReviews.Remove(report.Review);
                    report.Status = "resolved";
                    report.ResolvedAt = DateTime.UtcNow;
                }
                else if (action == "dismiss")
                {
                    // Dismiss the report
                    report.Status = "dismissed";
                    report.ResolvedAt = DateTime.UtcNow;
                }
                else
                {
                    var bad = ResponseStatus<object>.Create<BasicResponse<object>>("01", "Invalid action. Use 'delete-review' or 'dismiss'", null, false);
                    return BadRequest(bad);
                }

                await _context.SaveChangesAsync();

                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", $"Report {report.Status} successfully", null, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while resolving the report", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }
    }
}
