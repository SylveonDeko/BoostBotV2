using BoostBotV2.Common.Yml;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace BoostBotV2.Common.Attributes.TextCommands;

public class RateLimitAttribute : PreconditionAttribute
{
    // Store the timestamp of the last message for each user
    private static readonly Dictionary<ulong, DateTime> UserTimeouts = new();

    // Set a limit on the number of seconds between messages
    private readonly TimeSpan _timeout;

    public RateLimitAttribute(int seconds)
    {
        _timeout = TimeSpan.FromSeconds(seconds);
    }

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var creds = services.GetService<Credentials>();
        if (creds.Owners.Contains(context.User.Id))
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
        if (UserTimeouts.TryGetValue(context.User.Id, out var timeout))
        {
            var timeSinceLastMessage = DateTime.UtcNow - timeout;
            if (timeSinceLastMessage < _timeout)
            {
                return Task.FromResult(PreconditionResult.FromError($"Please wait {_timeout.TotalSeconds} seconds between commands."));
            }
        }

        UserTimeouts[context.User.Id] = DateTime.UtcNow;

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}