using LinkojaMicroservice.Models;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                using (var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    client.EnableSsl = _smtpSettings.EnableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = isHtml
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Log the error (in production, use proper logging)
                Console.WriteLine($"Failed to send email to {toEmail}: {ex.Message}");
                // Don't throw to prevent blocking the main operation
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            var subject = "Password Reset Request - Linkoja";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Password Reset Request</h2>
                        <p>You have requested to reset your password for your Linkoja account.</p>
                        <p>Your password reset token is:</p>
                        <div style='background-color: #f4f4f4; padding: 15px; border-radius: 5px; font-size: 18px; font-weight: bold; text-align: center;'>
                            {resetToken}
                        </div>
                        <p>This token will expire in 1 hour.</p>
                        <p>If you didn't request this password reset, please ignore this email.</p>
                        <br/>
                        <p style='color: #666; font-size: 12px;'>
                            This is an automated message from Linkoja. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var subject = "Your Verification Code - Linkoja";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Phone Number Verification</h2>
                        <p>Your verification code for Linkoja is:</p>
                        <div style='background-color: #4CAF50; color: white; padding: 15px; border-radius: 5px; font-size: 24px; font-weight: bold; text-align: center; letter-spacing: 5px;'>
                            {otpCode}
                        </div>
                        <p>This code will expire in 10 minutes.</p>
                        <p>If you didn't request this code, please ignore this email.</p>
                        <br/>
                        <p style='color: #666; font-size: 12px;'>
                            This is an automated message from Linkoja. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendBusinessApprovalEmailAsync(string toEmail, string businessName, string status, string reason = null)
        {
            var subject = status == "verified" 
                ? $"Congratulations! Your Business '{businessName}' Has Been Approved - Linkoja" 
                : $"Business Registration Update - {businessName}";

            string body;
            if (status == "verified")
            {
                body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #4CAF50;'>✓ Business Approved!</h2>
                            <p>Great news! Your business <strong>{businessName}</strong> has been verified and approved.</p>
                            <p>Your business is now live on Linkoja and customers can find and connect with you.</p>
                            <div style='background-color: #f0f8ff; padding: 15px; border-left: 4px solid #4CAF50; margin: 20px 0;'>
                                <h3 style='margin-top: 0;'>Next Steps:</h3>
                                <ul>
                                    <li>Complete your business profile</li>
                                    <li>Add photos and descriptions</li>
                                    <li>Start posting updates</li>
                                    <li>Engage with your customers</li>
                                </ul>
                            </div>
                            <p>Welcome to the Linkoja community!</p>
                            <br/>
                            <p style='color: #666; font-size: 12px;'>
                                This is an automated message from Linkoja. Please do not reply to this email.
                            </p>
                        </div>
                    </body>
                    </html>
                ";
            }
            else
            {
                body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #f44336;'>Business Registration Update</h2>
                            <p>We've reviewed your business registration for <strong>{businessName}</strong>.</p>
                            <p>Unfortunately, we were unable to approve your business at this time.</p>
                            {(string.IsNullOrEmpty(reason) ? "" : $@"
                            <div style='background-color: #fff3cd; padding: 15px; border-left: 4px solid #f44336; margin: 20px 0;'>
                                <h3 style='margin-top: 0;'>Reason:</h3>
                                <p>{reason}</p>
                            </div>
                            ")}
                            <p>If you believe this is an error or have questions, please contact our support team.</p>
                            <br/>
                            <p style='color: #666; font-size: 12px;'>
                                This is an automated message from Linkoja. Please do not reply to this email.
                            </p>
                        </div>
                    </body>
                    </html>
                ";
            }

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to Linkoja!";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Welcome to Linkoja, {userName}!</h2>
                        <p>Thank you for joining Linkoja - your local business connection platform.</p>
                        <div style='background-color: #f0f8ff; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='margin-top: 0;'>Get Started:</h3>
                            <ul>
                                <li>Discover local businesses near you</li>
                                <li>Read reviews and ratings</li>
                                <li>Follow your favorite businesses</li>
                                <li>Connect with your community</li>
                            </ul>
                        </div>
                        <p>If you're a business owner, you can register your business and start connecting with customers today!</p>
                        <br/>
                        <p style='color: #666; font-size: 12px;'>
                            This is an automated message from Linkoja. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
