using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project4.WebAPI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Project4.WebAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FavoritesController : AppControllerBase
    {
        private readonly AppDbContext _ctx;

        public FavoritesController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var favorites = await _ctx.Favorites
                .Where(favorite => favorite.UserId == CurrentUserId.Value)
                .OrderByDescending(favorite => favorite.CreatedAt)
                .Select(favorite => favorite.ProductId)
                .ToListAsync();

            return Ok(favorites);
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> AddFavorite(int productId)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var exists = await _ctx.Favorites
                .AnyAsync(favorite => favorite.UserId == CurrentUserId.Value && favorite.ProductId == productId);
            if (exists)
            {
                return Ok(new { message = "Favori mevcut." });
            }

            _ctx.Favorites.Add(new Favorite
            {
                UserId = CurrentUserId.Value,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            });

            await _ctx.SaveChangesAsync();
            return Ok(new { message = "Favoriye eklendi." });
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFavorite(int productId)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var favorite = await _ctx.Favorites
                .FirstOrDefaultAsync(item => item.UserId == CurrentUserId.Value && item.ProductId == productId);
            if (favorite is null)
            {
                return NotFound();
            }

            _ctx.Favorites.Remove(favorite);
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Favoriden cikarildi." });
        }
    }
}
