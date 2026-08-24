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
    private IServiceProvider Services { get; set; } = default!;

    private bool IsStaticGeneration =>
        Services.GetService<StaticPageRenderContext>()?.IsStaticGeneration == true;

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
    /// </summary>
    [Parameter]
    public DateTime? Date { get; set; }

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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Services.GetService<IStaticPageMetadataSink>()?.Capture(new StaticPageMetadata(
            Title,
            Description,
            Date,
            Image,
            Locale,
            IncludeInSitemap));
    }
}