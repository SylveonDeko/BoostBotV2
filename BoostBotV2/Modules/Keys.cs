using BoostBotV2.Common;
using BoostBotV2.Common.Attributes.TextCommands;
using BoostBotV2.Db;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace BoostBotV2.Modules;

public class Keys : BoostModuleBase
{
    private readonly DbService _db;

    public Keys(DbService db)
    {
        _db = db;
    }

    [Command("genkeys")]
    [Summary("Generates keys for a role.")]
    [Usage("genkeys <role name> <amount>")]
    [IsOwner]
    public async Task GenKeysAsync(string rolename, int amount)
    {
        await using var uow = _db.GetDbContext();
        // values and keys
        var keysandvalues = new Dictionary<string, string>();
        for (var i = 0; i < amount; i++)
        {
            var key = Extensions.GenerateSecureString(12);
            keysandvalues.Add(key, rolename);
        }
        
        await uow.Keys.AddRangeAsync(keysandvalues.Select(x => new Db.Models.Keys
        {
            Key = x.Key,
            RoleName = x.Value
        }));
        
        await uow.SaveChangesAsync();
        var fileName = $"{rolename}-keys.txt";

        using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, leaveOpen: true);
        foreach(var kv in keysandvalues)
        {
            await writer.WriteLineAsync($"Key: {kv.Key}, RoleName: {kv.Value}");
        }
        await writer.FlushAsync();

        ms.Position = 0;

        // Send file to Discord
        await Context.Channel.SendFileAsync(ms, fileName);
    }
    
    [Command("redeemkey")]
    [Summary("Redeems a key.")]
    public async Task RedeemKeyAsync(string key)
    {
        await using var uow = _db.GetDbContext();
        var dbkey = await uow.Keys.FirstOrDefaultAsync(x => x.Key == key);
        if (dbkey is null)
        {
            await ReplyAsync("Invalid key.");
            return;
        }

        var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == dbkey.RoleName);
        if (role is null)
        {
            await ReplyAsync("Invalid role.");
            return;
        }
        
        uow.Keys.Remove(dbkey);
        await uow.SaveChangesAsync();
        
        var user = Context.User as IGuildUser;
        await user.AddRoleAsync(role);
        await ReplyAsync("Role added.");
    }
}