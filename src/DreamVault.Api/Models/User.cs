using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DreamVault.Api.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    public string Username { get; set; }

    [Required]
    public string AccessCode { get; set; }

    public string? Gender { get; set; }
    public string? Avatar { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public int? ShelterId { get; set; }

    [ForeignKey("ShelterId")]
    public Shelter? Shelter { get; set; }

    public bool IsOnline { get; set; } = false;
}