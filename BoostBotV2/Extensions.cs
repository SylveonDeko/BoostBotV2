using System.Diagnostics;
using System.Text;
using Discord;

namespace BoostBotV2;

public static class Extensions
{
    public static async Task SendErrorAsync(this IMessageChannel channel, string message)
        => await channel.SendMessageAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithDescription(message).Build()).ConfigureAwait(false);
    
    public static async Task ReplyErrorAsync(this IUserMessage userMessage, string message)
        => await userMessage.ReplyAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithDescription(message).Build()).ConfigureAwait(false);
    
    public static string TrimTo(this string str, int maxLength, bool hideDots = false)
    {
        switch (maxLength)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(maxLength),
                    $"Argument {nameof(maxLength)} can't be negative.");
            case 0:
                return string.Empty;
            case <= 3:
                return new string('.', maxLength);
        }

        if (str.Length < maxLength)
            return str;

        return hideDots ? string.Concat(str.Take(maxLength)) : $"{string.Concat(str.Take(maxLength - 1))}…";
    }
    
    public static string GenerateSecureString(int length)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-!@#$%^&*()_+~`|}{[]:;?><,./=";

        var sb = new StringBuilder();
        var rnd = new Random();

        for (var i = 0; i < length; i++)
        {
            var index = rnd.Next(chars.Length);
            sb.Append(chars[index]);
        }

        return sb.ToString();
    }
    
    public static string ExecuteCommand(string cmd)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"-c \"{cmd}\""
            }
        };

        process.Start();
        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result.Trim();
    }

}