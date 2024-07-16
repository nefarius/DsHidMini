using System.Text.Json.Serialization;

namespace Nefarius.DsHidMini.ControlApp.Models.Util.Web;

public sealed class ArtifactMetaData
{
    [JsonPropertyName("Comments")]
    public string Comments { get; set; }

    [JsonPropertyName("CompanyName")]
    public string CompanyName { get; set; }

    [JsonPropertyName("FileDescription")]
    public string FileDescription { get; set; }

    [JsonPropertyName("FileVersion")]
    public string FileVersion { get; set; }

    [JsonPropertyName("InternalName")]
    public string InternalName { get; set; }

    [JsonPropertyName("LegalCopyright")]
    public string LegalCopyright { get; set; }

    [JsonPropertyName("LegalTrademarks")]
    public string LegalTrademarks { get; set; }

    [JsonPropertyName("OriginalFilename")]
    public string OriginalFilename { get; set; }

    [JsonPropertyName("PrivateBuild")]
    public string PrivateBuild { get; set; }

    [JsonPropertyName("ProductName")]
    public string ProductName { get; set; }

    [JsonPropertyName("ProductVersion")]
    public string ProductVersion { get; set; }

    [JsonPropertyName("SpecialBuild")]
    public string SpecialBuild { get; set; }
}