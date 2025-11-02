using System;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.DTOs
{
    public class CreateBusinessRequest
    {
        [Required]
        public string Name { get; set; }

        public string LogoUrl { get; set; }
        
        public string CoverPhotoUrl { get; set; }
        
        public string Description { get; set; }
        
        public string Category { get; set; }
        
        public string Address { get; set; }
        
        public double? Latitude { get; set; }
        
        public double? Longitude { get; set; }
        
        public string VerificationDocUrl { get; set; }
    }

    public class UpdateBusinessRequest
    {
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string CoverPhotoUrl { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class BusinessDto
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string CoverPhotoUrl { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Status { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int FollowerCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateReviewRequest
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        public string Comment { get; set; }
    }

    public class CreatePostRequest
    {
        [Required]
        public string Content { get; set; }
        
        public string ImageUrl { get; set; }
        
        public string VideoUrl { get; set; }
    }
}
