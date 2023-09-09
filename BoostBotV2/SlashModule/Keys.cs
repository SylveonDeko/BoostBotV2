using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.Interactions;
using BoostBotV2.Db;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace BoostBotV2.SlashModule;

[Group("keys", "Key Commands.")]
public class Keys : BoostInteractionModuleBase
{
    private readonly DbService _db;

    public Keys(DbService db)
    {
        _db = db;
    }

    [SlashCommand("gen", "Generates keys for a role.")]
    [IsOwner]
    public async Task GenKeysAsync(IRole role, int amount)
    {
        await DeferAsync();
        await using var uow = _db.GetDbContext();
        // values and keys
        var keysandvalues = new Dictionary<string, string>();
        for (var i = 0; i < amount; i++)
        {
            var key = Extensions.GenerateSecureString(12);
            keysandvalues.Add(key, role.Name);
        }
        
        await uow.Keys.AddRangeAsync(keysandvalues.Select(x => new Db.Models.Keys
        {
            Key = x.Key,
            RoleName = x.Value
        }));
        
        await uow.SaveChangesAsync();
        var fileName = $"{role.Name}-keys.txt";

        using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, leaveOpen: true);
        foreach(var kv in keysandvalues)
        {
            await writer.WriteLineAsync($"Key: {kv.Key}, RoleName: {kv.Value}");
        }
        await writer.FlushAsync();

        ms.Position = 0;
        
        await Context.Interaction.RespondWithFileAsync(ms, fileName);
    }
    
    [SlashCommand("redeem", "Redeems a key.")]
    public async Task RedeemKeyAsync(string key)
    {
        await DeferAsync();
        await using var uow = _db.GetDbContext();
        var dbkey = await uow.Keys.FirstOrDefaultAsync(x => x.Key == key);
        if (dbkey is null)
        {
            await Context.Interaction.SendErrorAsync("Invalid key.");
            return;
        }

        var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == dbkey.RoleName);
        if (role is null)
        {
            await Context.Interaction.SendErrorAsync("Invalid role.");
            return;
        }
        
        uow.Keys.Remove(dbkey);
        await uow.SaveChangesAsync();
        
        var user = Context.User as IGuildUser;
        await user.AddRoleAsync(role);
        await Context.Interaction.SendConfirmAsync("Role added.");
    }
}