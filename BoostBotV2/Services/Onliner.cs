using System.Net.WebSockets;
using System.Text;
using BoostBotV2.Services.Impl.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BoostBotV2.Services;

public class Onliner
{
    private readonly string _token;
    private readonly ClientWebSocket _ws;
    private readonly OnlinerConfig _config;

    private static readonly Lazy<List<SpotifySong>> Songs = new(() => JsonConvert.DeserializeObject<List<SpotifySong>>(File.ReadAllText("spotify songs.json")));
    private static readonly Lazy<List<string>> CustomStatuses = new(() => new List<string>(File.ReadAllLines("custom status.txt")));
    private static readonly Lazy<List<string>> Bios = new(() => new List<string>(File.ReadAllLines("user bios.txt")));

    private readonly Random _random = new();

    public Onliner(string token)
    {
        _ws = new ClientWebSocket();
        _token = token;
        _config = JsonConvert.DeserializeObject<OnlinerConfig>(File.ReadAllText("config.json"));
    }

    public async Task Connect()
    {
        await EstablishWebSocket();
        var heartbeatInterval = await ExtractHeartbeatInterval();
        await Identify();
        await HandleVoiceConnection();
        await MaintainConnection(heartbeatInterval);
    }

    private async Task EstablishWebSocket()
    {
        if (_ws.State is WebSocketState.None or WebSocketState.Closed)
        {
            await _ws.ConnectAsync(new Uri("wss://gateway.discord.gg/?v=6&encoding=json"), CancellationToken.None);
        }
    }

    private async Task<int> ExtractHeartbeatInterval()
    {
        var response = await ReceiveWebSocketMessage();
        var data = JObject.Parse(response);
        return data["d"]["heartbeat_interval"].Value<int>();
    }

    private async Task<string> ReceiveWebSocketMessage()
    {
        var buffer = new ArraySegment<byte>(new byte[8192]);
        var result = await _ws.ReceiveAsync(buffer, CancellationToken.None);
        return Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
    }

    private async Task Identify()
    {
        var device = DecideActionBasedOnWeight(new Dictionary<string, int>
        {
            { "Discord iOS", 25 },
            { "Windows", 75 }
        });

        var identificationPayload = new
        {
            op = 2,
            d = new
            {
                token = _token,
                properties = new
                {
                    os = device,
                    browser = device,
                    device = device
                }
            },
            s = (string)null,
            t = (string)null
        };

        await SendWebSocketMessage(identificationPayload);
    }

    private async Task HandleVoiceConnection()
    {
        var joinVoiceDecision = DecideActionBasedOnWeight(new Dictionary<string, int>
        {
            { "yes", _config.join_voice.yes },
            { "no", _config.join_voice.no }
        });
        if (_config.voice && joinVoiceDecision == "yes")
        {
            var channel = _config.vcs[_random.Next(_config.vcs.Count)];
            var voicePayload = new
            {
                op = 4,
                d = new
                {
                    guild_id = _config.guild,
                    channel_id = channel,
                    self_mute = RandomChoice(new[] { true, false }),
                    self_deaf = RandomChoice(new[] { true, false })
                }
            };
            await SendWebSocketMessage(voicePayload);

            var livestreamDecision = DecideActionBasedOnWeight(new Dictionary<string, int>
            {
                { "yes", _config.livestream.yes },
                { "no", _config.livestream.no }
            });

            if (livestreamDecision == "yes")
            {
                var livestreamPayload = new
                {
                    op = 18,
                    d = new
                    {
                        type = "guild",
                        guild_id = _config.guild,
                        channel_id = channel,
                        preferred_region = "singapore"
                    }
                };
                await SendWebSocketMessage(livestreamPayload);
            }
        }
    }

    private async Task MaintainConnection(int heartbeatInterval)
    {
        while (true)
        {
            await Task.Delay(heartbeatInterval);
            try
            {
                await SendHeartbeat();
                await ManageStatus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                if (_ws.State is WebSocketState.Closed or WebSocketState.Aborted)
                {
                    Console.WriteLine("Attempting to reconnect in 5 seconds...");
                    await Task.Delay(5000); // Wait for 5 seconds before trying to reconnect
                    await Connect();
                }
                // Avoid infinite recursion by not calling MaintainConnection() here again
            }
        }
    }



    private async Task SendHeartbeat()
    {
        var heartbeatPayload = new { op = 1, d = (object)null };
        await SendWebSocketMessage(heartbeatPayload);
    }

    private T RandomChoice<T>(IReadOnlyList<T> choices)
    {
        return choices[_random.Next(choices.Count)];
    }

    private async Task SetRandomActivity()
    {
        var type = DecideActionBasedOnWeight(new Dictionary<string, int>
        {
            { "normal", _config.status.normal },
            { "playing", _config.status.playing },
            { "spotify", _config.status.spotify },
            { "visual_studio", _config.status.visual_studio }
        });

        List<object> activities = new List<object>();

        switch (type)
        {
            case "normal":
                activities = new List<object>();
                break;
            case "playing":
                var gamesDict = _config.games.GetType()
                    .GetProperties()
                    .ToDictionary(
                        prop => prop.Name,
                        prop => (int)prop.GetValue(_config.games)
                    );
                var game = DecideActionBasedOnWeight(gamesDict);
                activities.Add(new
                {
                    type = 0,
                    timestamps = new { start = RandomTime() },
                    name = game
                });
                break;
            case "spotify":
                var song = RandomChoice(Songs.Value);
                activities.Add(new
                {
                    type = 2,
                    name = "Spotify",
                    assets = new
                    {
                        large_image = "spotify:" + song.Track.Album.Images[0].Url.Split("https://i.scdn.co/image/")[1],
                        large_text = song.Track.Album.Name
                    },
                    details = song.Track.Name,
                    state = song.Track.Artists[0].Name,
                    timestamps = new
                    {
                        start = DateTime.UtcNow.Ticks,
                        end = DateTime.UtcNow.AddMilliseconds(song.Track.DurationMs).Ticks
                    },
                    party = new { id = "spotify:" + Guid.NewGuid().ToString() },
                    sync_id = song.Track.ExternalUrls.Spotify.Split("https://open.spotify.com/track/")[1],
                    flags = 48,
                    metadata = new
                    {
                        album_id = song.Track.Album.Id,
                        artist_ids = song.Track.Artists.Select(a => a.Id).ToList()
                    }
                });
                break;
            case "visual_studio":
                var workspace = RandomChoice(_config.visual_studio.workspaces);
                var filename = RandomChoice(_config.visual_studio.names);
                var fileExtension = filename.Split('.')[1];

                var largeImage = fileExtension switch
                {
                    "py" => _config.visual_studio.images.py,
                    "js" => _config.visual_studio.images.js,
                    _ => ""
                };

                activities.Add(new
                {
                    type = 0,
                    name = "Visual Studio Code",
                    state = $"Workspace: {workspace}",
                    details = $"Editing {filename}",
                    application_id = "383226320970055681",
                    timestamps = new { start = RandomTime() },
                    assets = new
                    {
                        small_text = "Visual Studio Code",
                        small_image = "565945770067623946",
                        large_image = largeImage
                    }
                });
                break;
        }

        // If custom status updating is enabled, add it to activities
        if (_config.update_status && DecideActionBasedOnWeight(new Dictionary<string, int> { { "yes", _config.custom_status.yes }, { "no", _config.custom_status.no } }) == "yes")
        {
            activities.Add(new
            {
                type = 4,
                state = RandomChoice(CustomStatuses.Value),
                name = "Custom Status",
                id = "custom",
                emoji = new { id = (string)null, name = "😃", animated = false }
            });
        }

        // Final payload
        var payload = new
        {
            op = 3,
            d = new
            {
                since = 0,
                activities,
                status = RandomChoice(new List<string> { "online", "dnd", "idle" }),
                afk = false
            }
        };

        await SendWebSocketMessage(payload);
    }

    private long RandomTime()
    {
        var randomMinutes = _random.Next(0, 24 * 60); // Random number of minutes up to 24 hours
        var randomTimeAgo = DateTime.UtcNow.AddMinutes(-randomMinutes);
        return randomTimeAgo.Ticks;
    }


    private async Task ManageStatus()
    {
        await SetRandomActivity();
    }

    private async Task SendWebSocketMessage(object payload)
    {
        var payloadString = JsonConvert.SerializeObject(payload);
        await _ws.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(payloadString)),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken: CancellationToken.None
        );
    }

    private string DecideActionBasedOnWeight(Dictionary<string, int> weightedChoices)
    {
        var totalWeight = weightedChoices.Values.Sum();

        var randomNumber = _random.Next(totalWeight);
        foreach (var choice in weightedChoices)
        {
            if (randomNumber < choice.Value)
            {
                return choice.Key;
            }

            randomNumber -= choice.Value;
        }

        return weightedChoices.Keys.Last(); // default to the last key if all else fails
    }
}