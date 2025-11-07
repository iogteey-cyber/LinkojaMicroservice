using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Services;
using LinkojaMicroservice.Data;
using LinkojaMicroservice.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;
        private readonly ApplicationDbContext _context;

        public BusinessController(IBusinessService businessService, ApplicationDbContext context)
        {
            _businessService = businessService;
            _context = context;
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
        public async Task<IActionResult> GetAllBusinesses(
            [FromQuery] string category = null, 
            [FromQuery] string status = null,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null,
            [FromQuery] double? radiusKm = null)
        {
            try
            {
                var businesses = await _businessService.GetAllBusinesses(category, status);
                
                // Filter by location if coordinates provided
                if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
                {
                    businesses = businesses.Where(b => 
                        b.Latitude.HasValue && 
                        b.Longitude.HasValue &&
                        CalculateDistance(latitude.Value, longitude.Value, b.Latitude.Value, b.Longitude.Value) <= radiusKm.Value
                    ).ToList();
                }
                
                var businessDtos = businesses.Select(b => new BusinessDto
                {
                    Id = b.Id,
                    OwnerId = b.OwnerId,
                    OwnerName = b.Owner?.Name,
                    Name = b.Name,
                    LogoUrl = b.LogoUrl,
                    CoverPhotoUrl = b.CoverPhotoUrl,
                    Description = b.Description,
                    Category = b.Category,
                    Address = b.Address,
                    Latitude = b.Latitude,
                    Longitude = b.Longitude,
                    Status = b.Status,
                    ReviewCount = b.Reviews?.Count ?? 0,
                    AverageRating = b.Reviews?.Any() == true ? b.Reviews.Average(r => r.Rating) : 0,
                    FollowerCount = b.Followers?.Count ?? 0,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                }).ToList();

                return Ok(businessDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching businesses", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBusinessById(int id)
        {
            try
            {
                var business = await _businessService.GetBusinessById(id);
                var businessDto = new BusinessDto
                {
                    Id = business.Id,
                    OwnerId = business.OwnerId,
                    OwnerName = business.Owner?.Name,
                    Name = business.Name,
                    LogoUrl = business.LogoUrl,
                    CoverPhotoUrl = business.CoverPhotoUrl,
                    Description = business.Description,
                    Category = business.Category,
                    Address = business.Address,
                    Latitude = business.Latitude,
                    Longitude = business.Longitude,
                    Status = business.Status,
                    ReviewCount = business.Reviews?.Count ?? 0,
                    AverageRating = business.Reviews?.Any() == true ? business.Reviews.Average(r => r.Rating) : 0,
                    FollowerCount = business.Followers?.Count ?? 0,
                    CreatedAt = business.CreatedAt,
                    UpdatedAt = business.UpdatedAt
                };

                return Ok(businessDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching the business", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest request)
        {
            try
            {
                var userId = GetUserId();
                var business = await _businessService.CreateBusiness(userId, request);
                return CreatedAtAction(nameof(GetBusinessById), new { id = business.Id }, business);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the business", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBusiness(int id, [FromBody] UpdateBusinessRequest request)
        {
            try
            {
                var userId = GetUserId();
                var business = await _businessService.UpdateBusiness(id, userId, request);
                return Ok(business);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the business", error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusiness(int id)
        {
            try
            {
                var userId = GetUserId();
                await _businessService.DeleteBusiness(id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the business", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-businesses")]
        public async Task<IActionResult> GetMyBusinesses()
        {
            try
            {
                var userId = GetUserId();
                var businesses = await _businessService.GetUserBusinesses(userId);
                return Ok(businesses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching your businesses", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{id}/reviews")]
        public async Task<IActionResult> AddReview(int id, [FromBody] CreateReviewRequest request)
        {
            try
            {
                var userId = GetUserId();
                var review = await _businessService.AddReview(id, userId, request);
                return CreatedAtAction(nameof(GetBusinessById), new { id = id }, review);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding the review", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{id}/follow")]
        public async Task<IActionResult> FollowBusiness(int id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _businessService.FollowBusiness(id, userId);
                if (result)
                {
                    return Ok(new { message = "Business followed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Already following this business" });
                }
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while following the business", error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}/follow")]
        public async Task<IActionResult> UnfollowBusiness(int id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _businessService.UnfollowBusiness(id, userId);
                if (result)
                {
                    return Ok(new { message = "Business unfollowed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Not following this business" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while unfollowing the business", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{id}/posts")]
        public async Task<IActionResult> CreatePost(int id, [FromBody] CreatePostRequest request)
        {
            try
            {
                var userId = GetUserId();
                var post = await _businessService.CreatePost(id, userId, request);
                return CreatedAtAction(nameof(GetBusinessById), new { id = id }, post);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the post", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{id}/insights")]
        public async Task<IActionResult> GetBusinessInsights(int id)
        {
            try
            {
                var userId = GetUserId();
                var business = await _businessService.GetBusinessById(id);

                // Check if user owns this business
                if (business.OwnerId != userId)
                {
                    return Forbid();
                }

                var insights = new BusinessInsightsDto
                {
                    ProfileViews = 0, // To be implemented with view tracking
                    FollowerCount = business.Followers?.Count ?? 0,
                    ReviewCount = business.Reviews?.Count ?? 0,
                    AverageRating = business.Reviews?.Any() == true ? business.Reviews.Average(r => r.Rating) : 0,
                    PostCount = business.Posts?.Count ?? 0
                };

                return Ok(insights);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching insights", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("reviews/{reviewId}/report")]
        public async Task<IActionResult> ReportReview(int reviewId, [FromBody] ReportReviewRequest request)
        {
            try
            {
                var userId = GetUserId();
                
                // Check if review exists
                var review = await _context.BusinessReviews.FindAsync(reviewId);
                if (review == null)
                {
                    return NotFound(new { message = "Review not found" });
                }

                // Check if user has already reported this review
                var existingReport = await _context.ReviewReports
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.ReportedByUserId == userId);

                if (existingReport != null)
                {
                    return BadRequest(new { message = "You have already reported this review" });
                }

                // Create report
                var report = new ReviewReport
                {
                    ReviewId = reviewId,
                    ReportedByUserId = userId,
                    Reason = request.Reason,
                    Description = request.Description,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReviewReports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Review reported successfully", reportId = report.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while reporting the review", error = ex.Message });
            }
        }

        // Helper method to calculate distance between two points using Haversine formula
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
