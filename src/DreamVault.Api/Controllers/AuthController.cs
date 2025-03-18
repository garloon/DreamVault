using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DreamVault.Api.Data;
using DreamVault.Api.Hubs;
using DreamVault.Api.Models;
using DreamVault.Api.Models.Requests;
using DreamVault.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DreamVault.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<ChatHub> _chatHubContext; // Inject ChatHub

    public AuthController(AppDbContext context, IConfiguration configuration, IHubContext<ChatHub> chatHubContext)
    {
        _context = context;
        _configuration = configuration;
        _chatHubContext = chatHubContext;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
    {
        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            return BadRequest("Username already exists");
        }

        string accessCode = GenerateAccessCode();
        while (await _context.Users.AnyAsync(u => u.AccessCode == accessCode))
        {
            accessCode = GenerateAccessCode();
        }

        var user = new User
        {
            Username = model.Username,
            AccessCode = accessCode,
            Gender = model.Gender,
            Avatar = model.Avatar,
            ShelterId = model.ShelterId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var shelters = await _context.Shelters.Select(s => new
        {
            Id = s.Id,
            Name = s.Name,
            UserCount = s.Users.Count
        }).ToListAsync();

        return Ok(new
        {
            Token = GenerateJwtToken(user),
            RefreshToken = GenerateRefreshToken(), // Generate refresh token on register
            User = new
            {
                Id = user.Id,
                Username = user.Username,
                AccessCode = user.AccessCode,
                Gender = user.Gender,
                Avatar = user.Avatar,
                ShelterId = user.ShelterId,
                IsOnline = user.IsOnline
            },
            Shelters = shelters
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.AccessCode == model.AccessCode);
        if (user == null)
        {
            return BadRequest("Invalid Access Code");
        }

        user.IsOnline = true;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["Jwt:RefreshTokenTTL"]));

        await _context.SaveChangesAsync();

        // Send online users to the joining user + notify the group about the new user
        var onlineUsers = await GetOnlineUsersForShelter(user.ShelterId);
        await _chatHubContext.Clients.Group($"shelter-{user.ShelterId}").SendAsync("UpdateOnlineUsers", onlineUsers);

        return Ok(new
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            User = new
            {
                Id = user.Id,
                Username = user.Username,
                AccessCode = user.AccessCode,
                Gender = user.Gender,
                Avatar = user.Avatar,
                ShelterId = user.ShelterId,
                IsOnline = user.IsOnline
            }
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return BadRequest("User not found");
        }

        user.IsOnline = false;
        await _context.SaveChangesAsync();

        // Notify other users about the user logging out
        var onlineUsers = await GetOnlineUsersForShelter(user.ShelterId);
        await _chatHubContext.Clients.Group($"shelter-{user.ShelterId}").SendAsync("UpdateOnlineUsers", onlineUsers);

        return Ok();
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
        {
            return BadRequest("Invalid token");
        }

        var username = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.AccessCode == username && u.RefreshToken == request.RefreshToken);
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return BadRequest("Invalid client request");
        }

        var newJwtToken = GenerateJwtToken(user);
        var newRefreshToken = HashRefreshToken(GenerateRefreshToken());
        user.RefreshToken = newRefreshToken;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Token = newJwtToken,
            RefreshToken = newRefreshToken,
            User = new
            {
                Id = user.Id,
                Username = user.Username,
                AccessCode = user.AccessCode,
                Gender = user.Gender,
                Avatar = user.Avatar,
                ShelterId = user.ShelterId,
                IsOnline = user.IsOnline
            }
        });
    }

    [HttpGet("shelters")]
    public async Task<IActionResult> GetShelters()
    {
        var shelters = await _context.Shelters.Select(s => new
        {
            Id = s.Id,
            Name = s.Name,
            UserCount = s.Users.Count
        }).ToListAsync();

        return Ok(shelters);
    }

    [HttpPost("change-shelter")]
    public async Task<IActionResult> ChangeShelter([FromBody] ChangeShelterRequest model)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return BadRequest("User not found");
        }

        var oldShelterId = user.ShelterId; // remember shelter before change

        var newShelter = await _context.Shelters.FindAsync(model.ShelterId);
        if (newShelter == null)
        {
            return BadRequest("Shelter not found");
        }

        user.IsOnline = false;
        user.ShelterId = newShelter.Id;
        user.IsOnline = true;
        await _context.SaveChangesAsync();

        // Get updated online users
        var onlineUsersOldShelter = await GetOnlineUsersForShelter(oldShelterId);
        var onlineUsersNewShelter = await GetOnlineUsersForShelter(user.ShelterId);

        // Notify groups about the change
        await _chatHubContext.Clients.Group($"shelter-{oldShelterId}").SendAsync("UpdateOnlineUsers", onlineUsersOldShelter);
        await _chatHubContext.Clients.Group($"shelter-{user.ShelterId}").SendAsync("UpdateOnlineUsers", onlineUsersNewShelter);

        return Ok(new
        {
            ShelterId = user.ShelterId,
            ShelterName = newShelter.Name
        });
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.AccessCode),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["Jwt:TokenExpiryMinutes"])),
            SigningCredentials = creds,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        ClaimsPrincipal? principal;

        try
        {
            principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
        }
        catch (Exception)
        {
            return null;
        }

        return principal;
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    private string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GenerateAccessCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async Task<List<UserViewModel>> GetOnlineUsersForShelter(int? shelterId)
    {
        if (shelterId == null) return new List<UserViewModel>();

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