using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazorade.StaticPages.StaticGeneration;

internal static class StaticJsonLdSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Default,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static bool IsSupportedSchemaType(string? schemaType) =>
        schemaType is "WebPage" or "Article";

    public static string Serialize(
        string schemaType,
        string title,
        string? description,
        string? author,
        string? authorUrl,
        string? datePublished,
        string? image,
        string? url,
        string? keywords,
        string? copyrightNotice)
    {
        if (!IsSupportedSchemaType(schemaType))
        {
            throw new ArgumentException($"The schema type '{schemaType}' is not supported.", nameof(schemaType));
        }

        var document = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["@context"] = "https://schema.org",
            ["@type"] = schemaType,
            ["@id"] = CreateEntityId(url, schemaType),
            ["name"] = title,
            ["description"] = description,
            ["url"] = url,
            ["image"] = image,
            ["author"] = author is null
                ? null
                : new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["@type"] = "Person",
                    ["@id"] = CreateAuthorId(authorUrl),
                    ["name"] = author,
                    ["url"] = authorUrl
                },
            ["datePublished"] = datePublished,
            ["keywords"] = keywords,
            ["copyrightNotice"] = copyrightNotice
        };

        if (schemaType == "Article")
        {
            document["headline"] = title;
        }

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    private static string? CreateEntityId(string? url, string schemaType) =>
        url is null ? null : $"{url}#{schemaType.ToLowerInvariant()}";

    private static string? CreateAuthorId(string? authorUrl) =>
        authorUrl is null ? null : $"{authorUrl}#person";
}
