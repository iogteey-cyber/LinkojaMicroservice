using System;

namespace LinkojaMicroservice.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        public string Token { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
