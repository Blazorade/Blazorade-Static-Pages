# Optional static-output validation

**Priority:** 7

Investigate validation as an optional companion capability rather than making it part of the core static generation path. Validation may be more appropriate as a separate tool or build step.

## Possible scope

- Validate generated HTML structure.
- Detect broken internal links.
- Detect missing `alt` attributes.
- Detect duplicate canonical URLs.
- Detect missing titles or descriptions where configured as required.
- Detect static assets referenced by pages but absent from `wwwroot`.
- Verify sitemap entries map to generated pages.
- Verify feed links map to generated pages.
- Support warnings without blocking builds.
- Support an explicitly enabled strict mode for CI.

## Design questions

- Should validation live in this package, a separate package, or an external CI tool?
- Which checks are content-generation concerns versus general site-quality checks?
- Should validation operate on the normalized page model, generated output, or both?
