using Discord.Commands;

namespace BoostBotV2.Common.Attributes.TextCommands;

public class Usage : Attribute
{
    public Usage(string usage)
    {
        if (string.IsNullOrWhiteSpace(usage))
            throw new ArgumentException("Usage cannot be null or whitespace.", nameof(usage));

        UsageString = usage.Split("\n").ToList();
    }
    
    public List<string> UsageString { get; set; }
}