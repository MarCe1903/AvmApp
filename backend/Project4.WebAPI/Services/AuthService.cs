using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project4.WebAPI.Controllers;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Project4.WebAPI.Services
{
    public class AuthService : IAuthService
    {
        private const int MfaMaxAttempts = 5;
        private static readonly TimeSpan MfaLifetime = TimeSpan.FromMinutes(5);

        private readonly AppDbContext _ctx;
        private readonly IJwtService _jwt;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext ctx, IJwtService jwt, IEmailService emailService)
        {
            _ctx = ctx;
            _jwt = jwt;
            _emailService = emailService;
        }

        public async Task<LoginResponse?> RegisterAsync(RegisterRequest request)
        {
            var exists = await _ctx.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
            {
                return null;
            }

            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = CreatePasswordHasher().HashPassword(user, request.Password);

            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();

            return BuildLoginResponse(user);
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var email = NormalizeEmail(request.Email);
            var user = await FindUserByEmailAsync(email);
            if (user is null)
            {
                return null;
            }

            var isPasswordValid = await ValidatePasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return null;
            }

            if (user.MfaEnabled)
            {
                return await CreateMfaChallengeResponseAsync(user);
            }

            return BuildLoginResponse(user);
        }

        public async Task<LoginResponse?> VerifyMfaAsync(VerifyMfaRequest request)
        {
            var challenge = await _ctx.MfaChallenges
                .FirstOrDefaultAsync(x => x.ChallengeId == request.ChallengeId && !x.IsUsed);

            if (challenge is null || challenge.ExpiresAt < DateTime.UtcNow)
            {
                return null;
            }

            if (challenge.AttemptCount >= MfaMaxAttempts)
            {
                return null;
            }

            if (challenge.CodeHash != HashCode(request.Code))
            {
                challenge.AttemptCount += 1;
                await _ctx.SaveChangesAsync();
                return null;
            }

            challenge.IsUsed = true;
            await _ctx.SaveChangesAsync();

            var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == challenge.UserId);
            return user is null ? null : BuildLoginResponse(user);
        }

        public async Task<bool> ToggleMfaAsync(int userId, bool enabled)
        {
            var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return false;
            }

            user.MfaEnabled = enabled;
            await _ctx.SaveChangesAsync();
            return true;
        }

        private async Task<User?> FindUserByEmailAsync(string normalizedEmail)
        {
            return await _ctx.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        }

        private Task<bool> ValidatePasswordAsync(User user, string password)
        {
            var hasher = CreatePasswordHasher();
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return Task.FromResult(verify != PasswordVerificationResult.Failed);
        }

        private async Task<LoginResponse> CreateMfaChallengeResponseAsync(User user)
        {
            await CloseOpenChallengesAsync(user.Id);

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var challengeId = Guid.NewGuid().ToString("N");

            _ctx.MfaChallenges.Add(new MfaChallenge
            {
                ChallengeId = challengeId,
                UserId = user.Id,
                CodeHash = HashCode(code),
                ExpiresAt = DateTime.UtcNow.Add(MfaLifetime),
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            });

            await _ctx.SaveChangesAsync();

            await _emailService.SendAsync(
                user.Email,
                "AVM App giris dogrulama kodunuz",
                $"""
                <p>Merhaba {user.FullName},</p>
                <p>Giris dogrulama kodunuz:</p>
                <p style="font-size:28px;font-weight:700;letter-spacing:4px;">{code}</p>
                <p>Bu kod 5 dakika boyunca gecerlidir.</p>
                """);

            return new LoginResponse
            {
                FullName = user.FullName,
                Role = user.Role,
                Email = user.Email,
                RequiresMfa = true,
                MfaChallengeId = challengeId
            };
        }

        private async Task CloseOpenChallengesAsync(int userId)
        {
            var openChallenges = await _ctx.MfaChallenges
                .Where(x => x.UserId == userId && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var challenge in openChallenges)
            {
                challenge.IsUsed = true;
            }
        }

        private LoginResponse BuildLoginResponse(User user)
        {
            return new LoginResponse
            {
                Token = _jwt.GenerateToken(user),
                FullName = user.FullName,
                Role = user.Role,
                Email = user.Email,
                RequiresMfa = false,
                MfaChallengeId = null
            };
        }

        private static PasswordHasher<User> CreatePasswordHasher()
        {
            return new PasswordHasher<User>();
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string HashCode(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; }
        public bool MfaEnabled { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<Project4.Entities.Product> Products => Set<Project4.Entities.Product>();
        public DbSet<Project4.Entities.OrderStatus> OrderStatuses => Set<Project4.Entities.OrderStatus>();
        public DbSet<UserOrder> UserOrders => Set<UserOrder>();
        public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();
    }

    public class Favorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChatSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int SenderId { get; set; }
        public string SenderRole { get; set; } = "User";
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class UserOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int OrderStatusId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MfaChallenge
    {
        public int Id { get; set; }
        public string ChallengeId { get; set; } = default!;
        public int UserId { get; set; }
        public string CodeHash { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public int AttemptCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
