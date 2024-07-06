using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace Nefarius.DsHidMini.Setup;

public class Detection
    {
        [JsonProperty("$type")]
        public string Type { get; set; }

        [JsonProperty("view")]
        public string View { get; set; }

        [JsonProperty("hive")]
        public string Hive { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class ExitCode
    {
        [JsonProperty("skipCheck")]
        public bool SkipCheck { get; set; }

        [JsonProperty("successCodes")]
        public List<int> SuccessCodes { get; set; }
    }

    public class Instance
    {
        [JsonProperty("helpUrl")]
        public string HelpUrl { get; set; }
    }

    public class Release
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("publishedAt")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("downloadSize")]
        public int DownloadSize { get; set; }

        [JsonProperty("launchArguments")]
        public string LaunchArguments { get; set; }

        [JsonProperty("exitCode")]
        public ExitCode ExitCode { get; set; }
    }

    public class UpdateResponse
    {
        [JsonProperty("instance")]
        public Instance Instance { get; set; }

        [JsonProperty("shared")]
        public Shared Shared { get; set; }

        [JsonProperty("releases")]
        public List<Release> Releases { get; set; }
    }

    public class Shared
    {
        [JsonProperty("windowTitle")]
        public string WindowTitle { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("detectionMethod")]
        public string DetectionMethod { get; set; }

        [JsonProperty("detection")]
        public Detection Detection { get; set; }
    }

