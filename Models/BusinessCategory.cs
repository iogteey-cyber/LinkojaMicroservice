using System;

namespace LinkojaMicroservice.Models
{
    public class BusinessCategory
    {
        public int Id { get; set; }
        
        public int BusinessId { get; set; }
        public Business Business { get; set; }
        
        public string CategoryName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
