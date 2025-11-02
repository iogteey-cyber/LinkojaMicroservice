using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("User registration attempt for email: {Email}", request.Email);
            try
            {
                var user = await _authService.Register(request.Email, request.Password, request.Phone, request.Name);
                var token = _authService.GenerateJwtToken(user);

                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Phone = user.Phone,
                        Name = user.Name,
                        Role = user.Role
                    }
                };

                _logger.LogInformation("User registered successfully: {Email}, UserId: {UserId}", user.Email, user.Id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration failed for {Email}: {Message}", request.Email, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during registration", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _authService.Login(request.Email, request.Password);
                var token = _authService.GenerateJwtToken(user);

                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Phone = user.Phone,
                        Name = user.Name,
                        Role = user.Role
                    }
                };

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var token = await _authService.GeneratePasswordResetToken(request.Email);
                
                // In production, send this token via email
                // For now, return it in response (not secure for production)
                return Ok(new { message = "Password reset token generated", token = token });
            }
            catch (InvalidOperationException ex)
            {
                // Return success even if email doesn't exist (security best practice)
                return Ok(new { message = "If the email exists, a reset link will be sent" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPassword(request.Token, request.NewPassword);
                return Ok(new { message = "Password reset successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _authService.ChangePassword(userId, request.CurrentPassword, request.NewPassword);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest request)
        {
            try
            {
                var user = await _authService.SocialLogin(
                    request.Provider, 
                    request.AccessToken, 
                    request.Email, 
                    request.Name, 
                    request.PhotoUrl
                );
                
                var token = _authService.GenerateJwtToken(user);

                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Phone = user.Phone,
                        Name = user.Name,
                        Role = user.Role
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during social login", error = ex.Message });
            }
        }
    }
}
