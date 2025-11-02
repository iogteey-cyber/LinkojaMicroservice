using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.DTOs
{
    public class SocialLoginRequest
    {
        [Required]
        public string Provider { get; set; } // "google", "facebook", "apple"
        
        [Required]
        public string AccessToken { get; set; }
        
        public string Email { get; set; }
        
        public string Name { get; set; }
        
        public string PhotoUrl { get; set; }
    }
}
