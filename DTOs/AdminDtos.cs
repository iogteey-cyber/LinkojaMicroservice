using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.DTOs
{
    public class ApproveBusinessRequest
    {
        [Required]
        public string Status { get; set; } // "verified" or "rejected"
        
        public string Reason { get; set; } // Rejection reason if applicable
    }

    public class BusinessAnalyticsDto
    {
        public int TotalBusinesses { get; set; }
        public int PendingBusinesses { get; set; }
        public int VerifiedBusinesses { get; set; }
        public int RejectedBusinesses { get; set; }
        public int TotalUsers { get; set; }
        public int TotalReviews { get; set; }
    }

    public class BusinessInsightsDto
    {
        public int ProfileViews { get; set; }
        public int FollowerCount { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int PostCount { get; set; }
    }
}
