using System.Diagnostics;
using System.Reflection;
using BoostBotV2.Common.PubSub;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using BoostBotV2.Services;
using BoostBotV2.Services.Impl;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Mewdeko.Common.PubSub;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using IResult = Discord.Interactions.IResult;

namespace BoostBotV2
{
    public class Bot
    {
        public Bot()
        {
            _db = new DbService();
            _db.Setup();
            Credentials = DeserializeYaml.CredsDeserialize();
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 15,
                LogLevel = LogSeverity.Info,
                ConnectionTimeout = int.MaxValue,
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All ^ GatewayIntents.GuildPresences,
                FormatUsersInBidirectionalUnicode = false,
                UseInteractionSnowflakeDate = true,
                LogGatewayIntentWarnings = false
            });

            _commands = new CommandService();
        }

        public Credentials Credentials { get; }

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private const float OneThousandth = 1.0f / 1000;
        private IServiceProvider? Services { get; set; }
        private readonly DbService _db;
        public static int ReadyCount { get; set; }
        public HashSet<string> Tokens { get; set; } = new();
        public HashSet<string> OnlineTokens { get; set; } = new();

        private async Task AddServices()
        {
            var cache = new BotDataCache();
            var sw = Stopwatch.StartNew();
            var s = new ServiceCollection()
                .AddSingleton(_db)
                .AddSingleton(_client)
                .AddSingleton(this)
                .AddSingleton(Credentials)
                .AddSingleton(_commands)
                .AddSingleton(cache)
                .AddSingleton(cache.Redis)
                .AddTransient<ISeria, JsonSeria>()
                .AddSingleton<DiscordAuthService>()
                .AddTransient<IPubSub, RedisPubSub>()
                .AddSingleton<InteractionService>()
                .AddSingleton<InteractiveService>();
            s.AddHttpClient<DiscordAuthService>();
            Services = s.BuildServiceProvider();


            sw.Stop();
            Log.Information("All services loaded in {ElapsedTotalSeconds}s", sw.Elapsed.TotalSeconds);
        }

        private async Task LoginAsync(string token)
        {
            _client.Log += Client_Log;
            var clientReady = new TaskCompletionSource<bool>();

            //connect
            Log.Information("Bot logging in ...");
            try
            {
                await _client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
                await _client.StartAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while logging in");
                throw;
            }

            Log.Information("Loading services...");
            try
            {
                await AddServices();
            }
            catch (Exception e)
            {
                Log.Error("Error while loading services\n{0}", e);
                throw;
            }

            _client.Ready += ShardReady;
            await clientReady.Task.ConfigureAwait(false);
            Log.Information("Ready!");
        }

        private async Task ShardReady()
        {
            try
            {
                try
                {
                    await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), Services);
                }
                catch (Exception e)
                {
                    Log.Error("Error adding Modules\n{0}", e);
                }

                _client.MessageReceived += HandleCommandAsync;
                var service = Services.GetRequiredService<InteractionService>();
                await service.AddModulesAsync(Assembly.GetEntryAssembly(), Services);
#if DEBUG
                await service.RegisterCommandsToGuildAsync(Credentials.CommandGuild);
#else
                await service.RegisterCommandsGloballyAsync();
#endif
                
                _client.InteractionCreated += HandleInteractionAsync;
                Tokens = (await File.ReadAllLinesAsync("tokens.txt")).ToHashSet();
                OnlineTokens = (await File.ReadAllLinesAsync("onlinetokens.txt")).ToHashSet();
                service.SlashCommandExecuted += HandleCommands;
            }
            catch (Exception e)
            {
                Log.Error("Error while starting up\n{0}", e);
                throw;
            }
        }

        private Task HandleInteractionAsync(SocketInteraction arg)
        {
            _ = Task.Run(async () =>
            {
                await using var uow = _db.GetDbContext();
                var blacklists = uow.Blacklists.ToList();
                if (blacklists.Any(x => x.BlacklistId == arg.User.Id && x.BlacklistType == BlacklistType.User))
                {
                    await arg.RespondAsync("You are blacklisted from using this bot.", ephemeral: true);
                    return;
                }

                if (blacklists.Any(x => x.BlacklistId == arg.GuildId && x.BlacklistType == BlacklistType.Guild))
                {
                    await arg.RespondAsync("This guild is blacklisted from using this bot.", ephemeral: true);
                    return;
                }

                var context = new InteractionContext(_client, arg);
                var service = Services.GetRequiredService<InteractionService>();
                await service.ExecuteCommandAsync(context, Services);
            });
            return Task.CompletedTask;
        }


        private async Task RunAsync()
        {
            var sw = Stopwatch.StartNew();
            if (string.IsNullOrEmpty(Credentials.BotToken))
            {
                Log.Error("Bot Token not set. Cannot start");
                Environment.Exit(1);
            }

            if (Credentials.CommandGuild is 0)
            {
                Log.Error("Command Guild not specified, cannot start");
                Environment.Exit(1);
            }

            if (File.Exists("tokens.txt"))
            {
                Tokens = (await File.ReadAllLinesAsync("tokens.txt")).ToHashSet();
            }
            else
            {
                File.Create("tokens.txt");
            }

            if (File.Exists("onlinetokens.txt"))
            {
                OnlineTokens = (await File.ReadAllLinesAsync("onlinetokens.txt")).ToHashSet();
            }
            else
            {
                File.Create("onlinetokens.txt");
            }

            await LoginAsync(Credentials.BotToken).ConfigureAwait(false);

            sw.Stop();
            Log.Information("Client connected in {Elapsed:F2}s", sw.Elapsed.TotalSeconds);
        }

        private static Task Client_Log(LogMessage arg)
        {
            if (arg.Exception != null)
                Log.Warning(arg.Exception, "{ArgSource} | {ArgMessage}", arg.Source, arg.Message);
            else
                Log.Warning("{ArgSource} | {ArgMessage}", arg.Source, arg.Message);

            return Task.CompletedTask;
        }

        public async Task RunAndBlockAsync()
        {
            await RunAsync().ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private Task HandleCommands(SlashCommandInfo slashInfo, IInteractionContext ctx, IResult result)
        {
            _ = Task.Run(async () =>
            {
                var toFetch = await _client.Rest.GetChannelAsync(Credentials.CommandLogChannel).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    await ctx.Interaction.SendErrorAsync($"Command failed for the following reason:\n{result.ErrorReason}").ConfigureAwait(false);
                    Log.Warning("Slash Command Errored\n\t" + "User: {0}\n\t" + "Server: {1}\n\t" + "Channel: {2}\n\t" + "Message: {3}\n\t" + "Error: {4}",
                        $"{ctx.User} [{ctx.User.Id}]", // {0}
                        ctx.Guild == null ? "PRIVATE" : $"{ctx.Guild.Name} [{ctx.Guild.Id}]", // {1}
                        ctx.Channel == null ? "PRIVATE" : $"{ctx.Channel.Name} [{ctx.Channel.Id}]", // {2}
                        slashInfo.MethodName, result.ErrorReason);

                    if (toFetch is not RestTextChannel restChannel) return;
                    var eb = new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithTitle("Slash Command Errored.")
                        .AddField("Reason", result.ErrorReason)
                        .AddField("Module", slashInfo.Module.Name ?? "None")
                        .AddField("Command", slashInfo.Name)
                        .AddField("User", $"{ctx.User.Mention} `{ctx.User.Id}`")
                        .AddField("Guild", ctx.Guild == null ? "PRIVATE" : $"{ctx.Guild.Name} `{ctx.Guild.Id}`");

                    await restChannel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
                    return;
                }

                var chan = ctx.Channel as ITextChannel;
                Log.Information("Slash Command Executed" + "\n\t" + "User: {0}\n\t" + "Server: {1}\n\t" + "Channel: {2}\n\t" + "Module: {3}\n\t" + "Command: {4}",
                    $"{ctx.User} [{ctx.User.Id}]", // {0}
                    ctx.Guild == null ? "PRIVATE" : $"{ctx.Guild.Name} [{ctx.Guild.Id}]", // {1}
                    chan == null ? "PRIVATE" : $"{chan.Name} [{chan.Id}]", // {2}
                    slashInfo.Module.SlashGroupName, slashInfo.MethodName); // {3}

                if (toFetch is RestTextChannel restChannel1)
                {
                    var eb = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle("Slash Command Executed.")
                        .AddField("Module", slashInfo.Module.Name ?? "None")
                        .AddField("Command", slashInfo.Name)
                        .AddField("User", $"{ctx.User.Mention} `{ctx.User.Id}`")
                        .AddField("Guild", ctx.Guild == null ? "PRIVATE" : $"{ctx.Guild.Name} `{ctx.Guild.Id}`");

                    await restChannel1.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
                }
            });
            return Task.CompletedTask;
        }

        private Task HandleCommandAsync(SocketMessage messageParam)
        {
            _ = Task.Run(async () =>
            {
                var execTime = Environment.TickCount;

                if (messageParam is not SocketUserMessage message) return;
                if (messageParam.Channel is not SocketGuildChannel channel) return;
                if (message.Source != MessageSource.User) return;

                var argPos = 0;
                if (!(message.HasStringPrefix(Credentials.Prefix, ref argPos) ||
                      message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
                var exec2 = Environment.TickCount - execTime;

                await using var uow = _db.GetDbContext();
                var blacklists = uow.Blacklists;
                if (blacklists.Any(x => x.BlacklistId == messageParam.Author.Id && x.BlacklistType == BlacklistType.User) && !Credentials.Owners.Contains(message.Author.Id))
                {
                    Log.Error("User {User} is blacklisted.", messageParam.Author);
                    return;
                }

                if (blacklists.Any(x => x.BlacklistId == channel.Guild.Id && x.BlacklistType == BlacklistType.Guild) && channel.Guild.Id != Credentials.CommandGuild)
                {
                    Log.Error("Guild {Guild} is blacklisted.", channel.Guild);
                    return;
                }

                var context = new CommandContext(_client, message);
                execTime = Environment.TickCount - execTime;
                var executed = await _commands.ExecuteAsync(context, argPos, Services);
                if (executed.IsSuccess)
                {
                    await LogSuccessfulExecution(message, channel as ITextChannel, exec2, execTime).ConfigureAwait(false);
                }
                else
                {
                    if (executed.Error == CommandError.UnknownCommand)
                        return;
                    await LogErroredExecution(executed.ErrorReason, message, channel as ITextChannel, exec2, execTime).ConfigureAwait(false);
                    var eb = new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription(executed.ErrorReason)
                        .WithAuthor("Error", _client.CurrentUser.GetAvatarUrl())
                        .WithCurrentTimestamp();

                    await message.Channel.SendMessageAsync(embed: eb.Build());
                }
            });
            return Task.CompletedTask;
        }

        private Task LogSuccessfulExecution(IMessage usrMsg, IGuildChannel? channel, params int[] execPoints)
        {
            _ = Task.Run(async () =>
            {
                ulong guildId = 0;
                guildId = usrMsg.Content.Contains("djoin") ? ulong.Parse(usrMsg.Content.Split(" ")[1]) : channel.GuildId;
                var buttons = new ComponentBuilder()
                    .WithButton("Blacklist User", $"bluser:{usrMsg.Author.Id}", ButtonStyle.Danger)
                    .WithButton("Blacklist Guild", $"blguild:{guildId}", ButtonStyle.Danger)
                    .Build();

                Log.Information(
                    "Command Executed after "
                    + string.Join("/", execPoints.Select(x => (x * OneThousandth).ToString("F3")))
                    + "s\n\t"
                    + "User: {0}\n\t"
                    + "Server: {1}\n\t"
                    + "Channel: {2}\n\t"
                    + "Message: {3}", $"{usrMsg.Author} [{usrMsg.Author.Id}]", // {0}
                    channel == null ? "PRIVATE" : $"{channel.Guild.Name} [{channel.Guild.Id}]", // {1}
                    channel == null ? "PRIVATE" : $"{channel.Name} [{channel.Id}]", // {2}
                    usrMsg.Content); // {3}
                var toFetch = await _client.Rest.GetChannelAsync(Credentials.CommandLogChannel).ConfigureAwait(false);
                if (toFetch is RestTextChannel restChannel)
                {
                    var eb = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle("Text Command Executed")
                        .AddField("Executed Time", string.Join("/", execPoints.Select(x => (x * OneThousandth).ToString("F3"))))
                        .AddField("User", $"{usrMsg.Author.Mention} {usrMsg.Author} {usrMsg.Author.Id}")
                        .AddField("Guild", channel == null ? "PRIVATE" : $"{channel.Guild.Name} `{channel.Guild.Id}`")
                        .AddField("Channel", channel == null ? "PRIVATE" : $"{channel.Name} `{channel.Id}`")
                        .AddField("Message", usrMsg.Content.TrimTo(1000));

                    await restChannel.SendMessageAsync(embed: eb.Build(), components: buttons).ConfigureAwait(false);
                }
            });
            return Task.CompletedTask;
        }

        private Task LogErroredExecution(string errorMessage, IMessage usrMsg, IGuildChannel? channel, params int[] execPoints)
        {
            _ = Task.Run(async () =>
            {
                var buttons = new ComponentBuilder()
                    .WithButton("Blacklist User", $"bluser:{usrMsg.Author.Id}", ButtonStyle.Danger)
                    .WithButton("Blacklist Guild", $"blguild:{channel?.GuildId}", ButtonStyle.Danger)
                    .Build();

                var errorafter = string.Join("/", execPoints.Select(x => (x * OneThousandth).ToString("F3")));
                Log.Warning($"Command Errored after {errorafter}\n\t" + "User: {0}\n\t" + "Server: {1}\n\t" + "Channel: {2}\n\t" + "Message: {3}\n\t" + "Error: {4}",
                    $"{usrMsg.Author} [{usrMsg.Author.Id}]", // {0}
                    channel == null ? "PRIVATE" : $"{channel.Guild.Name} [{channel.Guild.Id}]", // {1}
                    channel == null ? "PRIVATE" : $"{channel.Name} [{channel.Id}]", // {2}
                    usrMsg.Content, errorMessage);

                var toFetch = await _client.Rest.GetChannelAsync(Credentials.CommandLogChannel).ConfigureAwait(false);
                if (toFetch is RestTextChannel restChannel)
                {
                    var eb = new EmbedBuilder().WithColor(Color.Red).WithTitle("Text Command Errored").AddField("Error Reason", errorMessage)
                        .AddField("Errored Time", errorafter)
                        .AddField("User", $"{usrMsg.Author} {usrMsg.Author.Id}")
                        .AddField("Guild", channel == null ? "PRIVATE" : $"{channel.Guild.Name} `{channel.Guild.Id}`")
                        .AddField("Channel", channel == null ? "PRIVATE" : $"{channel.Name} `{channel.Id}`").AddField("Message", usrMsg.Content.TrimTo(1000));

                    await restChannel.SendMessageAsync(embed: eb.Build(), components: buttons).ConfigureAwait(false);
                }
            });
            return Task.CompletedTask;
        }
    }
}