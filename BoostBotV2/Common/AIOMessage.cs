using Discord;

namespace BoostBotV2.Common;

public class AIOMessage: IMessage
{
    public ulong Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public async Task DeleteAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task AddReactionAsync(IEmote emote, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveReactionAsync(IEmote emote, ulong userId, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveAllReactionsAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveAllReactionsForEmoteAsync(IEmote emote, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(IEmote emoji, int limit, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public MessageType Type { get; set; }
    public MessageSource Source { get; set; }
    public bool IsTTS { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSuppressed { get; set; }
    public bool MentionedEveryone { get; set; }
    public string Content { get; set; }
    public string CleanContent { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? EditedTimestamp { get; set; }
    public IMessageChannel Channel { get; set; }
    public IUser Author { get; set; }
    public IThreadChannel Thread { get; set; }
    public IReadOnlyCollection<IAttachment> Attachments { get; set; }
    public IReadOnlyCollection<IEmbed> Embeds { get; set; }
    public IReadOnlyCollection<ITag> Tags { get; set; }
    public IReadOnlyCollection<ulong> MentionedChannelIds { get; set; }
    public IReadOnlyCollection<ulong> MentionedRoleIds { get; set; }
    public IReadOnlyCollection<ulong> MentionedUserIds { get; set; }
    public MessageActivity Activity { get; set; }
    public MessageApplication Application { get; set; }
    public MessageReference Reference { get; set; }
    public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions { get; set; }
    public IReadOnlyCollection<IMessageComponent> Components { get; set; }
    public IReadOnlyCollection<IStickerItem> Stickers { get; set; }
    public MessageFlags? Flags { get; set; }
    public IMessageInteraction Interaction { get; set; }
    public MessageRoleSubscriptionData RoleSubscriptionData { get; set; }
    public async Task ModifyAsync(Action<MessageProperties> func, RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task PinAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task UnpinAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public async Task CrosspostAsync(RequestOptions options = null)
    {
        throw new NotImplementedException();
    }

    public string Resolve(TagHandling userHandling = TagHandling.Name, TagHandling channelHandling = TagHandling.Name, TagHandling roleHandling = TagHandling.Name, TagHandling everyoneHandling = TagHandling.Ignore,
        TagHandling emojiHandling = TagHandling.Name)
    {
        throw new NotImplementedException();
    }

    public IUserMessage ReferencedMessage { get; set; }
}