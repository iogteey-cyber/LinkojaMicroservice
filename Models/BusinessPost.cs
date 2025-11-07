using System;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.Models
{
    public class BusinessPost
    {
        public int Id { get; set; }
        
        public int BusinessId { get; set; }
        public Business Business { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public string ImageUrl { get; set; }
        
        public string VideoUrl { get; set; }
        
        public int Likes { get; set; } = 0;
        
        public int Comments { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
