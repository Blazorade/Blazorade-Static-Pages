---
description: "Keep StaticPage metadata consistent between live Blazor rendering and generated static HTML."
applyTo: "src/Blazorade.StaticPages/Components/StaticPage.razor,src/Blazorade.StaticPages/Components/StaticPage.razor.cs,src/Blazorade.StaticPages/StaticGeneration/**/*.cs,src/Blazorade.StaticPages.Generator/**/*.cs,src/Blazorade.StaticPages/README.md,docs/configuration.md,docs/static-generation.md,docs/objective-and-design-principles.md"
---
# StaticMetadata contract

`StaticMetadata` has two metadata output paths that must remain semantically equivalent:

- Live browser rendering in `src/Blazorade.StaticPages/Components/StaticMetadata.razor` and its code-behind.
- Generated HTML rendering in `src/Blazorade.StaticPages.Generator/StaticPageGenerator.cs`.

`StaticSourcePageAnalyzer` extracts and validates metadata values from consuming application source. Changes to the supported metadata contract may therefore require updates in all of these areas.

## Supported metadata

The supported `StaticMetadata` parameters and their output are:

- `Title`: document title input, `og:title`, and `twitter:title`.
- `Description`: `description`, `og:description`, and `twitter:description` when non-null.
- `Author`: `author` when non-null.
- `Image`: `og:image` and `twitter:image` when non-null.
- `Locale`: `og:locale` when non-null; replace hyphens with underscores.
- `Date`: `article:published_time` when non-null; convert to UTC and use round-trip (`O`) formatting.
- `RenderInBrowser`: live metadata rendering only; it does not affect generated output.
- Every page produces `og:type=website` and `twitter:card=summary_large_image`.
- A canonical link and `og:url` are produced when a canonical URL is available.

The document `<title>` is managed separately from the meta elements, but its value must remain consistent with `StaticMetadata.Title` in generated output. `IncludeInSitemap` belongs to `StaticPageAttribute` and does not produce a head element.

## URL rules

- Generated HTML uses the configured `staticPages.siteUrl` for canonical URLs and absolute image URLs.
- Live rendering uses the current browser page URL for canonical URLs and the browser base URI for relative image URLs.
- These URL sources are intentionally different; the resulting metadata must represent the same page and image.

## Change checklist

When adding, removing, renaming, or changing a supported metadata parameter or output element:

1. Update `StaticMetadata` and its XML documentation.
2. Update `StaticMetadata.razor` and `StaticMetadata.razor.cs`.
3. Update `StaticSourcePageAnalyzer` and its metadata value model when source analysis is affected.
4. Update `StaticPageGenerator` and generated-output formatting.
5. Update relevant README or design documentation.
6. Add or update tests that compare live and generated metadata behavior.
7. Build both the runtime library and generator host.

Do not update only one rendering path. Keep conditional emission, element names, attribute names, and value transformations synchronized between live and generated output.
