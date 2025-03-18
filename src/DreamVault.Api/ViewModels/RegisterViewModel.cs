using System.ComponentModel.DataAnnotations;

namespace DreamVault.Api.ViewModels;

public class RegisterViewModel
{
    [Required]
    public string Username { get; set; } // Добавляем логин

    public string? Gender { get; set; }
    public string? Avatar { get; set; }
    public int? ShelterId { get; set; }
}