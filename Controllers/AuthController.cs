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
                var user = await _authService.Register(request.Email, request.Password, request.Phone, request.Name,request.SocialId);
                var token = _authService.GenerateJwtToken(user);

                var responseData = new AuthResponse
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

                var response = ResponseStatus<AuthResponse>.Create<BasicResponse<AuthResponse>>("00", "Registration successful", responseData, true);

                _logger.LogInformation("User registered successfully: {Email}, UserId: {UserId}", user.Email, user.Id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration failed for {Email}: {Message}", request.Email, ex.Message);
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("01", ex.Message, null, false);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred during registration", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _authService.Login(request.Email, request.Password);
                var token = _authService.GenerateJwtToken(user);

                var responseData = new AuthResponse
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
                var response = ResponseStatus<AuthResponse>.Create<BasicResponse<AuthResponse>>("00", "Login successful", responseData, true);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("02", ex.Message, null, false);
                return Unauthorized(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred during login", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var token = await _authService.GeneratePasswordResetToken(request.Email);
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Password reset token generated", new { token }, true);
                return Ok(response);
            }
            catch (InvalidOperationException)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "If the email exists, a reset link will be sent", null, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPassword(request.Token, request.NewPassword);
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Password reset successfully", null, true);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("01", ex.Message, null, false);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred", new { error = ex.Message }, false);
                return StatusCode(500, response);
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
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Password changed successfully", null, true);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("02", ex.Message, null, false);
                return Unauthorized(response);
            }
            catch (KeyNotFoundException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("04", ex.Message, null, false);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred", new { error = ex.Message }, false);
                return StatusCode(500, response);
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

                var responseData = new AuthResponse
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

                var response = ResponseStatus<AuthResponse>.Create<BasicResponse<AuthResponse>>("00", "Social login successful", responseData, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred during social login", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }
    }
}
