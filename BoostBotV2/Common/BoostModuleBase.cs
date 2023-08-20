using Discord;
using Discord.Commands;
using Discord.WebSocket;

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
}