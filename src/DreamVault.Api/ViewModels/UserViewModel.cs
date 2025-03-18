namespace DreamVault.Api.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string? Avatar { get; set; }
        public bool IsOnline { get; set; }
    }
}
