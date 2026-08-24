using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazorade.StaticPages;
using Blazorade.StaticPages.Components;
using Blazorade.StaticPages.StaticGeneration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blazorade.StaticPages.Generator;

/// <summary>
/// Generates the initial static output for a compiled Blazor WebAssembly application.
/// </summary>
public sealed class StaticPageGenerator
{
    /// <summary>
    /// Generates route files and Static Web Apps configuration for an application assembly.
    /// </summary>
    /// <param name="options">The generation options.</param>
    /// <returns>The number of generated pages.</returns>
    public async Task<int> GenerateAsync(StaticPageGeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var assembly = Assembly.LoadFrom(options.ApplicationAssemblyPath);
        var pages = DiscoverPages(assembly).ToArray();
        var metadataCapture = new StaticPageMetadataCapture();
        var services = new ServiceCollection()
            .AddSingleton(new StaticPageRenderContext(isStaticGeneration: true))
            .AddSingleton<IStaticPageMetadataSink>(metadataCapture)
            .BuildServiceProvider();
        var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        var configuration = ReadConfiguration(options.ProjectDirectory);

        Directory.CreateDirectory(options.OutputDirectory);

        foreach (var page in pages)
        {
            metadataCapture.Reset();
            var staticContent = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var renderedPage = await renderer.RenderComponentAsync(page.ComponentType);
                return renderedPage.ToHtmlString();
            });
            var metadata = metadataCapture.Current
                ?? throw new InvalidOperationException($"The static page component '{page.PageName}' did not provide metadata.");
            page.Metadata = metadata;
            var outputPath = Path.Combine(options.OutputDirectory, page.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, CreateHtmlDocument(page, staticContent, metadata, configuration, options), Encoding.UTF8);
        }

        File.WriteAllText(
            Path.Combine(options.OutputDirectory, "staticwebapp.config.json"),
            CreateStaticWebAppsConfiguration(pages),
            Encoding.UTF8);

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

        return pages.Length;
    }

    private static IEnumerable<StaticPageInfo> DiscoverPages(Assembly assembly)
    {
        foreach (var componentType in assembly.GetExportedTypes()
                     .Where(type => typeof(IComponent).IsAssignableFrom(type)))
        {
            var routes = componentType.GetCustomAttributes<RouteAttribute>();
            foreach (var route in routes)
            {
                if (route.Template.Contains('{'))
                {
                    throw new InvalidOperationException(
                        $"The static page route '{route.Template}' on '{componentType.FullName}' " +
                        "contains a route parameter, which is not supported yet.");
                }

                var filePath = CreateFilePath(route.Template, componentType.Name);
                yield return new StaticPageInfo(route.Template, filePath, componentType.Name, componentType);
            }
        }
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
        StaticPageMetadata metadata,
        StaticPagesConfiguration? configuration,
        StaticPageGeneratorOptions options)
    {
        var title = EncodeHtml(metadata.Title);
        var canonicalUrl = configuration?.SiteUrl is null
            ? null
            : new Uri(new Uri(configuration.SiteUrl), page.Route.TrimStart('/')).ToString();
        var bootstrapper = string.IsNullOrWhiteSpace(options.Bootstrapper)
            ? "_framework/blazor.webassembly.js"
            : options.Bootstrapper;

        return $"<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n" +
               "    <meta charset=\"utf-8\" />\n" +
               "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\n" +
               (metadata.Description is null ? string.Empty : $"    <meta name=\"description\" content=\"{EncodeHtml(metadata.Description)}\" />\n") +
               "    <meta property=\"og:type\" content=\"website\" />\n" +
               $"    <meta property=\"og:title\" content=\"{title}\" />\n" +
               (metadata.Description is null ? string.Empty : $"    <meta property=\"og:description\" content=\"{EncodeHtml(metadata.Description)}\" />\n") +
               (canonicalUrl is null ? string.Empty : $"    <link rel=\"canonical\" href=\"{EncodeHtml(canonicalUrl)}\" />\n    <meta property=\"og:url\" content=\"{EncodeHtml(canonicalUrl)}\" />\n") +
               (metadata.Image is null ? string.Empty : $"    <meta property=\"og:image\" content=\"{EncodeHtml(ResolveUrl(metadata.Image, configuration?.SiteUrl))}\" />\n    <meta name=\"twitter:image\" content=\"{EncodeHtml(ResolveUrl(metadata.Image, configuration?.SiteUrl))}\" />\n") +
               (metadata.Locale is null ? string.Empty : $"    <meta property=\"og:locale\" content=\"{EncodeHtml(metadata.Locale.Replace('-', '_'))}\" />\n") +
               (metadata.Date is null ? string.Empty : $"    <meta property=\"article:published_time\" content=\"{metadata.Date.Value.ToUniversalTime():O}\" />\n") +
               "    <meta name=\"twitter:card\" content=\"summary_large_image\" />\n" +
               $"    <meta name=\"twitter:title\" content=\"{title}\" />\n" +
               (metadata.Description is null ? string.Empty : $"    <meta name=\"twitter:description\" content=\"{EncodeHtml(metadata.Description)}\" />\n") +
               $"    <title>{title}</title>\n" +
               "    <base href=\"/\" />\n" +
               "</head>\n<body>\n" +
               $"    <div id=\"app\">{staticContent}</div>\n" +
               $"    <script src=\"{System.Net.WebUtility.HtmlEncode(bootstrapper)}\"></script>\n" +
               "</body>\n</html>\n";
    }

    private static string CreateStaticWebAppsConfiguration(IReadOnlyCollection<StaticPageInfo> pages)
    {
        var routes = pages.Select(page => new StaticWebAppsRoute(page.Route, "/" + page.FilePath));
        var configuration = new StaticWebAppsConfiguration(routes);

        return JsonSerializer.Serialize(configuration, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static StaticPagesConfiguration? ReadConfiguration(string projectDirectory)
    {
        var path = Path.Combine(projectDirectory, "blazorade.config.json");
        if (!File.Exists(path))
        {
            return null;
        }

        var document = JsonSerializer.Deserialize<BlazoradeConfiguration>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var siteUrl = document?.StaticPages?.SiteUrl;
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            return new StaticPagesConfiguration(null);
        }

        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException($"The 'staticPages.siteUrl' value in '{path}' must be an absolute URL with a host.");
        }

        return new StaticPagesConfiguration(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/");
    }

    private static string ResolveUrl(string value, string? siteUrl)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _)
            ? value
            : siteUrl is null ? value : new Uri(new Uri(siteUrl), value.TrimStart('/')).ToString();
    }

    private static string EncodeHtml(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static string EncodeXml(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;

    private sealed record StaticPageInfo(string Route, string FilePath, string PageName, Type ComponentType)
    {
        public StaticPageMetadata? Metadata { get; set; }
    }

    private sealed record StaticPagesConfiguration(string? SiteUrl);

    private sealed record BlazoradeConfiguration(StaticPagesSection? StaticPages);

    private sealed record StaticPagesSection(string? SiteUrl);

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

        public string[] Exclude { get; init; } =
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
public sealed record StaticPageGeneratorOptions(
    string ApplicationAssemblyPath,
    string OutputDirectory,
    string ProjectDirectory,
    string? Bootstrapper = null);