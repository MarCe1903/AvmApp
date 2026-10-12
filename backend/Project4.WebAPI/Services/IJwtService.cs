namespace Project4.WebAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }

    public class JwtSettings
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Secret { get; set; } = default!;
        public int AccessTokenMinutes { get; set; } = 60;
    }
}