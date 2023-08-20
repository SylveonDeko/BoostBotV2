using System.ComponentModel.DataAnnotations;

namespace BoostBotV2.Db.Models;

public class MemberFarmRegistry
{
    [Key]
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}