using Newtonsoft.Json;
namespace ApiAggregation.Infrastructure.Clients.ClientModels
{
    public class RestNewsClientModel
    {
        [JsonProperty("status")]
        public string Status { get; set; } = default!;

        [JsonProperty("totalResults")]
        public long TotalResults { get; set; }

        [JsonProperty("articles")]
        public List<Article> Articles { get; set; } = default!;
    }

    public class Article
    {
        [JsonProperty("source")]
        public Source Source { get; set; } = default!;

        [JsonProperty("author")]
        public string Author { get; set; } = default!;

        [JsonProperty("title")]
        public string Title { get; set; } = default!;

        [JsonProperty("description")]
        public string Description { get; set; } = default!;

        [JsonProperty("url")]
        public Uri Url { get; set; } = default!;

        [JsonProperty("urlToImage")]
        public Uri UrlToImage { get; set; } = default!;

        [JsonProperty("publishedAt")]
        public DateTimeOffset PublishedAt { get; set; } = default!;

        [JsonProperty("content")]
        public string Content { get; set; } = default!;
    }

    public class Source
    {
        [JsonProperty("id")]
        public string Id { get; set; } = default!;

        [JsonProperty("name")]
        public string Name { get; set; } = default!;
    }
}