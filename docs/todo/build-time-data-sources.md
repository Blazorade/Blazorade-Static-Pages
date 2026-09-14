# Build-time data sources and data-driven routes

**Priority:** 2

Support explicit build-time page sources so applications can generate concrete pages from reusable components and data instead of creating one routable Razor component per page.

## Proposed scope

- Introduce a normalized build-time static page model.
- Support concrete route instances for parameterized route patterns such as `/articles/{slug}`.
- Define an explicit provider contract for JSON, YAML, Markdown, CMS exports, or other deterministic sources.
- Keep providers in build-time tooling; never resolve the consuming application's runtime DI container.
- Allow providers to supply route, metadata, and static content.
- Reuse the normalized page model for HTML, sitemap, feeds, and hosting configuration.
- Produce actionable diagnostics for duplicate routes, invalid data, missing metadata, and unsupported content.
- Document how applications opt into and configure a provider.

## Deliberately excluded

- Arbitrary application code execution.
- Authentication-dependent or user-specific content.
- Browser APIs or JavaScript interop during generation.
