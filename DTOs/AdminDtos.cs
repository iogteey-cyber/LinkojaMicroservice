using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.DTOs
{
    public class ApproveBusinessRequest
    {
        [Required]
        public string Status { get; set; } // "verified" or "rejected"
        
        public string Reason { get; set; } // Rejection reason if applicable
    }

    public class BusinessAnalyticsDto
    {
        public int TotalBusinesses { get; set; }
        public int PendingBusinesses { get; set; }
        public int VerifiedBusinesses { get; set; }
        public int RejectedBusinesses { get; set; }
        public int TotalUsers { get; set; }
        public int TotalReviews { get; set; }
    }

    public class BusinessInsightsDto
    {
        public int ProfileViews { get; set; }
        public int FollowerCount { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int PostCount { get; set; }
    }

    // Admin directly creates a business on behalf of an owner; reuses CreateBusinessRequest's
    // fields (including Area/Road/Street for permanent BusinessId generation) plus OwnerId.
    public class AdminCreateBusinessRequest : CreateBusinessRequest
    {
        [Required]
        public int OwnerId { get; set; }
    }

    // Admin full edit of a business: location, category/subcategory assignment, status,
    // activation state, and basic product/service list management (no price/cart logic).
    public class AdminUpdateBusinessRequest
    {
        public string? Name { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Address { get; set; }
        public string? Area { get; set; }
        public string? Road { get; set; }
        public string? Street { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Status { get; set; } // "pending", "verified", "rejected"
        public bool? IsActive { get; set; }

        // Full replace: categories/subcategories assigned to the business
        public List<BusinessCategoryDto>? Categories { get; set; }

        // Full replace: products/services list. Include Id to update an existing one;
        // omit/0 Id to create a new one. Entries not present are removed.
        public List<AdminBusinessProductEntry>? Products { get; set; }
    }

    public class AdminBusinessProductEntry
    {
        public int Id { get; set; } // 0 for new
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Type { get; set; } // "Product" or "Service"
    }
}

