using Blazorade.Core.Components;
using Blazorade.StaticPages.StaticGeneration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blazorade.StaticPages.Components;

/// <summary>
/// Marks a routable component as a static page and provides metadata for generated HTML.
/// </summary>
public partial class StaticPage : BlazoradeComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the page content should be rendered when the app is running in the browser.
    /// The content is rendered during static generation regardless of this value.
    /// </summary>
    [Parameter]
    public bool RenderInBrowser { get; set; } = true;

    /// <summary>
    /// Gets or sets the page title used for generated title metadata.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = default!;

    /// <summary>
    /// Gets or sets the page description used for generated description metadata.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the page date used for generated date metadata.
    /// Date and time values without an offset are interpreted as UTC. Values with an offset are normalized to UTC.
    /// </summary>
    [Parameter]
    public string? Date { get; set; }

    /// <summary>
    /// Gets or sets the page image URL used for generated social metadata.
    /// Relative URLs are resolved against the configured site URL during static page generation.
    /// </summary>
    [Parameter]
    public string? Image { get; set; }

    /// <summary>
    /// Gets or sets the page locale in BCP 47 format.
    /// Hyphens are replaced with underscores when generating the <c>og:locale</c> metadata property.
    /// </summary>
    [Parameter]
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the page should be included in the generated sitemap.
    /// </summary>
    [Parameter]
    public bool IncludeInSitemap { get; set; } = true;

    private string CanonicalUrl => Navigation.ToAbsoluteUri(Navigation.Uri).GetComponents(
        UriComponents.SchemeAndServer | UriComponents.Path,
        UriFormat.UriEscaped);

    private string? ImageUrl => Image is null
        ? null
        : Navigation.ToAbsoluteUri(Image).AbsoluteUri;

    private DateTimeOffset? ParsedDate => StaticPageDateParser.TryParse(Date, out var date) ? date : null;

    private string? PublishedTime => ParsedDate is { } date
        ? StaticPageDateParser.FormatPublishedTime(date)
        : null;

    private string? PublishedDate => ParsedDate is { } date
        ? StaticPageDateParser.FormatDate(date)
        : null;

    /// <summary>
    /// Captures the current page metadata for static generation.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!string.IsNullOrWhiteSpace(Date) && !StaticPageDateParser.TryParse(Date, out _))
        {
            throw new InvalidOperationException($"The StaticPage Date value '{Date}' could not be parsed as a DateTimeOffset.");
        }

        Services.GetService<IStaticPageMetadataSink>()?.Capture(new StaticPageMetadata(
            Title,
            Description,
            ParsedDate,
            Image,
            Locale,
            IncludeInSitemap));
    }
}