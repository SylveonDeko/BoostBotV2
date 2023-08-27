using BoostBotV2.Common.Yml;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace BoostBotV2.Common.Attributes.TextCommands;

public class IsOwner : PreconditionAttribute
{

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var creds = services.GetService<Credentials>();
        return creds.Owners.Contains(context.User.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be an owner to use this command.");
    }
}