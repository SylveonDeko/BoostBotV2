using BoostBotV2.Common.Yml;
using Mewdeko.Common.PubSub;
using Serilog;
using StackExchange.Redis;

namespace BoostBotV2.Common.PubSub;

public sealed class RedisPubSub : IPubSub
{
    private readonly Credentials _creds;
    private readonly ConnectionMultiplexer _multi;
    private readonly ISeria _serializer;

    public RedisPubSub(ConnectionMultiplexer multi, ISeria serializer, Credentials creds)
    {
        this._multi = multi;
        this._serializer = serializer;
        this._creds = creds;
    }

    public Task Pub<TData>(in TypedKey<TData> key, TData data)
        where TData : notnull
    {
        var serialized = _serializer.Serialize(data);
        return _multi.GetSubscriber()
            .PublishAsync($"{_creds.BotToken[..10]}:{key.Key}", serialized, CommandFlags.FireAndForget);
    }

    public Task Sub<TData>(in TypedKey<TData> key, Func<TData, ValueTask> action)
        where TData : notnull
    {
        var eventName = key.Key;

        async void OnSubscribeHandler(RedisChannel _, RedisValue data)
        {
            try
            {
                var dataObj = _serializer.Deserialize<TData>(data);
                if (dataObj is not null)
                {
                    await action(dataObj).ConfigureAwait(false);
                }
                else
                {
                    Log.Warning("Publishing event {EventName} with a null value. This is not allowed",
                        eventName);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error handling the event {EventName}: {ErrorMessage}", eventName, ex.Message);
            }
        }

        return _multi.GetSubscriber().SubscribeAsync($"{_creds.BotToken[..10]}:{eventName}", OnSubscribeHandler);
    }
}