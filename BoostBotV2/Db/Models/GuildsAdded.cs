using System.ComponentModel.DataAnnotations;

namespace BoostBotV2.Db.Models;

public class GuildsAdded
{
    [Key]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public string? Token { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}