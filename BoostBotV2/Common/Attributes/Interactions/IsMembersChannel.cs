using BoostBotV2.Common.Yml;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace BoostBotV2.Common.Attributes.Interactions;

public class IsMembersChannel : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        var creds = services.GetRequiredService<Credentials>();
        if (creds.Owners.Contains(context.User.Id))
            return Task.FromResult(PreconditionResult.FromSuccess());
        var service = services.GetRequiredService<Credentials>();
        return context.Guild.Id != service.CommandGuild ? Task.FromResult(PreconditionResult.FromError("You must be in the command guild to use this command.")) : Task.FromResult(creds.FarmChannel == context.Channel.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You must be in the member-farm channel to use this command."));
    }
}