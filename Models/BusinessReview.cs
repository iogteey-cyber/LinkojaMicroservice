using System;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.Models
{
    public class BusinessReview
    {
        public int Id { get; set; }
        
        public int BusinessId { get; set; }
        public Business Business { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        public string Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
