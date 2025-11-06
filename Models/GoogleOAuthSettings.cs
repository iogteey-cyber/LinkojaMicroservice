namespace LinkojaMicroservice.Models
{
    public class GoogleOAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenValidationUrl { get; set; } = "https://oauth2.googleapis.com/tokeninfo";
    }
}
