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
