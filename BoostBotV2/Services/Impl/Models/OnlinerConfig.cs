using Newtonsoft.Json;

namespace BoostBotV2.Services.Impl.Models;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class CustomStatus
    {
        public int yes { get; set; }
        public int no { get; set; }
    }

    public class Games
    {
        public int Battlerite { get; set; }

        [JsonProperty("League of Legends")]
        public int LeagueofLegends { get; set; }

        [JsonProperty("PLAYERUNKNOWN'S BATTLEGROUNDS")]
        public int PLAYERUNKNOWNSBATTLEGROUNDS { get; set; }

        [JsonProperty("Counter-Strike: Global Offensive")]
        public int CounterStrikeGlobalOffensive { get; set; }
        public int Overwatch { get; set; }
        public int Minecraft { get; set; }

        [JsonProperty("World of Warcraft")]
        public int WorldofWarcraft { get; set; }

        [JsonProperty("Grand Theft Auto V")]
        public int GrandTheftAutoV { get; set; }

        [JsonProperty("Tom Clancy's Rainbow Six Siege")]
        public int TomClancysRainbowSixSiege { get; set; }

        [JsonProperty("Rocket League")]
        public int RocketLeague { get; set; }
    }

    public class Images
    {
        public string py { get; set; }
        public string js { get; set; }
    }

    public class JoinVoice
    {
        public int yes { get; set; }
        public int no { get; set; }
    }

    public class Livestream
    {
        public int yes { get; set; }
        public int no { get; set; }
    }

    public class RandomBio
    {
        public int yes { get; set; }
        public int no { get; set; }
    }

    public class OnlinerConfig
    {
        public string guild { get; set; }
        public bool update_bio { get; set; }
        public bool update_status { get; set; }
        public bool voice { get; set; }
        public Status status { get; set; }
        public CustomStatus custom_status { get; set; }
        public JoinVoice join_voice { get; set; }
        public Livestream livestream { get; set; }
        public RandomBio random_bio { get; set; }
        public List<string> vcs { get; set; }
        public Games games { get; set; }
        public VisualStudio visual_studio { get; set; }
    }

    public class Status
    {
        public int normal { get; set; }
        public int playing { get; set; }
        public int spotify { get; set; }
        public int visual_studio { get; set; }
    }

    public class VisualStudio
    {
        public Images images { get; set; }
        public List<string> workspaces { get; set; }
        public List<string> names { get; set; }
    }

