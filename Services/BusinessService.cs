using LinkojaMicroservice.Data;
using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public BusinessService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Business> CreateBusiness(int ownerId, CreateBusinessRequest request)
        {
            var business = new Business
            {
                OwnerId = ownerId,
                Name = request.Name,
                LogoUrl = request.LogoUrl,
                CoverPhotoUrl = request.CoverPhotoUrl,
                Description = request.Description,
                Category = request.Category,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                VerificationDocUrl = request.VerificationDocUrl,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Businesses.Add(business);
            await _context.SaveChangesAsync();

            return business;
        }

        public async Task<Business> UpdateBusiness(int businessId, int userId, UpdateBusinessRequest request)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            if (business.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this business");
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
            if (request.Latitude.HasValue)
                business.Latitude = request.Latitude;
            if (request.Longitude.HasValue)
                business.Longitude = request.Longitude;

            business.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return business;
        }

        public async Task<Business> GetBusinessById(int businessId)
        {
            var business = await _context.Businesses
                .Include(b => b.Owner)
                .Include(b => b.Reviews)
                .Include(b => b.Followers)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            return business;
        }

        public async Task<List<Business>> GetAllBusinesses(string category = null, string status = null)
        {
            var query = _context.Businesses
                .Include(b => b.Owner)
                .Include(b => b.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(b => b.Category == category);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Business>> GetUserBusinesses(int userId)
        {
            return await _context.Businesses
                .Include(b => b.Reviews)
                .Include(b => b.Followers)
                .Where(b => b.OwnerId == userId)
                .ToListAsync();
        }

        public async Task<bool> DeleteBusiness(int businessId, int userId)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            if (business.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this business");
            }

            _context.Businesses.Remove(business);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BusinessReview> AddReview(int businessId, int userId, CreateReviewRequest request)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            // Prevent business owners from reviewing their own businesses
            if (business.OwnerId == userId)
            {
                throw new InvalidOperationException("You cannot review your own business");
            }

            // Check if user has already reviewed this business
            var existingReview = await _context.BusinessReviews
                .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.UserId == userId);

            if (existingReview != null)
            {
                throw new InvalidOperationException("You have already reviewed this business");
            }

            var review = new BusinessReview
            {
                BusinessId = businessId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment,
                PhotoUrl = request.PhotoUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BusinessReviews.Add(review);
            await _context.SaveChangesAsync();

            // Send notification to business owner
            await _notificationService.CreateNotification(
                business.OwnerId,
                "review",
                "New Review",
                $"Your business '{business.Name}' received a new {request.Rating}-star review",
                businessId
            );

            return review;
        }

        public async Task<bool> FollowBusiness(int businessId, int userId)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            var existingFollow = await _context.BusinessFollowers
                .FirstOrDefaultAsync(f => f.BusinessId == businessId && f.UserId == userId);

            if (existingFollow != null)
            {
                return false; // Already following
            }

            var follower = new BusinessFollower
            {
                BusinessId = businessId,
                UserId = userId,
                FollowedAt = DateTime.UtcNow
            };

            _context.BusinessFollowers.Add(follower);
            await _context.SaveChangesAsync();

            // Send notification to business owner
            var user = await _context.Users.FindAsync(userId);
            await _notificationService.CreateNotification(
                business.OwnerId,
                "follower",
                "New Follower",
                $"{user?.Name ?? "Someone"} started following your business '{business.Name}'",
                businessId
            );

            return true;
        }

        public async Task<bool> UnfollowBusiness(int businessId, int userId)
        {
            var follower = await _context.BusinessFollowers
                .FirstOrDefaultAsync(f => f.BusinessId == businessId && f.UserId == userId);

            if (follower == null)
            {
                return false; // Not following
            }

            _context.BusinessFollowers.Remove(follower);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<BusinessPost> CreatePost(int businessId, int userId, CreatePostRequest request)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            if (business.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to post for this business");
            }

            var post = new BusinessPost
            {
                BusinessId = businessId,
                Content = request.Content,
                ImageUrl = request.ImageUrl,
                VideoUrl = request.VideoUrl,
                Likes = 0,
                Comments = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BusinessPosts.Add(post);
            await _context.SaveChangesAsync();

            return post;
        }
    }
}
