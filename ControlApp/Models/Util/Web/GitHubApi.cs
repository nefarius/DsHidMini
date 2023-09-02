using Newtonsoft.Json;

namespace Nefarius.DsHidMini.ControlApp.Models.Util.Web
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Author
    {
        [JsonProperty("login")] public string Login { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("node_id")] public string NodeId { get; set; }

        [JsonProperty("avatar_url")] public string AvatarUrl { get; set; }

        [JsonProperty("gravatar_id")] public string GravatarId { get; set; }

        [JsonProperty("url")] public string Url { get; set; }

        [JsonProperty("html_url")] public string HtmlUrl { get; set; }

        [JsonProperty("followers_url")] public string FollowersUrl { get; set; }

        [JsonProperty("following_url")] public string FollowingUrl { get; set; }

        [JsonProperty("gists_url")] public string GistsUrl { get; set; }

        [JsonProperty("starred_url")] public string StarredUrl { get; set; }

        [JsonProperty("subscriptions_url")] public string SubscriptionsUrl { get; set; }

        [JsonProperty("organizations_url")] public string OrganizationsUrl { get; set; }

        [JsonProperty("repos_url")] public string ReposUrl { get; set; }

        [JsonProperty("events_url")] public string EventsUrl { get; set; }

        [JsonProperty("received_events_url")] public string ReceivedEventsUrl { get; set; }

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("site_admin")] public bool SiteAdmin { get; set; }
    }

    public class Uploader
    {
        [JsonProperty("login")] public string Login { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("node_id")] public string NodeId { get; set; }

        [JsonProperty("avatar_url")] public string AvatarUrl { get; set; }

        [JsonProperty("gravatar_id")] public string GravatarId { get; set; }

        [JsonProperty("url")] public string Url { get; set; }

        [JsonProperty("html_url")] public string HtmlUrl { get; set; }

        [JsonProperty("followers_url")] public string FollowersUrl { get; set; }

        [JsonProperty("following_url")] public string FollowingUrl { get; set; }

        [JsonProperty("gists_url")] public string GistsUrl { get; set; }

        [JsonProperty("starred_url")] public string StarredUrl { get; set; }

        [JsonProperty("subscriptions_url")] public string SubscriptionsUrl { get; set; }

        [JsonProperty("organizations_url")] public string OrganizationsUrl { get; set; }

        [JsonProperty("repos_url")] public string ReposUrl { get; set; }

        [JsonProperty("events_url")] public string EventsUrl { get; set; }

        [JsonProperty("received_events_url")] public string ReceivedEventsUrl { get; set; }

        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("site_admin")] public bool SiteAdmin { get; set; }
    }

    public class Asset
    {
        [JsonProperty("url")] public string Url { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("node_id")] public string NodeId { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("label")] public object Label { get; set; }

        [JsonProperty("uploader")] public Uploader Uploader { get; set; }

        [JsonProperty("content_type")] public string ContentType { get; set; }

        [JsonProperty("state")] public string State { get; set; }

        [JsonProperty("size")] public int Size { get; set; }

        [JsonProperty("download_count")] public int DownloadCount { get; set; }

        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; }

        [JsonProperty("browser_download_url")] public string BrowserDownloadUrl { get; set; }
    }

    public class Root
    {
        [JsonProperty("url")] public string Url { get; set; }

        [JsonProperty("assets_url")] public string AssetsUrl { get; set; }

        [JsonProperty("upload_url")] public string UploadUrl { get; set; }

        [JsonProperty("html_url")] public string HtmlUrl { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("author")] public Author Author { get; set; }

        [JsonProperty("node_id")] public string NodeId { get; set; }

        [JsonProperty("tag_name")] public string TagName { get; set; }

        [JsonProperty("target_commitish")] public string TargetCommitish { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("draft")] public bool Draft { get; set; }

        [JsonProperty("prerelease")] public bool Prerelease { get; set; }

        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }

        [JsonProperty("published_at")] public DateTime PublishedAt { get; set; }

        [JsonProperty("assets")] public List<Asset> Assets { get; set; }

        [JsonProperty("tarball_url")] public string TarballUrl { get; set; }

        [JsonProperty("zipball_url")] public string ZipballUrl { get; set; }

        [JsonProperty("body")] public string Body { get; set; }
    }
}