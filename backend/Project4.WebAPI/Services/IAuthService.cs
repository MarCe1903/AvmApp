using System.Threading.Tasks;
using Project4.WebAPI.Controllers;

namespace Project4.WebAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<LoginResponse?> RegisterAsync(RegisterRequest request);
        Task<LoginResponse?> VerifyMfaAsync(VerifyMfaRequest request);
        Task<bool> ToggleMfaAsync(int userId, bool enabled);
    }
}
