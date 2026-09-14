# Extensible page metadata

**Priority:** 3

Expand the fixed `StaticMetadata` contract without requiring library changes for every new SEO, social, or structured-data field.

## Proposed scope

- Add per-page robots directives.
- Support `og:site_name`, `twitter:creator`, article sections, and article tags.
- Support `hreflang` alternate links.
- Support structured custom `<meta>` and `<link>` values.
- Support extension or replacement JSON-LD payloads through a structured model.
- Keep live browser rendering and generated HTML based on the same metadata model.
- Preserve safe encoding, validation, and URL resolution for all extension values.
- Document which extensions are emitted in both live and generated output.

Prefer structured metadata components or objects over arbitrary raw HTML strings.
