using System;
using System.ComponentModel.DataAnnotations;

namespace LinkojaMicroservice.Models
{
    // Purely descriptive catalogue entry for a business - Linkoja is not an e-commerce
    // platform, so there is no price, cart, checkout, or order logic here.
    public class BusinessProduct
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }
        public Business Business { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string PhotoUrl { get; set; }

        // "Product" or "Service"
        public string Type { get; set; } = "Product";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
