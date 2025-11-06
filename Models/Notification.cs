using System;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        [Required]
        public string Type { get; set; } // "follower", "review", "approval", "comment"
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public int? RelatedBusinessId { get; set; }
        public Business RelatedBusiness { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
