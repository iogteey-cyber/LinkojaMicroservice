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
        
        public double? Latitude { get; set; }
        
        public double? Longitude { get; set; }
        
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
    }
}