using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
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
        public async Task<IActionResult> GetAllBusinesses([FromQuery] string category = null, [FromQuery] string status = null)
        {
            try
            {
                var businesses = await _businessService.GetAllBusinesses(category, status);
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
    }
}
