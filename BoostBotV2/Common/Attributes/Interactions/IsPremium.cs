﻿using BoostBotV2.Common.Yml;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace BoostBotV2.Common.Attributes.Interactions;

public class IsPremium : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        await Task.CompletedTask;
        var creds = services.GetRequiredService<Credentials>();
        if (creds.Owners.Contains(context.User.Id))
            return PreconditionResult.FromSuccess();
        var user = context.User as IGuildUser;
        return user.RoleIds.Contains(creds.PremiumRoleId) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("This command can only be used by premium users.");
    }
}