using BoostBotV2.Common.Yml;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace BoostBotV2.Common.Attributes.TextCommands;

public class IsPremium : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var creds = services.GetRequiredService<Credentials>();
        if (creds.Owners.Contains(context.User.Id))
            return PreconditionResult.FromSuccess();
        var user = context.User as IGuildUser;
        return user.RoleIds.Contains<ulong>(1136525445693706370) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("This command can only be used by premium users.");
    }
}