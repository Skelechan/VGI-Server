using System.Text.Json.Serialization;

namespace vgi.Server.Models;

public class TwitchVideo
{
    public string Title { get; set; }
    public string StreamThumbnail { get; set; }
    public string DisplayName { get; set; }
    public string Url { get; set; }
    [JsonIgnore] public string CreationDate { get; set; }
}