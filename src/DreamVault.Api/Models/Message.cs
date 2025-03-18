namespace DreamVault.Api.Models;

public class Message
{
    public int Id { get; set; }
    public int ShelterId { get; set; }
    public int SenderId { get; set; }
    public string? Content { get; set; }
    public DateTime Timestamp { get; set; }
}