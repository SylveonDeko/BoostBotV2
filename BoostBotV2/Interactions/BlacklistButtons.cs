using BoostBotV2.Common.Attributes.Interactions;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace BoostBotV2.Interactions;

public class BlacklistButtons : InteractionModuleBase
{
    private readonly DbService _db;

    public BlacklistButtons(DbService db)
    {
        _db = db;
    }

    [ComponentInteraction("bluser:*"), IsOwner]
    public async Task BlacklistUser(ulong userId)
    {
        await using var uow = _db.GetDbContext();
        var user = await uow.Blacklists.FirstOrDefaultAsync(x => x.BlacklistId == userId && x.BlacklistType == BlacklistType.User);
        if (user is not null)
        {
            await Context.Interaction.RespondAsync("User is already blacklisted.", ephemeral: true);
            return;
        }
        await uow.Blacklists.AddAsync(new Blacklists
        {
            BlacklistId = userId,
            BlacklistType = BlacklistType.User
        });
        await uow.SaveChangesAsync();
        await Context.Interaction.RespondAsync("User blacklisted.", ephemeral: true);
    }
    
    [ComponentInteraction("blguild:*"), IsOwner]
    public async Task BlacklistGuild(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        var guild = await uow.Blacklists.FirstOrDefaultAsync(x => x.BlacklistId == guildId && x.BlacklistType == BlacklistType.Guild);
        if (guild is not null)
        {
            await Context.Interaction.RespondAsync("Guild is already blacklisted.", ephemeral: true);
            return;
        }
        await uow.Blacklists.AddAsync(new Blacklists
        {
            BlacklistId = guildId,
            BlacklistType = BlacklistType.Guild
        });
        await uow.SaveChangesAsync();
        await Context.Interaction.RespondAsync("Guild blacklisted.", ephemeral: true);
    }
}