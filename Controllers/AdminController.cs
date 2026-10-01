using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Data;
using LinkojaMicroservice.Models;
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
        private readonly Services.IBusinessIdGeneratorService _businessIdGenerator;

        public AdminController(ApplicationDbContext context, Services.INotificationService notificationService, Services.IEmailService emailService, Services.IBusinessIdGeneratorService businessIdGenerator)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _businessIdGenerator = businessIdGenerator;
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

        [HttpPost("businesses")]
        public async Task<IActionResult> CreateBusinessAdmin([FromBody] AdminCreateBusinessRequest request)
        {
            try
            {
                var owner = await _context.Users.FindAsync(request.OwnerId);
                if (owner == null)
                {
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Owner not found", null, false);
                    return NotFound(notFound);
                }

                var business = new Business
                {
                    OwnerId = request.OwnerId,
                    Name = request.Name,
                    LogoUrl = request.LogoUrl,
                    CoverPhotoUrl = request.CoverPhotoUrl,
                    Description = request.Description,
                    Category = request.Category,
                    Address = request.Address,
                    Area = request.Area,
                    Road = request.Road,
                    Street = request.Street,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    VerificationDocUrl = request.VerificationDocUrl,
                    email = request.Email,
                    website = request.Website,
                    Status = "pending",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                business.BusinessId = await _businessIdGenerator.GenerateBusinessId(request.Area, request.Road, request.Street);

                _context.Businesses.Add(business);
                await _context.SaveChangesAsync();

                var businessDto = BusinessController.MapToDto(business);
                var response = ResponseStatus<BusinessDto>.Create<BasicResponse<BusinessDto>>("00", "Business created successfully", businessDto, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while creating the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPut("businesses/{id}")]
        public async Task<IActionResult> UpdateBusinessAdmin(int id, [FromBody] AdminUpdateBusinessRequest request)
        {
            try
            {
                var business = await _context.Businesses
                    .Include(b => b.BusinessCategories)
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (business == null)
                {
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Business not found", null, false);
                    return NotFound(notFound);
                }

                if (!string.IsNullOrEmpty(request.Name))
                    business.Name = request.Name;
                if (!string.IsNullOrEmpty(request.LogoUrl))
                    business.LogoUrl = request.LogoUrl;
                if (!string.IsNullOrEmpty(request.CoverPhotoUrl))
                    business.CoverPhotoUrl = request.CoverPhotoUrl;
                if (!string.IsNullOrEmpty(request.Description))
                    business.Description = request.Description;
                if (!string.IsNullOrEmpty(request.Category))
                    business.Category = request.Category;
                if (!string.IsNullOrEmpty(request.Address))
                    business.Address = request.Address;
                if (!string.IsNullOrEmpty(request.Area))
                    business.Area = request.Area;
                if (!string.IsNullOrEmpty(request.Road))
                    business.Road = request.Road;
                if (!string.IsNullOrEmpty(request.Street))
                    business.Street = request.Street;
                if (request.Latitude.HasValue)
                    business.Latitude = request.Latitude;
                if (request.Longitude.HasValue)
                    business.Longitude = request.Longitude;
                if (!string.IsNullOrEmpty(request.Email))
                    business.email = request.Email;
                if (!string.IsNullOrEmpty(request.Website))
                    business.website = request.Website;
                if (!string.IsNullOrEmpty(request.Status))
                    business.Status = request.Status;
                if (request.IsActive.HasValue)
                    business.IsActive = request.IsActive.Value;

                // Full replace of category/subcategory assignments, if provided
                if (request.Categories != null)
                {
                    _context.BusinessCategories.RemoveRange(business.BusinessCategories);
                    foreach (var c in request.Categories)
                    {
                        _context.BusinessCategories.Add(new BusinessCategory
                        {
                            BusinessId = business.Id,
                            CategoryName = c.CategoryName,
                            Subcategory = c.Subcategory,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Full sync of product/service list, if provided
                if (request.Products != null)
                {
                    var existingProducts = business.Products.ToList();
                    var keepIds = request.Products.Where(p => p.Id > 0).Select(p => p.Id).ToHashSet();

                    foreach (var existing in existingProducts)
                    {
                        if (!keepIds.Contains(existing.Id))
                        {
                            _context.BusinessProducts.Remove(existing);
                        }
                    }

                    foreach (var p in request.Products)
                    {
                        if (p.Id > 0)
                        {
                            var existing = existingProducts.FirstOrDefault(e => e.Id == p.Id);
                            if (existing != null)
                            {
                                if (!string.IsNullOrEmpty(p.Name))
                                    existing.Name = p.Name;
                                if (p.Description != null)
                                    existing.Description = p.Description;
                                if (p.PhotoUrl != null)
                                    existing.PhotoUrl = p.PhotoUrl;
                                if (!string.IsNullOrEmpty(p.Type))
                                    existing.Type = p.Type;
                            }
                        }
                        else
                        {
                            _context.BusinessProducts.Add(new Models.BusinessProduct
                            {
                                BusinessId = business.Id,
                                Name = p.Name,
                                Description = p.Description,
                                PhotoUrl = p.PhotoUrl,
                                Type = string.IsNullOrEmpty(p.Type) ? "Product" : p.Type,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var businessDto = BusinessController.MapToDto(business);
                var response = ResponseStatus<BusinessDto>.Create<BasicResponse<BusinessDto>>("00", "Business updated successfully", businessDto, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while updating the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpGet("businesses/by-business-id/{businessId}")]
        public async Task<IActionResult> GetBusinessByBusinessId(string businessId)
        {
            try
            {
                var business = await _context.Businesses
                    .Include(b => b.Owner)
                    .Include(b => b.Reviews)
                    .Include(b => b.Followers)
                    .Include(b => b.BusinessCategories)
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.BusinessId == businessId);

                if (business == null)
                {
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Business not found", null, false);
                    return NotFound(notFound);
                }

                var businessDto = BusinessController.MapToDto(business);
                var response = ResponseStatus<BusinessDto>.Create<BasicResponse<BusinessDto>>("00", "Business fetched successfully", businessDto, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPut("businesses/{id}/activate")]
        public async Task<IActionResult> ActivateBusiness(int id)
        {
            try
            {
                var business = await _context.Businesses.FindAsync(id);
                if (business == null)
                {
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Business not found", null, false);
                    return NotFound(notFound);
                }

                business.IsActive = true;
                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var businessDto = BusinessController.MapToDto(business);
                var response = ResponseStatus<BusinessDto>.Create<BasicResponse<BusinessDto>>("00", "Business activated successfully", businessDto, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while activating the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPut("businesses/{id}/deactivate")]
        public async Task<IActionResult> DeactivateBusiness(int id)
        {
            try
            {
                var business = await _context.Businesses.FindAsync(id);
                if (business == null)
                {
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Business not found", null, false);
                    return NotFound(notFound);
                }

                business.IsActive = false;
                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var businessDto = BusinessController.MapToDto(business);
                var response = ResponseStatus<BusinessDto>.Create<BasicResponse<BusinessDto>>("00", "Business deactivated successfully", businessDto, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while deactivating the business", new { error = ex.Message }, false);
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
