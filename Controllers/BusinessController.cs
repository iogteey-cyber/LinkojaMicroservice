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

                var response = ResponseStatus<List<BusinessDto>>.Create<BasicResponse<List<BusinessDto>>>("00", "Businesses fetched successfully", businessDtos, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching businesses", new { error = ex.Message }, false);
                return StatusCode(500, response);
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

                var response = ResponseStatus<BusinessDto>.Create<BasicResponse<BusinessDto>>("00", "Business fetched successfully", businessDto, true);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<Business>.Create<BasicResponse<Business>>("00", "Business created successfully", business, true);
                return CreatedAtAction(nameof(GetBusinessById), new { id = business.Id }, response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while creating the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<Business>.Create<BasicResponse<Business>>("00", "Business updated successfully", business, true);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while updating the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Business deleted successfully", null, true);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while deleting the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<List<Business>>.Create<BasicResponse<List<Business>>>("00", "Businesses fetched successfully", businesses, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching your businesses", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<BusinessReview>.Create<BasicResponse<BusinessReview>>("00", "Review added successfully", review, true);
                return CreatedAtAction(nameof(GetBusinessById), new { id = id }, response);
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while adding the review", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Business followed successfully", null, true);
                    return Ok(response);
                }
                else
                {
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("01", "Already following this business", null, false);
                    return BadRequest(response);
                }
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while following the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Business unfollowed successfully", null, true);
                    return Ok(response);
                }
                else
                {
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("01", "Not following this business", null, false);
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while unfollowing the business", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<BusinessPost>.Create<BasicResponse<BusinessPost>>("00", "Post created successfully", post, true);
                return CreatedAtAction(nameof(GetBusinessById), new { id = id }, response);
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while creating the post", new { error = ex.Message }, false);
                return StatusCode(500, response);
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

                var response = ResponseStatus<BusinessInsightsDto>.Create<BasicResponse<BusinessInsightsDto>>("00", "Insights fetched successfully", insights, true);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while fetching insights", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                    var notFound = ResponseStatus<object>.Create<BasicResponse<object>>("04", "Review not found", null, false);
                    return NotFound(notFound);
                }

                // Check if user has already reported this review
                var existingReport = await _context.ReviewReports
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.ReportedByUserId == userId);

                if (existingReport != null)
                {
                    var badReq = ResponseStatus<object>.Create<BasicResponse<object>>("01", "You have already reported this review", null, false);
                    return BadRequest(badReq);
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

                var ok = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Review reported successfully", new { reportId = report.Id }, true);
                return Ok(ok);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while reporting the review", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
