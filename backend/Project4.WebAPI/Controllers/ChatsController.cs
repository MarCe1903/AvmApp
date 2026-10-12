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
    public class ChatsController : AppControllerBase
    {
        private readonly AppDbContext _ctx;

        public ChatsController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public async Task<IActionResult> GetSessions()
        {
            if (!IsSellerUser)
            {
                return Forbid();
            }

            var sessions = await _ctx.ChatSessions
                .Join(_ctx.Users,
                    session => session.UserId,
                    user => user.Id,
                    (session, user) => new ChatSessionListItem
                    {
                        Id = session.Id,
                        UserId = session.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        LastMessageAt = session.LastMessageAt ?? session.CreatedAt
                    })
                .OrderByDescending(session => session.LastMessageAt)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMySession()
        {
            if (IsSellerUser)
            {
                return Forbid();
            }

            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var session = await GetLatestUserSessionAsync(CurrentUserId.Value);
            if (session is null)
            {
                return NotFound();
            }

            return Ok(MapSession(session));
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start()
        {
            if (IsSellerUser)
            {
                return Forbid();
            }

            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var session = await GetLatestUserSessionAsync(CurrentUserId.Value);
            if (session is null)
            {
                session = new ChatSession
                {
                    UserId = CurrentUserId.Value,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };

                _ctx.ChatSessions.Add(session);
                await _ctx.SaveChangesAsync();
            }

            return Ok(MapSession(session));
        }

        [HttpGet("{sessionId}/messages")]
        public async Task<IActionResult> GetMessages(int sessionId)
        {
            if (!await CanAccessSessionAsync(sessionId))
            {
                return Forbid();
            }

            var messages = await _ctx.ChatMessages
                .Where(message => message.SessionId == sessionId)
                .OrderBy(message => message.SentAt)
                .Select(message => new ChatMessageDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderRole = message.SenderRole,
                    Text = message.Text,
                    SentAt = message.SentAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("{sessionId}/messages")]
        public async Task<IActionResult> SendMessage(int sessionId, [FromBody] ChatMessageRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { message = "Mesaj bos olamaz." });
            }

            if (!await CanAccessSessionAsync(sessionId))
            {
                return Forbid();
            }

            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var message = new ChatMessage
            {
                SessionId = sessionId,
                SenderId = CurrentUserId.Value,
                SenderRole = IsSellerUser ? "Seller" : "User",
                Text = request.Text.Trim(),
                SentAt = DateTime.UtcNow
            };

            _ctx.ChatMessages.Add(message);

            var session = await _ctx.ChatSessions.FirstOrDefaultAsync(item => item.Id == sessionId);
            if (session is not null)
            {
                session.LastMessageAt = message.SentAt;
            }

            await _ctx.SaveChangesAsync();
            return Ok(new { message = "Mesaj gonderildi." });
        }

        [HttpPost("{sessionId}/close")]
        public async Task<IActionResult> CloseChat(int sessionId)
        {
            if (!await CanAccessSessionAsync(sessionId))
            {
                return Forbid();
            }

            var session = await _ctx.ChatSessions.FirstOrDefaultAsync(item => item.Id == sessionId);
            if (session is null)
            {
                return NotFound();
            }

            var messages = _ctx.ChatMessages.Where(message => message.SessionId == sessionId);
            _ctx.ChatMessages.RemoveRange(messages);
            _ctx.ChatSessions.Remove(session);
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Sohbet kapatildi." });
        }

        private Task<ChatSession?> GetLatestUserSessionAsync(int userId)
        {
            return _ctx.ChatSessions
                .OrderByDescending(session => session.CreatedAt)
                .FirstOrDefaultAsync(session => session.UserId == userId);
        }

        private ChatSessionDto MapSession(ChatSession session)
        {
            return new ChatSessionDto
            {
                Id = session.Id,
                UserId = session.UserId
            };
        }

        private async Task<bool> CanAccessSessionAsync(int sessionId)
        {
            if (IsSellerUser)
            {
                return true;
            }

            if (CurrentUserId is null)
            {
                return false;
            }

            return await _ctx.ChatSessions
                .AnyAsync(session => session.Id == sessionId && session.UserId == CurrentUserId.Value);
        }
    }

    public class ChatSessionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
    }

    public class ChatSessionListItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
    }

    public class ChatMessageRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderRole { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
