using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project4.WebAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project4.WebAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    public class UsersController : AppControllerBase
    {
        private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "User",
            "Seller"
        };

        private readonly AppDbContext _ctx;

        public UsersController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _ctx.Users
                .Select(user => new UserListItem
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleRequest request)
        {
            var normalizedRole = NormalizeRole(request?.Role);
            if (normalizedRole is null)
            {
                return BadRequest(new { message = "Role must be Admin, User, or Seller." });
            }

            var user = await _ctx.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            user.Role = normalizedRole;
            await _ctx.SaveChangesAsync();
            return Ok(new { message = "Role updated." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _ctx.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            _ctx.Users.Remove(user);
            await _ctx.SaveChangesAsync();
            return Ok(new { message = "User deleted." });
        }

        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            var trimmedRole = role.Trim();
            if (!AllowedRoles.Contains(trimmedRole))
            {
                return null;
            }

            return trimmedRole.ToLowerInvariant() switch
            {
                "admin" => "Admin",
                "seller" => "Seller",
                _ => "User"
            };
        }

        public class UserListItem
        {
            public int Id { get; set; }
            public string Email { get; set; } = default!;
            public string FullName { get; set; } = default!;
            public string Role { get; set; } = default!;
        }

        public class UpdateUserRoleRequest
        {
            public string Role { get; set; } = default!;
        }
    }
}
