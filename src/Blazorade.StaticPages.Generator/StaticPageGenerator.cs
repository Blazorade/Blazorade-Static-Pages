using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Xml;
using Blazorade.StaticPages.StaticGeneration;

namespace Blazorade.StaticPages.Generator;

/// <summary>
/// Generates the initial static output for a compiled Blazor WebAssembly application.
/// </summary>
public sealed class StaticPageGenerator
{
    private const string StaticMetadataMarker = " data-blazorade-static-metadata";

    /// <summary>
    /// Generates route files and Static Web Apps configuration for an application assembly.
    /// </summary>
    /// <param name="options">The generation options.</param>
    /// <returns>The number of generated pages.</returns>
    public Task<int> GenerateAsync(StaticPageGeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var analyzedPages = new StaticSourcePageAnalyzer(options.ProjectDirectory).Analyze();
        var pages = analyzedPages
            .Select(page => new StaticPageInfo(page.Route, page.FilePath, page.PageName, page.Content, page.Metadata))
            .ToArray();
        var configuration = ReadConfiguration(options.ProjectDirectory, options.Configuration);
        var template = ReadHtmlTemplate(options.ProjectDirectory);

        Directory.CreateDirectory(options.OutputDirectory);
        RemoveStaleRssOutput(options.OutputDirectory, configuration?.Rss);

        foreach (var page in pages)
        {
            var metadata = page.Metadata;
            var outputPath = Path.Combine(options.OutputDirectory, page.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, CreateHtmlDocument(page, page.Content, metadata, configuration, options, template), Encoding.UTF8);
        }

        File.WriteAllText(
            Path.Combine(options.OutputDirectory, "staticwebapp.config.json"),
            CreateStaticWebAppsConfiguration(pages, configuration?.Rss),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        if (configuration?.SiteUrl is not null)
        {
            var sitemapPages = pages
                .Where(page => page.Metadata?.IncludeInSitemap != false)
                .Select(page => $"<url><loc>{EncodeXml(new Uri(new Uri(configuration.SiteUrl), page.Route.TrimStart('/')).ToString())}</loc></url>");
            File.WriteAllText(
                Path.Combine(options.OutputDirectory, "sitemap.xml"),
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">{string.Concat(sitemapPages)}</urlset>\n",
                Encoding.UTF8);
        }

        if (configuration?.Rss is { } rss)
        {
            var feedPath = Path.Combine(options.OutputDirectory, rss.File.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(feedPath)!);
            File.WriteAllText(feedPath, CreateRssFeed(pages, configuration.SiteUrl!, rss), new UTF8Encoding(false));
        }

        return Task.FromResult(pages.Length);
    }

    private static void RemoveStaleRssOutput(string outputDirectory, RssConfiguration? rss)
    {
        var statePath = Path.Combine(Directory.GetParent(outputDirectory)?.FullName ?? outputDirectory, "rss-output.path");
        var outputRoot = Path.GetFullPath(outputDirectory);
        if (File.Exists(statePath))
        {
            var previousFile = File.ReadAllText(statePath).Trim();
            if (!string.IsNullOrWhiteSpace(previousFile))
            {
                var previousPath = Path.GetFullPath(Path.Combine(outputRoot, previousFile.Replace('/', Path.DirectorySeparatorChar)));
                if (previousPath.StartsWith(outputRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(previousPath))
                {
                    File.Delete(previousPath);
                }
            }
        }

        if (rss is null)
        {
            if (File.Exists(statePath))
            {
                File.Delete(statePath);
            }

            return;
        }

        File.WriteAllText(statePath, rss.File, Encoding.UTF8);
    }

    private static string CreateFilePath(string route, string pageName)
    {
        if (route == "/")
        {
            return $"{pageName}.html";
        }

        return route.Trim('/').TrimEnd('/') + ".html";
    }

    private static string CreateHtmlDocument(
        StaticPageInfo page,
        string staticContent,
        StaticSourcePageAnalyzer.StaticPageMetadataValues metadata,
        StaticPagesConfiguration? configuration,
        StaticPageGeneratorOptions options,
        string template)
    {
        var title = EncodeHtml(metadata.Title);
        var canonicalUrl = configuration?.SiteUrl is null
            ? null
            : new Uri(new Uri(configuration.SiteUrl), page.Route.TrimStart('/')).ToString();
        var rssUrl = configuration?.Rss is { } configuredRss && configuration.SiteUrl is not null
            ? new Uri(new Uri(configuration.SiteUrl), configuredRss.Route.TrimStart('/')).ToString()
            : null;
        var rssTitle = configuration?.Rss is { } rss && configuration.SiteUrl is not null
            ? rss.Title ?? new Uri(configuration.SiteUrl).Host
            : null;
        var structuredData = metadata.SchemaType is null
            ? null
            : StaticJsonLdSerializer.Serialize(
                metadata.SchemaType,
                metadata.Title,
                metadata.Description,
                metadata.Author,
                ResolveAuthorUrl(metadata.AuthorUrl, canonicalUrl),
                metadata.Date is { } date ? StaticPageDateParser.FormatPublishedTime(date) : null,
                ResolveJsonLdImageUrl(metadata.Image, configuration?.SiteUrl),
                canonicalUrl,
                metadata.Keywords,
                metadata.CopyrightNotice);
        var bootstrapper = string.IsNullOrWhiteSpace(options.Bootstrapper)
            ? "_framework/blazor.webassembly.js"
            : options.Bootstrapper;

        var appContent = ContainsElement(template, "main") || ContainsElement(staticContent, "main")
            ? staticContent
            : WrapInMainElement(staticContent);
        var document = ContainsElement(template, "title")
            ? ReplaceElementContent(template, "title", title)
            : InsertBeforeClosingTag(template, "head", $"    <title>{title}</title>\n");
        document = AddAttributeToFirstElement(document, "title", StaticMetadataMarker);
        document = ReplaceElementContentById(document, "app", appContent);
        document = ReplaceBootstrapper(document, bootstrapper);

        var metadataMarkup =
            (metadata.Description is null ? string.Empty : $"    <meta name=\"description\" content=\"{EncodeHtml(metadata.Description)}\"{StaticMetadataMarker} />\n") +
            $"    <meta property=\"og:type\" content=\"website\"{StaticMetadataMarker} />\n" +
            $"    <meta property=\"og:title\" content=\"{title}\"{StaticMetadataMarker} />\n" +
            (metadata.Description is null ? string.Empty : $"    <meta property=\"og:description\" content=\"{EncodeHtml(metadata.Description)}\"{StaticMetadataMarker} />\n") +
            (metadata.Author is null ? string.Empty : $"    <meta name=\"author\" content=\"{EncodeHtml(metadata.Author)}\"{StaticMetadataMarker} />\n") +
            (canonicalUrl is null ? string.Empty : $"    <link rel=\"canonical\" href=\"{EncodeHtml(canonicalUrl)}\"{StaticMetadataMarker} />\n    <meta property=\"og:url\" content=\"{EncodeHtml(canonicalUrl)}\"{StaticMetadataMarker} />\n") +
            (rssUrl is null ? string.Empty : $"    <link rel=\"alternate\" type=\"application/rss+xml\" title=\"{EncodeHtml(rssTitle!)}\" href=\"{EncodeHtml(rssUrl)}\"{StaticMetadataMarker} />\n") +
            (metadata.Image is null ? string.Empty : $"    <meta property=\"og:image\" content=\"{EncodeHtml(ResolveUrl(metadata.Image, configuration?.SiteUrl))}\"{StaticMetadataMarker} />\n    <meta name=\"twitter:image\" content=\"{EncodeHtml(ResolveUrl(metadata.Image, configuration?.SiteUrl))}\"{StaticMetadataMarker} />\n") +
            (metadata.Locale is null ? string.Empty : $"    <meta property=\"og:locale\" content=\"{EncodeHtml(metadata.Locale.Replace('-', '_'))}\"{StaticMetadataMarker} />\n") +
            (metadata.Date is null ? string.Empty : $"    <meta property=\"article:published_time\" content=\"{StaticPageDateParser.FormatPublishedTime(metadata.Date.Value)}\"{StaticMetadataMarker} />\n    <meta name=\"date\" content=\"{StaticPageDateParser.FormatDate(metadata.Date.Value)}\"{StaticMetadataMarker} />\n") +
            (structuredData is null ? string.Empty : $"    <script type=\"application/ld+json\"{StaticMetadataMarker}>{structuredData}</script>\n") +
            $"    <meta name=\"twitter:card\" content=\"summary_large_image\"{StaticMetadataMarker} />\n" +
            $"    <meta name=\"twitter:title\" content=\"{title}\"{StaticMetadataMarker} />\n" +
            (metadata.Description is null ? string.Empty : $"    <meta name=\"twitter:description\" content=\"{EncodeHtml(metadata.Description)}\"{StaticMetadataMarker} />\n");

        return InsertBeforeClosingTag(document, "head", metadataMarkup);
    }

    private static string WrapInMainElement(string content)
    {
        return $"<main>\n{content}</main>";
    }

    private static string ReadHtmlTemplate(string projectDirectory)
    {
        var path = Path.Combine(projectDirectory, "wwwroot", "index.html");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"The Blazor application HTML template '{path}' was not found.");
        }

        return File.ReadAllText(path);
    }

    private static string ReplaceElementContent(string document, string elementName, string content)
    {
        var openingStart = document.IndexOf($"<{elementName}", StringComparison.OrdinalIgnoreCase);
        if (openingStart < 0)
        {
            throw new InvalidOperationException($"The HTML template does not contain a <{elementName}> element.");
        }

        var openingEnd = document.IndexOf('>', openingStart);
        var closingStart = document.IndexOf($"</{elementName}>", openingEnd + 1, StringComparison.OrdinalIgnoreCase);
        if (openingEnd < 0 || closingStart < 0)
        {
            throw new InvalidOperationException($"The HTML template contains an incomplete <{elementName}> element.");
        }

        return document[..(openingEnd + 1)] + content + document[closingStart..];
    }

    private static string AddAttributeToFirstElement(string document, string elementName, string attribute)
    {
        var openingStart = document.IndexOf($"<{elementName}", StringComparison.OrdinalIgnoreCase);
        if (openingStart < 0)
        {
            return document;
        }

        var openingEnd = document.IndexOf('>', openingStart);
        if (openingEnd < 0)
        {
            throw new InvalidOperationException($"The document contains an incomplete <{elementName}> element.");
        }

        return document[..openingEnd] + attribute + document[openingEnd..];
    }

    private static string ReplaceElementContentById(string document, string id, string content)
    {
        var openingStart = document.IndexOf("<div id=\"" + id + "\"", StringComparison.OrdinalIgnoreCase);
        if (openingStart < 0)
        {
            throw new InvalidOperationException($"The HTML template does not contain a <div id=\"{id}\"> element.");
        }

        var openingEnd = document.IndexOf('>', openingStart);
        if (openingEnd < 0)
        {
            throw new InvalidOperationException("The HTML template contains an incomplete app element.");
        }

        var closingStart = FindMatchingClosingDiv(document, openingEnd);
        return document[..(openingEnd + 1)] + content + document[closingStart..];
    }

    private static bool ContainsElement(string document, string elementName)
    {
        var searchStart = 0;
        while (true)
        {
            var elementStart = document.IndexOf('<' + elementName, searchStart, StringComparison.OrdinalIgnoreCase);
            if (elementStart < 0)
            {
                return false;
            }

            var nameEnd = elementStart + elementName.Length + 1;
            if (nameEnd < document.Length && (char.IsWhiteSpace(document[nameEnd]) || document[nameEnd] is '>' or '/'))
            {
                return true;
            }

            searchStart = nameEnd;
        }
    }

    private static int FindMatchingClosingDiv(string document, int openingEnd)
    {
        var depth = 1;
        var position = openingEnd + 1;
        while (position < document.Length)
        {
            var nextOpening = document.IndexOf("<div", position, StringComparison.OrdinalIgnoreCase);
            var nextClosing = document.IndexOf("</div", position, StringComparison.OrdinalIgnoreCase);
            if (nextClosing < 0)
            {
                break;
            }

            if (nextOpening >= 0 && nextOpening < nextClosing)
            {
                depth++;
                position = document.IndexOf('>', nextOpening) + 1;
                continue;
            }

            depth--;
            if (depth == 0)
            {
                return nextClosing;
            }

            position = document.IndexOf('>', nextClosing) + 1;
        }

        throw new InvalidOperationException("The HTML template contains an incomplete app element.");
    }

    private static string ReplaceBootstrapper(string document, string bootstrapper)
    {
        var encodedBootstrapper = System.Net.WebUtility.HtmlEncode(bootstrapper);
        var placeholders = new[]
        {
            "_framework/blazor.webassembly#[.{fingerprint}].js",
            "_framework/blazor.webassembly.js"
        };

        foreach (var placeholder in placeholders)
        {
            if (document.Contains(placeholder, StringComparison.Ordinal))
            {
                return document.Replace(placeholder, encodedBootstrapper, StringComparison.Ordinal);
            }
        }

        return InsertBeforeClosingTag(document, "body", $"    <script src=\"{encodedBootstrapper}\"></script>\n");
    }

    private static string InsertBeforeClosingTag(string document, string elementName, string content)
    {
        var closingTag = $"</{elementName}>";
        var closingStart = document.LastIndexOf(closingTag, StringComparison.OrdinalIgnoreCase);
        if (closingStart < 0)
        {
            throw new InvalidOperationException($"The HTML template does not contain a closing </{elementName}> tag.");
        }

        return document[..closingStart] + content + document[closingStart..];
    }

    private static string CreateRssFeed(IReadOnlyCollection<StaticPageInfo> pages, string siteUrl, RssConfiguration rss)
    {
        var items = pages
            .Where(page => page.Metadata.IncludeInRss && page.Metadata.Date.HasValue)
            .OrderByDescending(page => page.Metadata.Date)
            .Take(rss.ItemCount)
            .ToArray();

        var channelTitle = rss.Title ?? new Uri(siteUrl).Host;
        var channelDescription = rss.Description ?? $"RSS feed for {new Uri(siteUrl).Host}";
        var channelLink = new Uri(new Uri(siteUrl), rss.Route.TrimStart('/')).ToString();

        var builder = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            NewLineChars = "\n",
            OmitXmlDeclaration = true
        };

        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "atom", null, "http://www.w3.org/2005/Atom");
            writer.WriteAttributeString("xmlns", "content", null, "http://purl.org/rss/1.0/modules/content/");

            writer.WriteStartElement("channel");
            writer.WriteElementString("title", channelTitle);
            writer.WriteElementString("description", channelDescription);
            writer.WriteElementString("link", channelLink);
            writer.WriteStartElement("atom", "link", "http://www.w3.org/2005/Atom");
            writer.WriteAttributeString("rel", "self");
            writer.WriteAttributeString("type", "application/rss+xml");
            writer.WriteAttributeString("href", channelLink);
            writer.WriteEndElement();

            foreach (var page in items)
            {
                var link = new Uri(new Uri(siteUrl), page.Route.TrimStart('/')).ToString();
                var publicationDate = page.Metadata.Date!.Value.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);

                writer.WriteStartElement("item");
                writer.WriteElementString("title", page.Metadata.Title);
                writer.WriteElementString("description", page.Metadata.Description ?? string.Empty);
                writer.WriteElementString("link", link);
                writer.WriteStartElement("guid");
                writer.WriteAttributeString("isPermaLink", "true");
                writer.WriteString(link);
                writer.WriteEndElement();
                writer.WriteElementString("pubDate", publicationDate);

                if (rss.IncludeContent)
                {
                    writer.WriteStartElement("content", "encoded", "http://purl.org/rss/1.0/modules/content/");
                    writer.WriteRaw(CreateCData(page.Content));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n{builder}";
    }

    private static string CreateCData(string content)
    {
        var safeContent = content.Replace("]]>", "]]]]><![CDATA[>", StringComparison.Ordinal);
        return $"<![CDATA[{safeContent}]]>";
    }

    private static string CreateStaticWebAppsConfiguration(IReadOnlyCollection<StaticPageInfo> pages, RssConfiguration? rss)
    {
        var routes = pages.Select(page => new StaticWebAppsRoute(page.Route, "/" + page.FilePath)).ToList();
        if (rss is not null)
        {
            routes.Add(new StaticWebAppsRoute(rss.Route, "/" + rss.File));
        }
        var configuration = new StaticWebAppsConfiguration(routes);
        if (rss is not null)
        {
            configuration.NavigationFallback.Exclude = configuration.NavigationFallback.Exclude
                .Concat([rss.Route, "/" + rss.File])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return JsonSerializer.Serialize(configuration, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static StaticPagesConfiguration? ReadConfiguration(string projectDirectory, string? configuration)
    {
        var paths = new[]
        {
            Path.Combine(projectDirectory, "blazorade.config.json"),
            string.IsNullOrWhiteSpace(configuration)
                ? null
                : Path.Combine(projectDirectory, $"blazorade.config.{configuration}.json")
        }
        .Where(path => path is not null && File.Exists(path))
        .Cast<string>()
        .ToArray();

        if (paths.Length == 0)
        {
            return null;
        }

        var mergedDocument = new JsonObject();
        foreach (var path in paths)
        {
            JsonNode document;
            try
            {
                document = JsonNode.Parse(File.ReadAllText(path))
                    ?? throw new InvalidOperationException("The configuration file is empty.");
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"The configuration file '{path}' contains invalid JSON.", exception);
            }

            if (document is not JsonObject configurationObject)
            {
                throw new InvalidOperationException($"The configuration file '{path}' must contain a JSON object.");
            }

            MergeConfiguration(mergedDocument, configurationObject);
        }

        var deserializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserialized = mergedDocument.Deserialize<BlazoradeConfiguration>(deserializationOptions);
        var staticPages = deserialized?.StaticPages;
        var siteUrl = staticPages?.SiteUrl;
        var configuredRss = staticPages is null ? null : staticPages.Rss ?? new RssSection();
        RssConfiguration? rss = null;
        if (configuredRss is not null && configuredRss.Enabled)
        {
            if (string.IsNullOrWhiteSpace(siteUrl))
            {
                throw new InvalidOperationException("The 'staticPages.siteUrl' value is required when RSS generation is enabled.");
            }

            rss = ValidateRss(configuredRss, paths[^1]);
        }
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            return new StaticPagesConfiguration(null, rss);
        }

        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException($"The 'staticPages.siteUrl' value in '{paths[^1]}' must be an absolute URL with a host.");
        }

        return new StaticPagesConfiguration(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/", rss);
    }

    private static RssConfiguration ValidateRss(RssSection rss, string path)
    {
        if (rss.ItemCount <= 0)
        {
            throw new InvalidOperationException($"The 'staticPages.rss.itemCount' value in '{path}' must be a positive integer.");
        }

        var configuredFile = rss.File ?? "feed.xml";
        var file = configuredFile.Replace('\\', '/').TrimStart('/');
        if (Path.IsPathRooted(configuredFile) || string.IsNullOrWhiteSpace(file) || file.Split('/').Any(part => part is "" or "." or ".."))
        {
            throw new InvalidOperationException($"The 'staticPages.rss.file' value in '{path}' must be a relative path within the generated web root.");
        }

        var route = (rss.Route ?? "/feed").Trim();
        if (string.IsNullOrWhiteSpace(route) || !route.StartsWith('/') || route.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The 'staticPages.rss.route' value in '{path}' must be an absolute site-relative path.");
        }

        return new RssConfiguration(file, route, rss.ItemCount, rss.IncludeContent, rss.Title, rss.Description);
    }

    private static void MergeConfiguration(JsonObject target, JsonObject source)
    {
        foreach (var property in source)
        {
            if (property.Value is JsonObject sourceObject && target[property.Key] is JsonObject targetObject)
            {
                MergeConfiguration(targetObject, sourceObject);
            }
            else
            {
                target[property.Key] = property.Value?.DeepClone();
            }
        }
    }

    private static string ResolveUrl(string value, string? siteUrl)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _)
            ? value
            : siteUrl is null ? value : new Uri(new Uri(siteUrl), value.TrimStart('/')).ToString();
    }

    private static string? ResolveAuthorUrl(string? value, string? canonicalUrl)
    {
        if (value is null)
        {
            return null;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            return absolute.AbsoluteUri;
        }

        if (canonicalUrl is null)
        {
            throw new InvalidOperationException("A canonical site URL is required to resolve a relative StaticMetadata AuthorUrl for JSON-LD.");
        }

        return new Uri(new Uri(canonicalUrl), value).AbsoluteUri;
    }

    private static string? ResolveJsonLdImageUrl(string? value, string? siteUrl)
    {
        if (value is null)
        {
            return null;
        }

        var resolved = ResolveUrl(value, siteUrl);
        if (!Uri.TryCreate(resolved, UriKind.Absolute, out var absolute))
        {
            throw new InvalidOperationException("An absolute site URL is required to resolve a relative StaticMetadata Image for JSON-LD.");
        }

        return absolute.AbsoluteUri;
    }

    private static string EncodeHtml(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static string EncodeXml(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;

    private sealed record StaticPageInfo(string Route, string FilePath, string PageName, string Content, StaticSourcePageAnalyzer.StaticPageMetadataValues Metadata);

    private sealed record StaticPagesConfiguration(string? SiteUrl, RssConfiguration? Rss);

    private sealed record BlazoradeConfiguration(StaticPagesSection? StaticPages);

    private sealed record StaticPagesSection(string? SiteUrl, RssSection? Rss);

    private sealed class RssSection
    {
        public bool Enabled { get; init; } = true;
        public string? File { get; init; }
        public string? Route { get; init; }
        public int ItemCount { get; init; } = 20;
        public bool IncludeContent { get; init; } = true;
        public string? Title { get; init; }
        public string? Description { get; init; }
    }

    private sealed record RssConfiguration(string File, string Route, int ItemCount, bool IncludeContent, string? Title, string? Description);

    private sealed record StaticWebAppsConfiguration(
        IEnumerable<StaticWebAppsRoute> Routes,
        StaticWebAppsNavigationFallback NavigationFallback = null!)
    {
        public StaticWebAppsNavigationFallback NavigationFallback { get; } = NavigationFallback ?? new();
    }

    private sealed record StaticWebAppsRoute(string Route, string Rewrite);

    private sealed class StaticWebAppsNavigationFallback
    {
        public string Rewrite { get; init; } = "/index.html";

        public string[] Exclude { get; set; } =
        [
            "/*.html", "/css/*", "/js/*", "/lib/*", "/sitemap.xml",
            "/*.{png,ico,svg,gif,woff,woff2,ttf,json}", "/*.pdf", "/*.svg",
            "/*.{css,scss,js,png,gif,ico,jpg,svg,wasm,dll,dat,blat,pdb,woff,woff2,ttf,eot}",
            "/assets/*", "/_content/*", "/_framework/*"
        ];
    }
}

/// <summary>
/// Specifies the inputs and output location for static page generation.
/// </summary>
/// <param name="ApplicationAssemblyPath">The compiled application assembly path.</param>
/// <param name="OutputDirectory">The generated output directory.</param>
/// <param name="Bootstrapper">The relative Blazor bootstrapper path.</param>
/// <param name="Configuration">The active MSBuild build configuration.</param>
public sealed record StaticPageGeneratorOptions(
    string ApplicationAssemblyPath,
    string OutputDirectory,
    string ProjectDirectory,
    string? Bootstrapper = null,
    string? Configuration = null);