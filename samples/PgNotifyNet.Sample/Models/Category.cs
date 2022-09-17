using System.Text.Json.Serialization;

namespace PgNotifyNet.Sample.Models;
public class Category
{
    public string? Description { get; set; }

    [JsonPropertyName("category_id")]
    public long Id { get; set; }

    [JsonPropertyName("category_name")]
    public string? Name { get; set; }
}
