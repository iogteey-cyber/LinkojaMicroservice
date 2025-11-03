using LinkojaMicroservice.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public class TermiiSmsService : ISmsService
    {
        private readonly TermiiSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TermiiSmsService> _logger;

        public TermiiSmsService(IOptions<TermiiSettings> settings, HttpClient httpClient, ILogger<TermiiSmsService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                _logger.LogInformation("Sending SMS to {PhoneNumber} via Termii", phoneNumber);

                // Ensure phone number is in correct format (with country code)
                var formattedPhone = FormatPhoneNumber(phoneNumber);

                var payload = new
                {
                    to = formattedPhone,
                    from = _settings.SenderId,
                    sms = message,
                    type = "plain",
                    channel = _settings.Channel,
                    api_key = _settings.ApiKey
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.ApiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}. Response: {Response}", phoneNumber, responseContent);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {StatusCode}, Response: {Response}", 
                        phoneNumber, response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove any spaces, dashes, or special characters
            phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // If phone number doesn't start with + or country code, assume Nigeria (+234)
            if (!phoneNumber.StartsWith("+"))
            {
                if (phoneNumber.StartsWith("0"))
                {
                    // Replace leading 0 with +234 for Nigeria
                    phoneNumber = "+234" + phoneNumber.Substring(1);
                }
                else if (!phoneNumber.StartsWith("234"))
                {
                    // Add +234 if not present
                    phoneNumber = "+234" + phoneNumber;
                }
                else
                {
                    // Just add + if 234 is already there
                    phoneNumber = "+" + phoneNumber;
                }
            }

            return phoneNumber;
        }
    }
}
