namespace LinkojaMicroservice.Models
{
    // Controls how OTP verification codes are delivered. Email is the default
    // channel (no per-message cost, faster/more reliable delivery than SMS in
    // many markets); SMS remains available as a fallback for phone-only users
    // or via explicit configuration.
    public class OtpSettings
    {
        // When true (default), OTP codes are sent via email whenever the
        // target phone number is linked to a user with an email address.
        // SMS is only used as a fallback when no email is available.
        public bool PreferEmail { get; set; } = true;
    }
}
