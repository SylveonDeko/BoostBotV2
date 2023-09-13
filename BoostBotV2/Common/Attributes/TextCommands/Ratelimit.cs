using BoostBotV2.Common.Yml;
using Discord;
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
        var user = context.User as IGuildUser;
        var creds = services.GetRequiredService<Credentials>();

        // Use a local variable for timeout
        var currentTimeout = _timeout;

        if (user.RoleIds.Contains<ulong>(creds.PremiumRoleId))
        {
            currentTimeout = TimeSpan.FromSeconds(_timeout.Seconds / 2);
        }
        
        if (creds.Owners.Contains(context.User.Id))
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        if (UserTimeouts.TryGetValue(context.User.Id, out var timeout))
        {
            var timeSinceLastMessage = DateTime.UtcNow - timeout;
            if (timeSinceLastMessage < currentTimeout)
            {
                return Task.FromResult(PreconditionResult.FromError($"Please wait {currentTimeout.TotalSeconds} seconds between commands."));
            }
        }

        UserTimeouts[context.User.Id] = DateTime.UtcNow;

        return Task.FromResult(PreconditionResult.FromSuccess());
    }

}