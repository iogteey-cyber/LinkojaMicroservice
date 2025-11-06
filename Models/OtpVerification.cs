using System;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.Models
{
    public class OtpVerification
    {
        public int Id { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        public string OtpCode { get; set; }
        
        public bool IsVerified { get; set; } = false;
        
        public DateTime ExpiresAt { get; set; }
        
        public int AttemptCount { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
