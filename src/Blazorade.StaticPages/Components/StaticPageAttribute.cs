namespace Blazorade.StaticPages.Components;

/// <summary>
/// Marks a routable component for static page generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StaticPageAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the page should be included in the generated sitemap.
    /// </summary>
    public bool IncludeInSitemap { get; set; } = true;
}