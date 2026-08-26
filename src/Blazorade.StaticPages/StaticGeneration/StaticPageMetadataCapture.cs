namespace Blazorade.StaticPages.StaticGeneration;

/// <summary>
/// Receives metadata from a static page while it is rendered by the build-time generator.
/// </summary>
public interface IStaticPageMetadataSink
{
    /// <summary>
    /// Records the metadata supplied to a static page.
    /// </summary>
    /// <param name="metadata">The static page metadata.</param>
    void Capture(StaticPageMetadata metadata);
}

/// <summary>
/// Contains metadata supplied to a static page component.
/// </summary>
/// <param name="Title">The page title used for the document title, Open Graph title, and Twitter title.</param>
/// <param name="Description">The optional page description used for description, Open Graph description, and Twitter description metadata.</param>
/// <param name="Date">The optional page date used for article published-time metadata.</param>
/// <param name="Image">The optional page image URL used for Open Graph and Twitter image metadata.</param>
/// <param name="Locale">The optional page locale used for Open Graph locale metadata.</param>
/// <param name="IncludeInSitemap">A value indicating whether the page should be included in the generated sitemap.</param>
/// <remarks>
/// The metadata is consumed by both the live <c>StaticPage</c> component and the static HTML generator.
/// The rendering paths must remain synchronized when this contract or its value transformations change.
/// <para>
/// <see cref="IncludeInSitemap"/> controls sitemap generation only and does not produce a head element.
/// Canonical URL metadata is derived from the current browser URL during live rendering and from the configured
/// site URL during static generation, so it is intentionally not stored in this record.
/// </para>
/// </remarks>
public sealed record StaticPageMetadata(
    string Title,
    string? Description,
    DateTime? Date,
    string? Image,
    string? Locale,
    bool IncludeInSitemap);

/// <summary>
/// Stores metadata captured during one static page render.
/// </summary>
public sealed class StaticPageMetadataCapture : IStaticPageMetadataSink
{
    /// <summary>
    /// Gets the most recently captured metadata, or <see langword="null"/> when no page was captured.
    /// </summary>
    public StaticPageMetadata? Current { get; private set; }

    /// <inheritdoc />
    public void Capture(StaticPageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Current = metadata;
    }

    /// <summary>
    /// Clears metadata captured by a previous render.
    /// </summary>
    public void Reset() => Current = null;
}
