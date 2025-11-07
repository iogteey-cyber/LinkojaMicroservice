using LinkojaMicroservice.Data;
using LinkojaMicroservice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace LinkojaMicroservice.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IGoogleOAuthService _googleOAuthService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService, IGoogleOAuthService googleOAuthService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _googleOAuthService = googleOAuthService;
        }

        public async Task<User> Register(string email, string password, string phone, string name, string socialId)
        {
            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Create new user
            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Phone = phone,
                Name = name,
                Role = "user",
                AuthProvider = "local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SocialId = socialId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Name ?? "User");
            }
            catch
            {
                // Don't fail registration if email fails
            }

            return user;
        }

        public async Task<User> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            return user;
        }

        public string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GeneratePasswordResetToken(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // For security, don't reveal if email exists
                throw new InvalidOperationException("If the email exists, a reset link will be sent");
            }

            // Generate a secure random token
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_");

            // Create reset token entry
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // Send password reset email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, token);
            }
            catch
            {
                // Don't fail if email fails
            }

            return token;
        }

        public async Task<bool> ResetPassword(string token, string newPassword)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsUsed);

            if (resetToken == null)
            {
                throw new InvalidOperationException("Invalid or expired reset token");
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Reset token has expired");
            }

            // Update password
            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            resetToken.User.UpdatedAt = DateTime.UtcNow;

            // Mark token as used
            resetToken.IsUsed = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User> SocialLogin(string provider, string accessToken, string email, string name, string photoUrl)
        {
            // Validate the token with the provider's API
            if (provider.ToLower() == "google")
            {
                // Validate Google ID token
                var googleUserInfo = await _googleOAuthService.ValidateTokenAsync(accessToken);
                
                // Use validated information from Google
                email = googleUserInfo.Email;
                name = googleUserInfo.Name;
                photoUrl = googleUserInfo.Picture;
                
                // Check if user exists with this social provider
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email || u.SocialId == googleUserInfo.GoogleId);

                if (existingUser != null)
                {
                    // User exists, update if needed
                    if (existingUser.AuthProvider != provider || existingUser.SocialId != googleUserInfo.GoogleId)
                    {
                        existingUser.AuthProvider = provider;
                        existingUser.SocialId = googleUserInfo.GoogleId;
                        existingUser.Name = name;
                        existingUser.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    return existingUser;
                }

                // Create new user with Google social login
                var newUser = new User
                {
                    Email = email,
                    Name = name,
                    Role = "user",
                    AuthProvider = provider,
                    SocialId = googleUserInfo.GoogleId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password for social login
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Send welcome email
                try
                {
                    await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.Name ?? "User");
                }
                catch
                {
                    // Don't fail registration if email fails
                }

                return newUser;
            }
            else if (provider.ToLower() == "facebook" || provider.ToLower() == "apple")
            {
                // For Facebook and Apple, implement similar validation
                // For now, basic implementation without full validation
                // In production, validate Facebook and Apple tokens properly
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    // User exists, update if needed
                    if (user.AuthProvider != provider)
                    {
                        user.AuthProvider = provider;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    return user;
                }

                // Create new user with social login
                var newUser = new User
                {
                    Email = email,
                    Name = name,
                    Role = "user",
                    AuthProvider = provider,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password for social login
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Send welcome email
                try
                {
                    await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.Name ?? "User");
                }
                catch
                {
                    // Don't fail registration if email fails
                }

                return newUser;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported provider: {provider}");
            }
        }
    }
}
