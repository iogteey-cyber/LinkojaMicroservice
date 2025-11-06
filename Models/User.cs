using System.ComponentModel.DataAnnotations;
using System;

namespace LinkojaMicroservice.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required, EmailAddress]
        public string Email { get; set; }
        
        public string Phone { get; set; }
        
        public bool IsPhoneVerified { get; set; } = false;
        
        [Required]
        public string PasswordHash { get; set; }
        
        public string Name { get; set; }
        
        public string Role { get; set; } = "user"; // user, business_owner, admin
        
        public string AuthProvider { get; set; } // "local", "google", "facebook", "apple"
        
        public string SocialId { get; set; } // ID from social provider
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}