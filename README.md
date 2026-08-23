# Blazorade Scribe
Blazorade Scribe — Static publishing for Blazor WebAssembly

Blazorade Scribe is an application-first static publishing layer for ordinary Blazor WebAssembly applications. Developers use standard Blazor tooling, routing, layouts, navigation, and components; Scribe adds build-time generation of crawler-visible static HTML.

The application remains the source of truth. Scribe publishes explicitly declared static content and layers normal Blazor interactivity on top at runtime.

## Core contract

Scribe defines three transparent marker components: `StaticPage` identifies a publishable page and its metadata, `StaticContent` exposes a reusable component's static representation, and `InteractiveContent` excludes a runtime-only subtree from static output.

All three components render no wrapper elements at runtime. A routable component normally contains one `StaticPage`; content directly inside it is static by default.

See [the product direction and initial implementation scope](docs/product-direction.md) for the detailed contract, rendering model, constraints, and planned vertical slice.

## Status

Early-stage development. The new application-first publisher is intentionally separate from the older Blazorade Scraibe codebase.
