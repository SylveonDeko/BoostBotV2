using BoostBotV2.Common.Yml;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace BoostBotV2.Common.Attributes.Interactions;

public class IsOwner : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        var creds = services.GetService<Credentials>();
        return creds.Owners.Contains(context.User.Id) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be an owner to use this command.");
    }
}