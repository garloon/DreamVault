using System.Security.Claims;
using DreamVault.Api.Data;
using DreamVault.Api.Hubs;
using DreamVault.Api.Models;
using DreamVault.Api.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DreamVault.Api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _chatHubContext;

    public ChatController(AppDbContext context, IHubContext<ChatHub> chatHubContext)
    {
        _context = context;
        _chatHubContext = chatHubContext;
    }

    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var message = new Message
        {
            ShelterId = request.ShelterId,
            SenderId = userId,
            Content = request.Content,
            Timestamp = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return BadRequest("User not found");
        }

        await _chatHubContext.Clients.Group($"shelter-{request.ShelterId}").SendAsync("ReceiveMessage", new
        {
            SenderId = userId,
            Content = message.Content,
            Timestamp = message.Timestamp,
            Username = user.Username
        });

        return Ok();
    }

    [HttpGet("get-messages/{shelterId}")]
    public async Task<IActionResult> GetMessages(int shelterId)
    {
        /*var messages = await _context.Messages
            .Where(m => m.ShelterId == shelterId)
            .OrderBy(m => m.Timestamp)
            .Join(_context.Users,
                message => message.SenderId,
                user => user.Id,
                (message, user) => new
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    Username = user.Username
                })
            .ToListAsync();

        return Ok(messages);*/
        return Ok(new List<Message>());
    }
}