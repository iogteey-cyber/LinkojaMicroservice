namespace LinkojaMicroservice.Models
{
    public class TermiiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SenderId { get; set; } = "Linkoja";
        public string ApiUrl { get; set; } = "https://api.ng.termii.com/api/sms/send";
        public string Channel { get; set; } = "generic";
    }
}
