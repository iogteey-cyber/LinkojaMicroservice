using System;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.Models
{
    public class ReviewReport
    {
        public int Id { get; set; }
        
        public int ReviewId { get; set; }
        public BusinessReview Review { get; set; }
        
        public int ReportedByUserId { get; set; }
        public User ReportedBy { get; set; }
        
        [Required]
        public string Reason { get; set; } // "spam", "inappropriate", "fake", "offensive"
        
        public string Description { get; set; }
        
        public string Status { get; set; } = "pending"; // pending, reviewed, resolved, dismissed
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ResolvedAt { get; set; }
    }
}
