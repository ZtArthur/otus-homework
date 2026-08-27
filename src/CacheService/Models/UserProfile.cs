using Generators.BinarySerializer;

namespace CacheService.Models;

[GenerateBinarySerializer]
public partial class UserProfile
{
    public int Id { get; set; }

    public string Username { get; set; }

    public DateTime CreatedAt { get; set; }
}