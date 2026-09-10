using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IValidator<RegisterRequest> _registerRequestValidator;
        private readonly IValidator<LoginRequest> _loginRequestValidator;
        private readonly IValidator<RefreshTokenRequest> _refreshTokenRequestValidator;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IValidator<RegisterRequest> registerRequestValidator, IValidator<LoginRequest> loginRequestValidator, IValidator<RefreshTokenRequest> refreshTokenRequestValidator)
        {
            _authService = authService;
            _logger = logger;
            _registerRequestValidator = registerRequestValidator;
            _loginRequestValidator = loginRequestValidator;
            _refreshTokenRequestValidator = refreshTokenRequestValidator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {

            var validationResult = _registerRequestValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }
       
            var tokenResponse = await _authService.RegisterAsync(request);
            return StatusCode(StatusCodes.Status201Created, tokenResponse);
        
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validationResult = _loginRequestValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }
           
            var tokenResponse = await _authService.LoginAsync(request);
            return Ok(tokenResponse);
        
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var validationResult = _refreshTokenRequestValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }
            
                var tokenResponse = await _authService.RefreshTokenAsync(request);
                return Ok(tokenResponse);
           
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var validationResult = _refreshTokenRequestValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            await _authService.LogoutAsync(request);
            return NoContent();
        }
    }
}