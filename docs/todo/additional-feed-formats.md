# Additional feed formats

**Priority:** 6

Add feed formats beyond RSS using the same static page model and selection rules.

## Proposed scope

- Add Atom feed generation.
- Add JSON Feed generation.
- Reuse page selection, publication dates, canonical URLs, and static content rules from RSS.
- Support the existing include/exclude controls and item limits.
- Add feed discovery links for each enabled feed format.
- Generate provider-neutral feed route metadata for hosting adapters.
- Validate required absolute URLs and feed-specific fields.
- Document configuration for enabling individual formats.

Multiple feeds, categories, enclosures, and media namespaces should remain separate design decisions rather than being assumed by the first implementation.
