using System.ComponentModel.DataAnnotations;

namespace DreamVault.Api.ViewModels;

public class LoginViewModel
{
    [Required]
    public string? AccessCode { get; set; }
}