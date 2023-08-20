using System.ComponentModel.DataAnnotations;

namespace BoostBotV2.Db.Models;

public class PrivateStock
{
    [Key]
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public string Token { get; set; }
    public bool IsOnline { get; set; } = false;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    
}