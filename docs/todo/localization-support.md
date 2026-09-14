# Localization support

**Priority:** 5

Support publishing localized variants of static pages and their associated metadata.

## Proposed scope

- Define localized page identities and route mappings.
- Generate `hreflang` alternate links.
- Generate localized canonical URLs.
- Include localized URLs in the sitemap.
- Support locale route prefixes such as `/en/...` and `/fi/...`.
- Consider locale-specific RSS, Atom, or JSON Feed output.
- Keep `StaticMetadata.Locale` consistent with the generated page locale.
- Report missing translations, duplicate localized routes, and invalid locale identifiers.

This should build on the normalized page model introduced by data-driven routes.
