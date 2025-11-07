using System;

namespace LinkojaMicroservice.Models
{
    public class BusinessFollower
    {
        public int Id { get; set; }
        
        public int BusinessId { get; set; }
        public Business Business { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    }
}
