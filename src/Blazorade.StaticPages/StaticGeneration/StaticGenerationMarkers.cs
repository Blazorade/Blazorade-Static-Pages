namespace Blazorade.StaticPages.StaticGeneration;

/// <summary>
/// Defines the render markers used to identify content for static-page output.
/// </summary>
public static class StaticGenerationMarkers
{
    /// <summary>
    /// Gets the marker emitted before a static page's content.
    /// </summary>
    public const string StaticPageStart = "<!--blazorade-static-page-start-->";

    /// <summary>
    /// Gets the marker emitted after a static page's content.
    /// </summary>
    public const string StaticPageEnd = "<!--blazorade-static-page-end-->";

    /// <summary>
    /// Gets the marker emitted before explicitly marked static content.
    /// </summary>
    public const string StaticContentStart = "<!--blazorade-static-content-start-->";

    /// <summary>
    /// Gets the marker emitted after explicitly marked static content.
    /// </summary>
    public const string StaticContentEnd = "<!--blazorade-static-content-end-->";
}
