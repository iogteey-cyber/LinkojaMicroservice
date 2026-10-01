using System;

namespace LinkojaMicroservice.Models
{
    // Deterministic lookup table that assigns a stable 2-digit code to each distinct
    // Area/Road/Street value, used to build the permanent Linkoja BusinessId.
    public class LocationCode
    {
        public int Id { get; set; }

        // "Area", "Road", or "Street"
        public string Type { get; set; }

        // Normalized (trimmed, lower-cased) location value
        public string Value { get; set; }

        // 2-digit zero-padded code, e.g. "01"
        public string Code { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
