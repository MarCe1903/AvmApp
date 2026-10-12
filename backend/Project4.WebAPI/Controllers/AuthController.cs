using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project4.WebAPI.Extensions;
using Project4.WebAPI.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Project4.WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : AppControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _ctx;
        private readonly IJwtService _jwt;
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, AppDbContext ctx, IJwtService jwt, IConfiguration config)
        {
            _authService = authService;
            _ctx = ctx;
            _jwt = jwt;
            _config = config;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { message = "E-posta, sifre ve ad gereklidir." });
            }

            var response = await _authService.RegisterAsync(request);
            if (response is null)
            {
                return Conflict(new { message = "Bu e-posta ile kullanici zaten var." });
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "E-posta ve sifre gereklidir." });
            }

            var response = await _authService.LoginAsync(request);
            if (response is null)
            {
                return Unauthorized(new { message = "E-posta veya sifre hatali." });
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("google")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleComplete), new { returnUrl = returnUrl ?? "/" });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        [HttpGet("google/complete")]
        public async Task<IActionResult> GoogleComplete([FromQuery] string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync("External");
            if (!result.Succeeded || result.Principal is null)
            {
                return Redirect("/");
            }

            var email = result.Principal.GetEmailAddress();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Redirect("/");
            }

            var user = await FindOrCreateGoogleUserAsync(email, result.Principal.GetDisplayName());
            var token = _jwt.GenerateToken(user);

            await HttpContext.SignOutAsync("External");
            return Redirect(BuildFrontendRedirectUri(user, token, returnUrl));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { message = "Mevcut ve yeni sifre gerekli." });
            }

            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId.Value);
            if (user is null)
            {
                return NotFound();
            }

            var hasher = new PasswordHasher<User>();
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
            {
                return BadRequest(new { message = "Mevcut sifre hatali." });
            }

            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Sifre guncellendi." });
        }

        [AllowAnonymous]
        [HttpPost("verify-mfa")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
        {
            var response = await _authService.VerifyMfaAsync(request);
            if (response is null)
            {
                return Unauthorized(new { message = "Kod gecersiz veya suresi dolmus." });
            }

            return Ok(response);
        }

        [Authorize]
        [HttpPost("mfa/toggle")]
        public async Task<IActionResult> ToggleMfa([FromBody] ToggleMfaRequest request)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var ok = await _authService.ToggleMfaAsync(CurrentUserId.Value, request.Enabled);
            if (!ok)
            {
                return NotFound();
            }

            return Ok(new { message = request.Enabled ? "MFA aktif edildi." : "MFA kapatildi." });
        }

        private async Task<User> FindOrCreateGoogleUserAsync(string email, string fullName)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _ctx.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user is not null)
            {
                return user;
            }

            user = new User
            {
                Email = email,
                FullName = fullName,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, Guid.NewGuid().ToString("N"));
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();
            return user;
        }

        private string BuildFrontendRedirectUri(User user, string token, string? returnUrl)
        {
            var frontendBase = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
            var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/")
                ? "/"
                : returnUrl;

            return $"{frontendBase}{safeReturnUrl}?token={WebUtility.UrlEncode(token)}&fullName={WebUtility.UrlEncode(user.FullName)}&role={WebUtility.UrlEncode(user.Role)}&email={WebUtility.UrlEncode(user.Email)}";
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FullName { get; set; } = default!;
    }

    public class LoginResponse
    {
        public string? Token { get; set; }
        public string FullName { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string Email { get; set; } = default!;
        public bool RequiresMfa { get; set; }
        public string? MfaChallengeId { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
    }

    public class VerifyMfaRequest
    {
        public string ChallengeId { get; set; } = default!;
        public string Code { get; set; } = default!;
    }

    public class ToggleMfaRequest
    {
        public bool Enabled { get; set; }
    }
}
