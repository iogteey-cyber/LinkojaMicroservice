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
        private readonly IBusinessIdGeneratorService _businessIdGenerator;

        public BusinessService(ApplicationDbContext context, INotificationService notificationService, IBusinessIdGeneratorService businessIdGenerator)
        {
            _context = context;
            _notificationService = notificationService;
            _businessIdGenerator = businessIdGenerator;
        }

        public async Task<Business> CreateBusiness(int ownerId, CreateBusinessRequest request)
        {
            // Duplicate validations (case-insensitive trim comparisons)
            string name = request.Name?.Trim();
            string logo = request.LogoUrl?.Trim();
            string cover = request.CoverPhotoUrl?.Trim();
            string email = request.Email?.Trim();
            string website = request.Website?.Trim();

            if (await _context.Businesses.AnyAsync(b => b.OwnerId == ownerId && b.Name.ToLower() == name.ToLower()))
            {
                throw new InvalidOperationException("You already have a business with this name.");
            }
            if (!string.IsNullOrEmpty(email) && await _context.Businesses.AnyAsync(b => b.email != null && b.email.ToLower() == email.ToLower()))
            {
                throw new InvalidOperationException("A business with this email already exists.");
            }
            if (!string.IsNullOrEmpty(website) && await _context.Businesses.AnyAsync(b => b.website != null && b.website.ToLower() == website.ToLower()))
            {
                throw new InvalidOperationException("A business with this website already exists.");
            }
            if (!string.IsNullOrEmpty(logo) && await _context.Businesses.AnyAsync(b => b.LogoUrl != null && b.LogoUrl.ToLower() == logo.ToLower()))
            {
                throw new InvalidOperationException("Logo URL already in use by another business.");
            }
            if (!string.IsNullOrEmpty(cover) && await _context.Businesses.AnyAsync(b => b.CoverPhotoUrl != null && b.CoverPhotoUrl.ToLower() == cover.ToLower()))
            {
                throw new InvalidOperationException("Cover photo URL already in use by another business.");
            }

            var business = new Business
            {
                OwnerId = ownerId,
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

            // Prepare trimmed values
            string newName = request.Name?.Trim();
            string newLogo = request.LogoUrl?.Trim();
            string newCover = request.CoverPhotoUrl?.Trim();
            string newEmail = request.Email?.Trim();
            string newWebsite = request.Website?.Trim();

            // Duplicate checks excluding current business
            if (!string.IsNullOrEmpty(newName) && await _context.Businesses.AnyAsync(b => b.Id != businessId && b.OwnerId == userId && b.Name.ToLower() == newName.ToLower()))
            {
                throw new InvalidOperationException("You already have another business with this name.");
            }
            if (!string.IsNullOrEmpty(newEmail) && await _context.Businesses.AnyAsync(b => b.Id != businessId && b.email != null && b.email.ToLower() == newEmail.ToLower()))
            {
                throw new InvalidOperationException("Another business already uses this email.");
            }
            if (!string.IsNullOrEmpty(newWebsite) && await _context.Businesses.AnyAsync(b => b.Id != businessId && b.website != null && b.website.ToLower() == newWebsite.ToLower()))
            {
                throw new InvalidOperationException("Another business already uses this website.");
            }
            if (!string.IsNullOrEmpty(newLogo) && await _context.Businesses.AnyAsync(b => b.Id != businessId && b.LogoUrl != null && b.LogoUrl.ToLower() == newLogo.ToLower()))
            {
                throw new InvalidOperationException("Logo URL is already used by another business.");
            }
            if (!string.IsNullOrEmpty(newCover) && await _context.Businesses.AnyAsync(b => b.Id != businessId && b.CoverPhotoUrl != null && b.CoverPhotoUrl.ToLower() == newCover.ToLower()))
            {
                throw new InvalidOperationException("Cover photo URL is already used by another business.");
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
                .Include(b => b.BusinessCategories)
                .Include(b => b.Products)
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
                .Include(b => b.Followers)
                .Include(b => b.BusinessCategories)
                .Include(b => b.Products)
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
        public async Task<List<Business>> GetAllBusinesses()
        {
            var query = _context.Businesses
                .Include(b => b.Owner)
                .Include(b => b.Reviews)
                .Include(b => b.Followers)
                .Include(b => b.BusinessCategories)
                .Include(b => b.Products)
                .AsQueryable();

           


            return await query.Where(b => b.Status == "approved").ToListAsync();
        }
        public async Task<List<Business>> GetUserBusinesses(int userId)
        {
            return await _context.Businesses
                .Include(b => b.Reviews)
                .Include(b => b.Followers)
                .Include(b => b.BusinessCategories)
                .Include(b => b.Products)
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
                Likes =0,
                Comments =0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BusinessPosts.Add(post);
            await _context.SaveChangesAsync();

            return post;
        }

        public async Task<Business> GetBusinessByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
            var business = await _context.Businesses
                .Include(b => b.Owner)
                .Include(b => b.Reviews)
                .Include(b => b.Followers)
                .Include(b => b.BusinessCategories)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.email != null && b.email.ToLower() == email.Trim().ToLower());
            if (business == null) throw new KeyNotFoundException("Business not found");
            return business;
        }

        public async Task<Business> GetBusinessByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required", nameof(phone));
            var business = await _context.Businesses
                .Include(b => b.Owner)
                .Include(b => b.Reviews)
                .Include(b => b.Followers)
                .Include(b => b.BusinessCategories)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Owner != null && b.Owner.Phone != null && b.Owner.Phone == phone.Trim());
            if (business == null) throw new KeyNotFoundException("Business not found");
            return business;
        }

        public async Task<BusinessProduct> AddProduct(int businessId, int userId, CreateBusinessProductRequest request)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            if (business.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to manage this business's products");
            }

            var product = new BusinessProduct
            {
                BusinessId = businessId,
                Name = request.Name,
                Description = request.Description,
                PhotoUrl = request.PhotoUrl,
                Type = string.IsNullOrEmpty(request.Type) ? "Product" : request.Type,
                CreatedAt = DateTime.UtcNow
            };

            _context.BusinessProducts.Add(product);
            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<BusinessProduct> UpdateProduct(int businessId, int productId, int userId, UpdateBusinessProductRequest request)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            if (business.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to manage this business's products");
            }

            var product = await _context.BusinessProducts
                .FirstOrDefaultAsync(p => p.Id == productId && p.BusinessId == businessId);
            if (product == null)
            {
                throw new KeyNotFoundException("Product/service not found");
            }

            if (!string.IsNullOrEmpty(request.Name))
                product.Name = request.Name;
            if (request.Description != null)
                product.Description = request.Description;
            if (request.PhotoUrl != null)
                product.PhotoUrl = request.PhotoUrl;
            if (!string.IsNullOrEmpty(request.Type))
                product.Type = request.Type;

            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteProduct(int businessId, int productId, int userId)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                throw new KeyNotFoundException("Business not found");
            }

            if (business.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to manage this business's products");
            }

            var product = await _context.BusinessProducts
                .FirstOrDefaultAsync(p => p.Id == productId && p.BusinessId == businessId);
            if (product == null)
            {
                throw new KeyNotFoundException("Product/service not found");
            }

            _context.BusinessProducts.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Business>> SearchBusinesses(
            string? q = null,
            string? category = null,
            string? subcategory = null,
            string? area = null,
            string? road = null,
            string? street = null,
            double? latitude = null,
            double? longitude = null,
            double? radiusKm = null)
        {
            var query = _context.Businesses
                .Include(b => b.Owner)
                .Include(b => b.Reviews)
                .Include(b => b.Followers)
                .Include(b => b.BusinessCategories)
                .Include(b => b.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(b =>
                    (b.Name != null && b.Name.ToLower().Contains(term)) ||
                    (b.Description != null && b.Description.ToLower().Contains(term)) ||
                    b.Products.Any(p => p.Name != null && p.Name.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.Trim().ToLower();
                query = query.Where(b =>
                    (b.Category != null && b.Category.ToLower() == cat) ||
                    b.BusinessCategories.Any(bc => bc.CategoryName != null && bc.CategoryName.ToLower() == cat));
            }

            if (!string.IsNullOrWhiteSpace(subcategory))
            {
                var sub = subcategory.Trim().ToLower();
                query = query.Where(b =>
                    b.BusinessCategories.Any(bc => bc.Subcategory != null && bc.Subcategory.ToLower() == sub));
            }

            if (!string.IsNullOrWhiteSpace(area))
            {
                var a = area.Trim().ToLower();
                query = query.Where(b => b.Area != null && b.Area.ToLower() == a);
            }

            if (!string.IsNullOrWhiteSpace(road))
            {
                var r = road.Trim().ToLower();
                query = query.Where(b => b.Road != null && b.Road.ToLower() == r);
            }

            if (!string.IsNullOrWhiteSpace(street))
            {
                var s = street.Trim().ToLower();
                query = query.Where(b => b.Street != null && b.Street.ToLower() == s);
            }

            var businesses = await query.ToListAsync();

            if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
            {
                businesses = businesses.Where(b =>
                    b.Latitude.HasValue &&
                    b.Longitude.HasValue &&
                    CalculateDistance(latitude.Value, longitude.Value, b.Latitude.Value, b.Longitude.Value) <= radiusKm.Value
                ).ToList();
            }

            return businesses;
        }

        // Helper method to calculate distance between two points using Haversine formula (shared with search)
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
