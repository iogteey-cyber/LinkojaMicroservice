using LinkojaMicroservice.Data;
using LinkojaMicroservice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<OtpService> _logger;

        public OtpService(ApplicationDbContext context, IEmailService emailService, ISmsService smsService, ILogger<OtpService> logger)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<bool> SendOtp(string phoneNumber)
        {
            _logger.LogInformation("Sending OTP to phone number: {PhoneNumber}", phoneNumber);
            
            // Generate a 6-digit OTP
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Check if there's an existing OTP for this phone number
            var existingOtp = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && !o.IsVerified)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingOtp != null)
            {
                // Update existing OTP
                existingOtp.OtpCode = otpCode;
                existingOtp.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                existingOtp.AttemptCount = 0;
                existingOtp.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new OTP
                var otp = new OtpVerification
                {
                    PhoneNumber = phoneNumber,
                    OtpCode = otpCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsVerified = false,
                    AttemptCount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OtpVerifications.Add(otp);
            }

            await _context.SaveChangesAsync();

            // Send OTP via SMS using Termii
            try
            {
                var smsMessage = $"Your Linkoja verification code is: {otpCode}. This code expires in 10 minutes.";
                var smsSent = await _smsService.SendSmsAsync(phoneNumber, smsMessage);
                
                if (smsSent)
                {
                    _logger.LogInformation("OTP sent successfully via SMS to {PhoneNumber}", phoneNumber);
                }
                else
                {
                    _logger.LogWarning("Failed to send OTP via SMS to {PhoneNumber}, will attempt email backup", phoneNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP via SMS to {PhoneNumber}", phoneNumber);
            }

            // Also send via email as backup (if user has email with this phone)
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == phoneNumber);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendOtpEmailAsync(user.Email, otpCode);
                    _logger.LogInformation("OTP sent via email backup to user with phone: {PhoneNumber}", phoneNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email backup for phone: {PhoneNumber}", phoneNumber);
            }

            return true;
        }

        public async Task<bool> VerifyOtp(string phoneNumber, string otpCode)
        {
            var otp = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && !o.IsVerified)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                throw new InvalidOperationException("No OTP found for this phone number");
            }

            if (otp.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("OTP has expired");
            }

            if (otp.AttemptCount >= 3)
            {
                throw new InvalidOperationException("Maximum verification attempts exceeded");
            }

            otp.AttemptCount++;

            if (otp.OtpCode != otpCode)
            {
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Invalid OTP code");
            }

            // Mark as verified
            otp.IsVerified = true;
            await _context.SaveChangesAsync();

            // Update user's phone verification status
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == phoneNumber);
            if (user != null)
            {
                user.IsPhoneVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> ResendOtp(string phoneNumber)
        {
            // Check if the last OTP was sent less than 60 seconds ago
            var lastOtp = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp != null && (DateTime.UtcNow - lastOtp.CreatedAt).TotalSeconds < 60)
            {
                throw new InvalidOperationException("Please wait before requesting a new OTP");
            }

            return await SendOtp(phoneNumber);
        }
    }
}
