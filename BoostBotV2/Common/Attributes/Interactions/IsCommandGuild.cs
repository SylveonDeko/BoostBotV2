﻿using System.Diagnostics;
using BoostBotV2.Common.Yml;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace BoostBotV2.Common.Attributes.Interactions;

public class IsCommandGuild : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        var creds = services.GetRequiredService<Credentials>();
        if (creds.Owners.Contains(context.User.Id))
            return PreconditionResult.FromSuccess();
        Debug.Assert(creds != null, nameof(creds) + " != null");
        return creds.CommandGuild == context.Guild.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("This command can only be used in the command guild.");

    }
}