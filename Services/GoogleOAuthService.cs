using LinkojaMicroservice.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public class GoogleOAuthService : IGoogleOAuthService
    {
        private readonly GoogleOAuthSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleOAuthService> _logger;

        public GoogleOAuthService(IOptions<GoogleOAuthSettings> settings, HttpClient httpClient, ILogger<GoogleOAuthService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<GoogleUserInfo> ValidateTokenAsync(string idToken)
        {
            try
            {
                _logger.LogInformation("Validating Google OAuth token");

                var url = $"{_settings.TokenValidationUrl}?id_token={idToken}";
                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google token validation failed. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                    throw new InvalidOperationException("Invalid Google token");
                }

                // Parse the response
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                // Verify the audience (client ID)
                if (root.TryGetProperty("aud", out var aud))
                {
                    if (aud.GetString() != _settings.ClientId)
                    {
                        _logger.LogError("Token audience mismatch. Expected: {Expected}, Got: {Got}", 
                            _settings.ClientId, aud.GetString());
                        throw new InvalidOperationException("Invalid token audience");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Token does not contain audience");
                }

                // Extract user information
                var userInfo = new GoogleUserInfo
                {
                    Email = root.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                    Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                    GoogleId = root.TryGetProperty("sub", out var sub) ? sub.GetString() ?? "" : "",
                    EmailVerified = root.TryGetProperty("email_verified", out var verified) && 
                                   (verified.GetString() == "true" || verified.ValueKind == JsonValueKind.True),
                    Picture = root.TryGetProperty("picture", out var picture) ? picture.GetString() ?? "" : ""
                };

                if (string.IsNullOrEmpty(userInfo.Email) || string.IsNullOrEmpty(userInfo.GoogleId))
                {
                    throw new InvalidOperationException("Token does not contain required user information");
                }

                _logger.LogInformation("Google token validated successfully for email: {Email}", userInfo.Email);
                return userInfo;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Exception occurred while validating Google token");
                throw new InvalidOperationException("Failed to validate Google token", ex);
            }
        }
    }
}
