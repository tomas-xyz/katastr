
using System.Text.Json.Serialization;

namespace tomxyz.katastr;

public class ChromeDriverDownloads
{
    [JsonPropertyName("downloads")]
    public ChromeDriverDrivers Driver { get; set; } = new ChromeDriverDrivers();
}


public class ChromeDriverDrivers
{
    [JsonPropertyName("chromedriver")]
    public List<ChromeDriverUrl> Platforms { get; set; } = new List<ChromeDriverUrl>();
}

public class ChromeDriverUrl
{
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
