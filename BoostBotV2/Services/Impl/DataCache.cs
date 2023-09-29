using System.Runtime.CompilerServices;
using BoostBotV2.Common;
using BoostBotV2.Common.Yml;
using BoostBotV2.Services.Impl.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BoostBotV2.Services.Impl;

public class BotDataCache
{
    private Lazy<Task<ConnectionMultiplexer>> _lazyRedis;
    private readonly Credentials _credentials;

    public BotDataCache()
    {
        _credentials = DeserializeYaml.CredsDeserialize();
        var conf = ConfigurationOptions.Parse("127.0.0.1,syncTimeout=3000");
        conf.SocketManager = new SocketManager("Main", true);

        _lazyRedis = new Lazy<Task<ConnectionMultiplexer>>(() => LoadRedis(conf));
    }

    private static async Task<ConnectionMultiplexer> LoadRedis(ConfigurationOptions options)
    {
        options.AsyncTimeout = 20000;
        options.SyncTimeout = 20000;
        var redis = await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
        return redis;
    }
    
    // public async Task GetOnlineStock()
    // {
    //     var db = Redis.GetDatabase();
    //     await db.StringSetAsync($"onlineStock", JsonConvert.SerializeObject(new OnlineStock()));
    // }

    public ConnectionMultiplexer Redis => _lazyRedis.Value.Result;
}