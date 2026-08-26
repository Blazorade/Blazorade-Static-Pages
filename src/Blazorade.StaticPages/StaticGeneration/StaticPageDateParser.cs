using System.Globalization;

namespace Blazorade.StaticPages.StaticGeneration;

/// <summary>
/// Parses date values supplied to <see cref="Components.StaticPage.Date"/>.
/// </summary>
internal static class StaticPageDateParser
{
    private const DateTimeStyles Styles = DateTimeStyles.AllowWhiteSpaces
        | DateTimeStyles.AssumeUniversal
        | DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Attempts to parse a static page date as a UTC-normalized date and time.
    /// </summary>
    /// <param name="value">The date text to parse.</param>
    /// <param name="date">The parsed date, or <see langword="null"/> when parsing fails.</param>
    /// <returns><see langword="true"/> when the value was parsed successfully; otherwise, <see langword="false"/>.</returns>
    internal static bool TryParse(string? value, out DateTimeOffset? date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = null;
            return string.IsNullOrWhiteSpace(value);
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, Styles, out var parsed))
        {
            date = parsed;
            return true;
        }

        date = null;
        return false;
    }

    /// <summary>
    /// Formats a date for the <c>article:published_time</c> metadata property.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>A concise UTC date/time value in ISO 8601 format.</returns>
    internal static string FormatPublishedTime(DateTimeOffset date) =>
        date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a date for the standard date metadata value.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>A date-only value in ISO 8601 format.</returns>
    internal static string FormatDate(DateTimeOffset date) =>
        date.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}