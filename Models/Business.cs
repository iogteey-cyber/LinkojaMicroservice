using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace LinkojaMicroservice.Models
{
    public class Business
    {
        public int Id { get; set; }
        
        public int OwnerId { get; set; }
        public User Owner { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string LogoUrl { get; set; }
        
        public string CoverPhotoUrl { get; set; }
        
        public string Description { get; set; }
        
        public string Category { get; set; }
        
        public string Address { get; set; }

        // Structured location fields (used for search/filtering and permanent BusinessId generation)
        public string Area { get; set; }

        public string Road { get; set; }

        public string Street { get; set; }

        public double? Latitude { get; set; }
        
        public double? Longitude { get; set; }

        // Permanent Linkoja Business ID, e.g. "LNK0101001001". Generated once at creation and never changed.
        public string BusinessId { get; set; }

        // Separate from Status (pending/verified/rejected) - used for admin activate/deactivate toggling
        public bool IsActive { get; set; } = true;

        // New contact/branding fields
        [EmailAddress]
        public string email { get; set; }
        public string website { get; set; }
        
        public string Status { get; set; } = "pending"; // pending, verified, rejected
        
        public string VerificationDocUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<BusinessReview> Reviews { get; set; }
        public ICollection<BusinessFollower> Followers { get; set; }
        public ICollection<BusinessPost> Posts { get; set; }
        public ICollection<BusinessCategory> BusinessCategories { get; set; }
        public ICollection<BusinessProduct> Products { get; set; }
    }
}