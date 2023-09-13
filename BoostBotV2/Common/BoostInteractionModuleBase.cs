using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace BoostBotV2.Common;

public class BoostInteractionModuleBase : InteractionModuleBase
{
    public async Task<bool> PromptUserConfirmAsync(string message, ulong userid, bool alreadyDeferred = false, bool ephemeral = false)
        => await PromptUserConfirmAsync(new EmbedBuilder().WithColor(Color.Green).WithDescription(message), userid, alreadyDeferred, ephemeral).ConfigureAwait(false);

    public async Task<bool> PromptUserConfirmAsync(EmbedBuilder embed, ulong userid, bool alreadyDeferred = false, bool ephemeral = false)
    {
        embed.WithColor(Color.Green);
        var buttons = new ComponentBuilder().WithButton("Yes", "yes", ButtonStyle.Success).WithButton("No", "no", ButtonStyle.Danger);
        IUserMessage msg;
        msg = await Context.Interaction.FollowupAsync(embed: embed.Build(), components: buttons.Build()).ConfigureAwait(false);
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
                        if (!alreadyDeferred && !arg.HasResponded) await c.DeferAsync().ConfigureAwait(false);
                        return Task.CompletedTask;
                    }

                    if (c.Data.CustomId == "yes")
                    {
                        if (!alreadyDeferred && !arg.HasResponded) await c.DeferAsync().ConfigureAwait(false);
                        userInputTask.TrySetResult("Yes");
                        return Task.CompletedTask;
                    }

                    if (!alreadyDeferred && !arg.HasResponded) await c.DeferAsync().ConfigureAwait(false);
                    userInputTask.TrySetResult(c.Data.CustomId);
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            }
        }
    }

    public async Task<bool> GetAgreedRules(BoostContext context, Credentials creds)
    {
        await DeferAsync();
        if (!creds.RequireAgreement)
            return true;

        var attempts = context.RuleAttempts.Count(x => x.UserId == Context.User.Id && x.DateAdded > DateTime.UtcNow.AddMinutes(-5));
        if (attempts >= 3)
        {
            await Context.Interaction.SendErrorAsync("Too many attempts. Try again later.").ConfigureAwait(false);
            return false;
        }

        var agreed = await context.RulesAgreed.AnyAsync(x => x.UserId == Context.User.Id).ConfigureAwait(false);
        if (agreed)
            return true;
        
        var correctRule = creds.CorrectRule ?? 6;

        var rulesList = creds.Rules ?? new List<string>
        {
            "Use common sense",
            "Don't ping us and ask us to respond, we have lives too.",
            "Don't be a hero/don't try to minimod",
            "Read <#1133255787242860704>",
            "Staff don't have to be nice, don't act entitled.",
            "Don't beg for members/restocks/anything member related",
            "Don't be a dick",
            "No alts unless your other account got termed. None. Zero. Zilch."
        };

        var rulesDescription = string.Join("\n", rulesList.Select((rule, index) => $"{index + 1}. {rule}"));

        var eb = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Do you agree to these rules?")
            .WithDescription(rulesDescription)
            .WithFooter("If you are caught picking for someone else you will be timed out and so will they.");

        if (!await PromptUserConfirmAsync(eb, Context.User.Id, true).ConfigureAwait(false)) return false;

        var eb2 = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Skim Check")
            .WithDescription($"Which rule said: \n\n{rulesList[correctRule - 1]}?")
            .WithFooter("If you are caught picking for someone else you will be timed out and so will they.");

        var buttonRules = rulesList.Select((rule, index) => ($"{index + 1}", $"rule{index + 1}")).ToDictionary(pair => pair.Item1, pair => pair.Item2);

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

        var msg = await Context.Interaction.FollowupAsync(embed: eb2.Build(), components: componentBuilder.Build());
        var button = await GetButtonInputAsync(Context.Channel.Id, msg.Id, Context.User.Id, true);

        if (button == $"rule{correctRule}")
        {
            var toAdd = new RulesAgreed { UserId = Context.User.Id };
            await context.RulesAgreed.AddAsync(toAdd);
            await context.SaveChangesAsync();
            await msg.DeleteAsync();
            return true;
        }

        await msg.DeleteAsync();
        await context.RuleAttempts.AddAsync(new RuleAttempts { UserId = Context.User.Id });
        await context.SaveChangesAsync();
        await Context.Interaction.SendErrorAsync("You failed the skim check. You have to agree to the rules to use the bot.");
        return false;
    }
}