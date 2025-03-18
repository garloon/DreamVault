namespace DreamVault.Api.Models.Requests;

public class SendMessageRequest
{
    public int ShelterId { get; set; }
    public string? Content { get; set; }
}