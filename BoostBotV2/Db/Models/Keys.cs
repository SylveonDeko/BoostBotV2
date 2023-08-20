using System.ComponentModel.DataAnnotations;

namespace BoostBotV2.Db.Models;

public class Keys
{
    [Key]
    public int Id { get; set; }
    public string Key { get; set; }
    public string RoleName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}