
namespace DreamVault.Api.Models;

public class Shelter
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<User>? Users { get; set; }
}