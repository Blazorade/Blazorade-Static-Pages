using Blazorade.StaticPages.StaticGeneration;
using Microsoft.AspNetCore.Components;

namespace Blazorade.StaticPages.Components;

/// <summary>
/// Defines metadata for a statically generated page and optionally renders the same metadata in the browser.
/// </summary>
public partial class StaticMetadata
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = default!;

    /// <summary>
    /// Gets or sets the optional page description.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the optional page author.
    /// </summary>
    [Parameter]
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the optional page date.
    /// Date and time values without an offset are interpreted as UTC. Values with an offset are normalized to UTC.
    /// </summary>
    [Parameter]
    public string? Date { get; set; }

    /// <summary>
    /// Gets or sets the optional page image URL.
    /// </summary>
    [Parameter]
    public string? Image { get; set; }

    /// <summary>
    /// Gets or sets the optional page locale in BCP 47 format.
    /// </summary>
    [Parameter]
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the metadata should be rendered in the browser.
    /// Static generation includes the metadata regardless of this value.
    /// </summary>
    [Parameter]
    public bool RenderInBrowser { get; set; } = true;

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
}