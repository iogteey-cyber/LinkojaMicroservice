using LinkojaMicroservice.DTOs;
using LinkojaMicroservice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly IOtpService _otpService;

        public VerificationController(IOtpService otpService)
        {
            _otpService = otpService;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            try
            {
                await _otpService.SendOtp(request.PhoneNumber);
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "OTP sent successfully", null, true);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while sending OTP", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [Authorize]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var result = await _otpService.VerifyOtp(request.PhoneNumber, request.OtpCode);
                if (result)
                {
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "Phone number verified successfully", new { verified = true }, true);
                    return Ok(response);
                }
                else
                {
                    var response = ResponseStatus<object>.Create<BasicResponse<object>>("01", "Verification failed", null, false);
                    return BadRequest(response);
                }
            }
            catch (InvalidOperationException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("01", ex.Message, null, false);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred during verification", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] SendOtpRequest request)
        {
            try
            {
                await _otpService.ResendOtp(request.PhoneNumber);
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("00", "OTP resent successfully", null, true);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("01", ex.Message, null, false);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                var response = ResponseStatus<object>.Create<BasicResponse<object>>("99", "An error occurred while resending OTP", new { error = ex.Message }, false);
                return StatusCode(500, response);
            }
        }
    }
}
