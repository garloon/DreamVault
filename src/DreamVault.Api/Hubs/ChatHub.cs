using DreamVault.Api.Data;
using DreamVault.Api.Models;
using DreamVault.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DreamVault.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinShelter(int shelterId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"shelter-{shelterId}");
        // Send initial online users to the new connected client.
        await Clients.Caller.SendAsync("UpdateOnlineUsers", await GetOnlineUsersForShelter(shelterId));
    }

    public async Task LeaveShelter(int shelterId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"shelter-{shelterId}");
    }

    private async Task<List<UserViewModel>> GetOnlineUsersForShelter(int shelterId)
    {
        return await _context.Users
            .Where(u => u.ShelterId == shelterId && u.IsOnline)
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Avatar = u.Avatar,
                IsOnline = u.IsOnline
            })
            .ToListAsync();
    }
}