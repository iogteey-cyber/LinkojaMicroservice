using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.DTOs
{
    public class SendOtpRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }
    }

    public class VerifyOtpRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string OtpCode { get; set; }
    }

    public class ReportReviewRequest
    {
        [Required]
        public string Reason { get; set; } // "spam", "inappropriate", "fake", "offensive"
        
        public string Description { get; set; }
    }

    public class ReviewReportDto
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int ReportedByUserId { get; set; }
        public string ReportedByName { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
