using BoostBotV2.Common.Yml;
using StackExchange.Redis;

// ReSharper disable CollectionNeverQueried.Local

namespace BoostBotV2.Api.Services.Impl;

public class ApiDataCache : IDataCache
{
    private string redisKey;

    public ApiDataCache(Credentials credentials)
    {
        var conf = ConfigurationOptions.Parse("127.0.0.1,syncTimeout=3000");
        conf.SocketManager = new SocketManager("Main", true);
        LoadRedis(conf, credentials).ConfigureAwait(false);
    }


    private async Task LoadRedis(ConfigurationOptions options, Credentials creds)
    {
        options.AsyncTimeout = 20000;
        options.SyncTimeout = 20000;
        Redis = await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
        redisKey = creds.BotToken[..10];
    }

    public ConnectionMultiplexer Redis { get; set; }
    
    
    
}