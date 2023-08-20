using System.ComponentModel.DataAnnotations;

namespace BoostBotV2.Db.Models;

public class Blacklists
{
    [Key]
    public int Id { get; set; }
    public ulong BlacklistId { get; set; }
    public BlacklistType BlacklistType { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}

public enum BlacklistType
{
    User,
    Guild
}