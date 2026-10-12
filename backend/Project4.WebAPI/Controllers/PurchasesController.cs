using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project4.WebAPI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Project4.WebAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PurchasesController : AppControllerBase
    {
        private const string PreparingStatusCode = "preparing";
        private const string OnRoadStatusCode = "on_road";
        private const string DeliveredStatusCode = "delivered";
        private static readonly TimeSpan StatusAdvanceDelay = TimeSpan.FromSeconds(10);

        private readonly AppDbContext _ctx;
        private readonly IServiceScopeFactory _scopeFactory;

        public PurchasesController(AppDbContext ctx, IServiceScopeFactory scopeFactory)
        {
            _ctx = ctx;
            _scopeFactory = scopeFactory;
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> Purchase(int productId)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var product = await _ctx.Products.FirstOrDefaultAsync(item => item.ProductId == productId);
            if (product is null)
            {
                return NotFound(new { message = "Urun bulunamadi." });
            }

            if (product.UnitsInStock <= 0)
            {
                return BadRequest(new { message = "Stokta urun kalmadi." });
            }

            var statusId = await GetStatusIdAsync(_ctx, PreparingStatusCode);
            if (statusId is null)
            {
                return StatusCode(500, new { message = "Order status bulunamadi." });
            }

            product.UnitsInStock -= 1;

            var order = new UserOrder
            {
                UserId = CurrentUserId.Value,
                ProductId = product.ProductId,
                OrderStatusId = statusId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _ctx.UserOrders.Add(order);
            await _ctx.SaveChangesAsync();

            _ = Task.Run(() => AdvanceOrderStatusAsync(order.Id));
            return Ok(new { message = "Satin alma basarili.", orderId = order.Id });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var orders = await _ctx.UserOrders
                .Where(order => order.UserId == CurrentUserId.Value)
                .Join(_ctx.Products, order => order.ProductId, product => product.ProductId, (order, product) => new { order, product })
                .Join(_ctx.OrderStatuses, item => item.order.OrderStatusId, status => status.Id, (item, status) => new
                {
                    item.order.Id,
                    item.order.CreatedAt,
                    item.order.ProductId,
                    ProductName = item.product.ProductName,
                    UnitPrice = item.product.UnitPrice,
                    StatusCode = status.Code,
                    StatusDescription = status.Description
                })
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();

            return Ok(orders);
        }

        private async Task<int?> GetStatusIdAsync(AppDbContext db, string code)
        {
            var normalizedCode = code.ToLower();
            var status = await db.OrderStatuses.FirstOrDefaultAsync(item => item.Code.ToLower() == normalizedCode);
            return status?.Id;
        }

        private async Task AdvanceOrderStatusAsync(int orderId)
        {
            await Task.Delay(StatusAdvanceDelay);
            await UpdateStatusAsync(orderId, OnRoadStatusCode);

            await Task.Delay(StatusAdvanceDelay);
            await UpdateStatusAsync(orderId, DeliveredStatusCode);
        }

        private async Task UpdateStatusAsync(int orderId, string statusCode)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var order = await db.UserOrders.FirstOrDefaultAsync(item => item.Id == orderId);
            if (order is null)
            {
                return;
            }

            var statusId = await GetStatusIdAsync(db, statusCode);
            if (statusId is null)
            {
                return;
            }

            order.OrderStatusId = statusId.Value;
            await db.SaveChangesAsync();

            if (statusCode is not (OnRoadStatusCode or DeliveredStatusCode))
            {
                return;
            }

            await SendStatusEmailAsync(db, emailService, order, statusId.Value, statusCode);
        }

        private static async Task SendStatusEmailAsync(
            AppDbContext db,
            IEmailService emailService,
            UserOrder order,
            int statusId,
            string statusCode)
        {
            try
            {
                var userEmail = await db.Users
                    .Where(user => user.Id == order.UserId)
                    .Select(user => user.Email)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return;
                }

                var productName = await db.Products
                    .Where(product => product.ProductId == order.ProductId)
                    .Select(product => product.ProductName)
                    .FirstOrDefaultAsync();

                var statusDescription = await db.OrderStatuses
                    .Where(status => status.Id == statusId)
                    .Select(status => status.Description)
                    .FirstOrDefaultAsync();

                var email = OrderStatusEmailComposer.Create(
                    order.Id,
                    productName ?? "Siparisiniz",
                    statusDescription ?? statusCode,
                    statusCode);

                await emailService.SendAsync(userEmail, email.Subject, email.Body);
                Console.WriteLine($"[mail] Order {order.Id} status={statusCode}, to={userEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[mail] send failed for order {order.Id}, status={statusCode}: {ex.Message}");
            }
        }
    }
}
