using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace BoostBotV2.Common;

public class BoostModuleBase : ModuleBase
{
    public async Task<bool> PromptUserConfirmAsync(string message, ulong userid)
        => await PromptUserConfirmAsync(new EmbedBuilder().WithColor(Color.Green).WithDescription(message), userid).ConfigureAwait(false);

    public async Task<bool> PromptUserConfirmAsync(EmbedBuilder embed, ulong userid)
    {
        embed.WithColor(Color.Green);
        var buttons = new ComponentBuilder().WithButton("Yes", "yes", ButtonStyle.Success).WithButton("No", "no", ButtonStyle.Danger);
        var msg = await Context.Channel.SendMessageAsync(embed: embed.Build(), components: buttons.Build()).ConfigureAwait(false);
        try
        {
            var input = await GetButtonInputAsync(msg.Channel.Id, msg.Id, userid).ConfigureAwait(false);

            return input == "Yes";
        }
        finally
        {
            _ = Task.Run(async () => await msg.DeleteAsync().ConfigureAwait(false));
        }
    }

    public async Task<string?> GetButtonInputAsync(ulong channelId, ulong msgId, ulong userId, bool alreadyDeferred = false)
    {
        var userInputTask = new TaskCompletionSource<string>();
        var dsc = (DiscordSocketClient)Context.Client;
        try
        {
            dsc.InteractionCreated += Interaction;
            if (await Task.WhenAny(userInputTask.Task, Task.Delay(30000)).ConfigureAwait(false) !=
                userInputTask.Task)
            {
                return null;
            }

            return await userInputTask.Task.ConfigureAwait(false);
        }
        finally
        {
            dsc.InteractionCreated -= Interaction;
        }

        async Task Interaction(SocketInteraction arg)
        {
            if (arg is SocketMessageComponent c)
            {
                await Task.Run(async () =>
                {
                    if (c.Channel.Id != channelId || c.Message.Id != msgId || c.User.Id != userId)
                    {
                        if (!alreadyDeferred) await c.DeferAsync().ConfigureAwait(false);
                        return Task.CompletedTask;
                    }

                    if (c.Data.CustomId == "yes")
                    {
                        if (!alreadyDeferred) await c.DeferAsync().ConfigureAwait(false);
                        userInputTask.TrySetResult("Yes");
                        return Task.CompletedTask;
                    }

                    if (!alreadyDeferred) await c.DeferAsync().ConfigureAwait(false);
                    userInputTask.TrySetResult(c.Data.CustomId);
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            }
        }
    }

    public async Task<bool> GetAgreedRules(BoostContext context, Credentials creds)
    {
        if (!creds.RequireAgreement)
            return true;
        
        var agreed = await context.RulesAgreed.AnyAsync(x => x.UserId == Context.User.Id).ConfigureAwait(false);
        if (agreed)
            return true;

        var eb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Do you agree to these rules?")
            .WithDescription("1. Use common sense" +
                             "\n2. Don't ping us and ask us to respond, we have lives too." +
                             "\n3. Don't be a hero/don't try to minimod" +
                             "\n4. Read <#1133255787242860704> " +
                             "\n5. Staff don't have to be nice, don't act entitled." +
                             "\n6. Don't beg for members/restocks/anything member related" +
                             "\n7. Don't be a dick" +
                             "\n8. No alts unless your other account got termed. None. Zero. Zilch.");

        if (!await PromptUserConfirmAsync(eb, Context.User.Id).ConfigureAwait(false)) return false;
        var eb2 = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Skim Check")
            .WithDescription("Which rule said no begging?");
        
        var buttonRules = new Dictionary<string, string>
        {
            {"1", "rule1"},
            {"2", "rule2"},
            {"3", "rule3"},
            {"4", "rule4"},
            {"5", "rule5"},
            {"6", "rule6"},
            {"7", "rule7"},
            {"8", "rule8"}
        };
        
        var shuffledKeys = buttonRules.Keys.ToList();
        var rng = new Random();
        var n = shuffledKeys.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (shuffledKeys[k], shuffledKeys[n]) = (shuffledKeys[n], shuffledKeys[k]);
        }
        
        var componentBuilder = new ComponentBuilder();
        foreach (var key in shuffledKeys)
        {
            componentBuilder.WithButton(key, buttonRules[key]);
        }
        var msg = await Context.Channel.SendMessageAsync(embed: eb2.Build(), components: componentBuilder.Build());
        var button = await GetButtonInputAsync(Context.Channel.Id, msg.Id, Context.User.Id);
        if (button == "rule6")
        {
            var toAdd = new RulesAgreed {UserId = Context.User.Id};
            await context.RulesAgreed.AddAsync(toAdd);
            await context.SaveChangesAsync();
            await msg.DeleteAsync();
            return true;
        }

        await msg.DeleteAsync();
        await Context.Channel.SendErrorAsync("You failed the skim check. You have to agree to the rules to use the bot.");
        return false;
    }
}