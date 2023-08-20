using System.ComponentModel.DataAnnotations;

namespace BoostBotV2.Db.Models;

public class NitroStock
{
    [Key]
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public string Token { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}