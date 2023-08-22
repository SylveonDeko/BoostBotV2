using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using BoostBotV2.Common.Yml;
using BoostBotV2.Db;
using BoostBotV2.Db.Models;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

#pragma warning disable CS8618
#nullable restore

namespace BoostBotV2.Services;

public class DiscordAuthService
{
    private readonly string _secret;
    private readonly string _clientId;
    private readonly string _redirect;
    private readonly string _apiEndpoint;
    private readonly string _auth;
    private readonly HttpClient _client;
    private readonly DiscordSocketClient _discord;
    private readonly DbService _db;
    private readonly Bot _bot;
    private readonly ConcurrentQueue<string> _tokensToRemove = new();
    private readonly ConcurrentDictionary<ulong, HashSet<string>> _boostTokens = new();
    private readonly ConcurrentDictionary<ulong, HashSet<string>> _privateofflinestock = new();
    private readonly ConcurrentDictionary<ulong, HashSet<string>> _privateonlinestock = new();
    private readonly Credentials _creds;

    private const string Useragent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
    private const int BuildNumber = 165486;
    private const string Cv = "108.0.0.0";
    private readonly string _properties;
    private static readonly Dictionary<string, int> MemberAddAllowences = new()
    {
        { "free", 48 },
        { "bronze", 70 },
        { "silver", 90 },
        { "gold", 200 },
        { "platinum", 500 }
    };

    public DiscordAuthService(Credentials creds, HttpClient client, DiscordSocketClient discord, Bot bot, DbService db)
    {
        _creds = creds;
        _client = client;
        _discord = discord;
        _bot = bot;
        _db = db;
        _secret = creds.ClientSecret;
        _clientId = creds.ClientId.ToString();
        _redirect = "http://localhost:8080";
        _apiEndpoint = "https://canary.discord.com/api/v9";
        _auth = $"https://canary.discord.com/api/oauth2/authorize?client_id={_clientId}&redirect_uri={_redirect}&response_type=code&scope=identify%20guilds.join";

        var properties = new
        {
            os = "Windows",
            browser = "Chrome",
            device = "PC",
            system_locale = "en-GB",
            browser_user_agent = Useragent,
            browser_version = Cv,
            os_version = "10",
            referrer = "https://discord.com/channels/@me",
            referring_domain = "discord.com",
            referrer_current = "",
            referring_domain_current = "",
            release_channel = "stable",
            client_build_number = BuildNumber,
            client_event_source = ""
        };
        var propertiesJson = JsonConvert.SerializeObject(properties);
        _properties = Convert.ToBase64String(Encoding.UTF8.GetBytes(propertiesJson));
        var uow = db.GetDbContext();
        _boostTokens = new ConcurrentDictionary<ulong, HashSet<string>>(uow.NitroStock
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Token).ToHashSet()));
        _ = ProcessRemovals();
        _privateofflinestock = new ConcurrentDictionary<ulong, HashSet<string>>(uow.PrivateStock
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.Where(z => !z.IsOnline)
                .Select(y => y.Token).ToHashSet()));
        _privateonlinestock = new ConcurrentDictionary<ulong, HashSet<string>>(uow.PrivateStock
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.Where(z => z.IsOnline)
                .Select(y => y.Token).ToHashSet()));
    }

    public async Task ProcessRemovals()
    {
        while (true)
        {
            if (_tokensToRemove.IsEmpty)
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for 5 seconds before checking the queue again
                continue;
            }

            foreach (var token in _tokensToRemove)
            {
                var lines = await File.ReadAllLinesAsync("tokens.txt");
                await File.WriteAllLinesAsync("tokens.txt", lines.Where(l => l != token));
                var tokens = await File.ReadAllLinesAsync("tokens.txt");
                _bot.Tokens = tokens.ToHashSet();
                _tokensToRemove.TryDequeue(out _); // Remove the processed token from the queue
            }
        }
    }


    private HttpRequestMessage CreateRequest(HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        var headers = GetHeaders(token);
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        return request;
    }

    private Dictionary<string, string> GetHeaders(string token)
    {
        return new Dictionary<string, string>
        {
            { "Authorization", token },
            { "Origin", "https://canary.discord.com" },
            { "Accept", "*/*" },
            { "X-Discord-Locale", "en-GB" },
            { "X-Super-Properties", _properties },
            { "User-Agent", Useragent },
            { "Referer", "https://canary.discord.com/channels/@me" },
            { "X-Debug-Options", "bugReporterEnabled" }
        };
    }

    public async Task<UserResponse?> GetUser(string access)
    {
        try
        {
            const string endpoint = "https://canary.discord.com/api/v9/users/@me";
            var request = CreateRequest(HttpMethod.Get, endpoint, $"Bearer {access}");
            var response = await _client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return null;
            var responseJson = JsonConvert.DeserializeObject<UserResponse>(responseContent);
            return responseJson;
        }
        catch (Exception e)
        {
            Log.Error("Failed to get user: {Message}", e.Message);
            return null;
        }
    }

    public async Task<TokenResponse?> ExchangeCode(string code)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _secret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _redirect }
            };

            var content = new FormUrlEncodedContent(data);
            var request = CreateRequest(HttpMethod.Post, $"{_apiEndpoint}/oauth2/token", _clientId, content);
            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode) return null;
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TokenResponse>(responseContent);
        }
        catch (Exception e)
        {
            Log.Error("Failed to exchange code: {Message}", e.Message);
            return null;
        }
    }

    public async Task<bool> AddToGuild(string accessToken, string userId, string guild)
    {
        try
        {
            var a = await _discord.GetGuild(Convert.ToUInt64(guild)).AddGuildUserAsync(Convert.ToUInt64(userId), accessToken, options: new RequestOptions()
            {
                RetryMode = RetryMode.RetryRatelimit
            });
        }
        catch (Exception e)
        {
            if (e.Message.Contains("You are at the 100 server limit."))
            {
                _tokensToRemove.Enqueue(accessToken);
                return false;
            }
            Log.Error("Failed to add user to guild: {Message}", e.Message);
            return false;
        }

        return true;
    }

    public async Task<bool> Authorizer(string token, string guild)
    {
        try
        {
            var json = JsonConvert.SerializeObject(new { authorize = "true" });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpRequestMessage request;
            try
            {
                request = CreateRequest(HttpMethod.Post, _auth, token, content);
            }
            catch (Exception e)
            {
                Log.Error("Failed to create request: {Message}", e.Message);
                _tokensToRemove.Enqueue(token);
                return false;
            }
            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)) return false;
                _tokensToRemove.Enqueue(token);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var code = JsonConvert.DeserializeObject<LocationModel>(responseContent).Location;

            var exchange = await ExchangeCode(code.Split("code=")[1].Split("&")[0]);
            if (exchange == null)
            {
                _tokensToRemove.Enqueue(token);
                return false;
            }

            Log.Information("$ Authorized Token");

            var accessToken = exchange?.AccessToken;
            var user = await GetUser(accessToken);

            var toreturn = await AddToGuild(accessToken, user.Id, guild);
            
            if (toreturn is false)
                return false;
            Log.Information($"$ Added to Guild {guild}");

            return true;
        }
        catch (Exception e)
        {
            Log.Error("Failed to authorize token: {e}", e);
            return false;
        }
    }

    public async Task<bool> BoostGuildWithTokens(string token, string guild)
    {
        var request = CreateRequest(HttpMethod.Get, "https://canary.discord.com/api/v9/users/@me/guilds/premium/subscription-slots", token);
        var response = await _client.SendAsync(request);
        Log.Information("Subscription Slots Status Code: " + response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var idk = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
            Log.Information("Subscription Slots JSON: " + idk);

            foreach (var x in idk)
            {
                var id = x.id;
                var payload = new { user_premium_guild_subscription_slot_ids = new[] { id } };
                var json = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                request = CreateRequest(HttpMethod.Put, $"https://canary.discord.com/api/v9/guilds/{guild}/premium/subscriptions", token, json);
                response = await _client.SendAsync(request);
                // Console.WriteLine("Boost Status Code: " + response.StatusCode);

                if (!response.IsSuccessStatusCode) continue;
                Log.Information("[+] Boosted " + guild);
                return true; // Return true to indicate successful boost to the guild
            }
        }

        // If there was an issue with subscription slots or boosting, still return false
        Log.Error("Failed to boost.");
        return false; // Return false to indicate failure in boost to the guild
    }

    public async Task<bool> BoostAuthorizer(string token, string guild)
    {
        try
        {
            var json = JsonConvert.SerializeObject(new { authorize = "true" });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = CreateRequest(HttpMethod.Post, _auth, token, content);
            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode) return false;
            var responseContent = await response.Content.ReadAsStringAsync();
            var code = JsonConvert.DeserializeObject<LocationModel>(responseContent).Location;

            var exchange = await ExchangeCode(code.Split("code=")[1].Split("&")[0]);
            if (exchange == null)
            {
                _tokensToRemove.Enqueue(token);
                return false;
            }

            Log.Information("$ Authorized Token");

            var accessToken = exchange?.AccessToken;
            var user = await GetUser(accessToken);

            await AddToGuild(accessToken, user.Id, guild);
            var boosted = await BoostGuildWithTokens(accessToken, guild);

            Log.Information($"$ Added to Guild {guild}");

            return boosted;
        }
        catch (Exception e)
        {
            Log.Error("Failed to authorize token: {e}", e);
            return false;
        }
    }

    public HashSet<string> GetBoostTokens(ulong userId)
    {
        return !_boostTokens.ContainsKey(userId) ? new HashSet<string>() : _boostTokens[userId];
    }

    public async Task AddBoostToken(ulong userId, string token)
    {
        if (!_boostTokens.ContainsKey(userId))
            _boostTokens[userId] = new HashSet<string>();
        _boostTokens[userId].Add(token);
        await using var uow = _db.GetDbContext();
        await uow.NitroStock.AddAsync(new NitroStock
        {
            UserId = userId,
            Token = token
        });
        await uow.SaveChangesAsync();
    }

    public async Task AddMultiplePrivateStock(ulong userId, IEnumerable<string> tokens, bool isOnline = false)
    {
        if (isOnline)
        {
            if (!_privateonlinestock.ContainsKey(userId))
                _privateonlinestock[userId] = new HashSet<string>();
            var toAdd = tokens.Select(x => new PrivateStock
            {
                UserId = userId,
                Token = x,
                IsOnline = true
            });
            foreach (var i in toAdd)
            {
                var stock = _privateonlinestock[userId];
                if (stock.Contains(i.Token))
                    continue;
                stock.Add(i.Token);
            }
            await using var uow = _db.GetDbContext();
            await uow.PrivateStock.AddRangeAsync(toAdd);
            await uow.SaveChangesAsync();
        }
        else
        {
            if (!_privateofflinestock.ContainsKey(userId))
                _privateofflinestock[userId] = new HashSet<string>();
            var toAdd = tokens.Select(x => new PrivateStock
            {
                UserId = userId,
                Token = x
            });
            foreach (var i in toAdd)
            {
                var stock = _privateofflinestock[userId];
                if (stock.Contains(i.Token))
                    continue;
                stock.Add(i.Token);
            }
            await using var uow = _db.GetDbContext();
            await uow.PrivateStock.AddRangeAsync(toAdd);
            await uow.SaveChangesAsync();
        }
    }
    
    public async Task<HashSet<string>?> GetPrivateStock(ulong userId, bool onlineStock = false)
    {
        if (onlineStock)
        {
            if (!_privateonlinestock.ContainsKey(userId))
                _privateonlinestock[userId] = new HashSet<string>();
            return _privateonlinestock[userId];
        }
        else
        {
            if (!_privateofflinestock.ContainsKey(userId))
                _privateofflinestock[userId] = new HashSet<string>();
            return _privateofflinestock[userId];
        }
    }

    public async Task<int?> GetAllowedAddCount(ulong userId, string rolename, ulong guildId)
    {
        if (_creds.Owners.Contains(userId))
            return null;
        MemberAddAllowences.TryGetValue(rolename, out var allowed);
        await using var uow = _db.GetDbContext();
        var registry = await uow.MemberFarmRegistry.FirstOrDefaultAsync(x => x.UserId == userId);
        if (registry == null)
        {
            await uow.MemberFarmRegistry.AddAsync(new MemberFarmRegistry
            {
                UserId = userId,
                GuildId = guildId
            });
            await uow.SaveChangesAsync();
        }

        if (registry is null)
        {
            return allowed;
        }
        
        var added = uow.GuildsAdded.Count(x => x.GuildId == guildId && x.DateAdded > DateTime.UtcNow.AddDays(-1));
        
        return allowed - added;
    }

    public class LocationModel
    {
        public string Location { get; set; }
    }

    public class TokenResponse
    {
        [JsonProperty("token_type")] public string TokenType { get; set; }

        [JsonProperty("access_token")] public string AccessToken { get; set; }

        [JsonProperty("expires_in")] public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")] public string RefreshToken { get; set; }

        [JsonProperty("scope")] public string Scope { get; set; }
    }

    public class UserResponse
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("username")] public string Username { get; set; }

        [JsonProperty("avatar")] public string Avatar { get; set; }

        [JsonProperty("discriminator")] public string Discriminator { get; set; }

        [JsonProperty("public_flags")] public int PublicFlags { get; set; }

        [JsonProperty("flags")] public int Flags { get; set; }

        [JsonProperty("mfa_enabled")] public bool MfaEnabled { get; set; }

        [JsonProperty("locale")] public string Locale { get; set; }

        [JsonProperty("premium_type")] public int PremiumType { get; set; }
    }

    public class QueueItem
    {
        public string AccessToken { get; set; }
        public string UserId { get; set; }
        public string GuildId { get; set; }
    }
}