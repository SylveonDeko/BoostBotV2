using Newtonsoft.Json;

namespace BoostBotV2.Services.Impl.Models;

// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
    public class AddedBy
    {
        [JsonProperty("external_urls")]
        public ExternalUrls ExternalUrls;

        [JsonProperty("href")]
        public string Href;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("uri")]
        public string Uri;
    }

    public class Album
    {
        [JsonProperty("album_type")]
        public string AlbumType;

        [JsonProperty("artists")]
        public List<Artist> Artists;

        [JsonProperty("external_urls")]
        public ExternalUrls ExternalUrls;

        [JsonProperty("href")]
        public string Href;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("images")]
        public List<Image> Images;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("release_date")]
        public string ReleaseDate;

        [JsonProperty("release_date_precision")]
        public string ReleaseDatePrecision;

        [JsonProperty("total_tracks")]
        public int TotalTracks;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("uri")]
        public string Uri;
    }

    public class Artist
    {
        [JsonProperty("external_urls")]
        public ExternalUrls ExternalUrls;

        [JsonProperty("href")]
        public string Href;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("uri")]
        public string Uri;
    }

    public class ExternalIds
    {
        [JsonProperty("isrc")]
        public string Isrc;
    }

    public class ExternalUrls
    {
        [JsonProperty("spotify")]
        public string Spotify;
    }

    public class Image
    {
        [JsonProperty("height")]
        public int Height;

        [JsonProperty("url")]
        public string Url;

        [JsonProperty("width")]
        public int Width;
    }

    public class LinkedFrom
    {
        [JsonProperty("external_urls")]
        public ExternalUrls ExternalUrls;

        [JsonProperty("href")]
        public string Href;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("uri")]
        public string Uri;
    }

    public class Restrictions
    {
        [JsonProperty("reason")]
        public string Reason;
    }

    public class SpotifySong
    {
        [JsonProperty("added_at")]
        public DateTime AddedAt;

        [JsonProperty("added_by")]
        public AddedBy AddedBy;

        [JsonProperty("is_local")]
        public bool IsLocal;

        [JsonProperty("primary_color")]
        public object PrimaryColor;

        [JsonProperty("sharing_info")]
        public SharingInfo SharingInfo;

        [JsonProperty("track")]
        public Track Track;

        [JsonProperty("video_thumbnail")]
        public VideoThumbnail VideoThumbnail;
    }

    public class SharingInfo
    {
        [JsonProperty("share_id")]
        public string ShareId;

        [JsonProperty("share_url")]
        public string ShareUrl;

        [JsonProperty("uri")]
        public string Uri;
    }

    public class Track
    {
        [JsonProperty("album")]
        public Album Album;

        [JsonProperty("artists")]
        public List<Artist> Artists;

        [JsonProperty("disc_number")]
        public int DiscNumber;

        [JsonProperty("duration_ms")]
        public int DurationMs;

        [JsonProperty("episode")]
        public bool Episode;

        [JsonProperty("explicit")]
        public bool Explicit;

        [JsonProperty("external_ids")]
        public ExternalIds ExternalIds;

        [JsonProperty("external_urls")]
        public ExternalUrls ExternalUrls;

        [JsonProperty("href")]
        public string Href;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("is_local")]
        public bool IsLocal;

        [JsonProperty("is_playable")]
        public bool IsPlayable;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("popularity")]
        public int Popularity;

        [JsonProperty("preview_url")]
        public string PreviewUrl;

        [JsonProperty("track_number")]
        public int TrackNumber;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("uri")]
        public string Uri;

        [JsonProperty("linked_from")]
        public LinkedFrom LinkedFrom;

        [JsonProperty("restrictions")]
        public Restrictions Restrictions;
    }

    public class VideoThumbnail
    {
        [JsonProperty("url")]
        public object Url;
    }

