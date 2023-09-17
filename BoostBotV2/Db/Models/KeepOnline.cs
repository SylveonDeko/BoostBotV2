using System.ComponentModel.DataAnnotations;

namespace BoostBotV2.Db.Models;

public class KeepOnline
{
    [Key]
    public int Id { get; set; }
    public string Token { get; set; }
    public ulong UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}