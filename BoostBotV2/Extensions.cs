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
    
    public static async Task SendConfirmAsync(this IMessageChannel channel, string message)
        => await channel.SendMessageAsync(embed: new EmbedBuilder().WithColor(Color.Green).WithDescription(message).Build()).ConfigureAwait(false);
    
    public static async Task ReplyConfirmAsync(this IUserMessage userMessage, string message)
        => await userMessage.ReplyAsync(embed: new EmbedBuilder().WithColor(Color.Green).WithDescription(message).Build()).ConfigureAwait(false);
    
    public static async Task SendErrorAsync(this IDiscordInteraction channel, string message)
        => await channel.FollowupAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithDescription(message).Build()).ConfigureAwait(false);
    
    public static async Task ReplyErrorAsync(this IDiscordInteraction userMessage, string message)
        => await userMessage.FollowupAsync(embed: new EmbedBuilder().WithColor(Color.Red).WithDescription(message).Build()).ConfigureAwait(false);
    
    public static async Task SendConfirmAsync(this IDiscordInteraction channel, string message)
        => await channel.FollowupAsync(embed: new EmbedBuilder().WithColor(Color.Green).WithDescription(message).Build()).ConfigureAwait(false);
    
    public static async Task ReplyConfirmAsync(this IDiscordInteraction userMessage, string message)
        => await userMessage.FollowupAsync(embed: new EmbedBuilder().WithColor(Color.Green).WithDescription(message).Build()).ConfigureAwait(false);
    
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
        Process process;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"/c {cmd}"
                }
            };
        }
        else
        {
            process = new Process
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
        }

        process.Start();
        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result.Trim();
    }
    
    public static IEmote? ToIEmote(this string emojiStr) =>
        Emote.TryParse(emojiStr, out var maybeEmote)
            ? maybeEmote
            : new Emoji(emojiStr);
    
    /// <summary>
    ///     Creates a task that will complete when all of the <see cref="Task{TResult}" /> objects in an enumerable
    ///     collection have completed
    /// </summary>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <typeparam name="TResult">The type of the completed task.</typeparam>
    /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
    public static Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> tasks)
        => Task.WhenAll(tasks);

    /// <summary>
    ///     Creates a task that will complete when all of the <see cref="Task" /> objects in an enumerable
    ///     collection have completed
    /// </summary>
    /// <param name="tasks">The tasks to wait on for completion.</param>
    /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
    public static Task WhenAll(this IEnumerable<Task> tasks)
        => Task.WhenAll(tasks);

}