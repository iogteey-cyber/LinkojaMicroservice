using System;

namespace LinkojaMicroservice.Models
{
    // Tracks the next 4-digit sequence number for a given Area/Road/Street code
    // combination, so permanent BusinessIds increment correctly per location.
    public class BusinessIdSequence
    {
        public int Id { get; set; }

        public string AreaCode { get; set; }

        public string RoadCode { get; set; }

        public string StreetCode { get; set; }

        public int NextSequence { get; set; } = 1;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
