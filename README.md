# Blazorade Static Pages
Blazorade Static Pages — Static page generation for Blazor WebAssembly

Blazorade Static Pages is an application-first static page generation library for ordinary Blazor WebAssembly applications. Developers use standard Blazor tooling, routing, layouts, navigation, and components; Static Pages adds build-time generation of crawler-visible HTML.

The application remains the source of truth. Static Pages generates pages from explicitly declared static content and layers normal Blazor interactivity on top at runtime.

## NuGet

Blazorade Static Pages is available on NuGet: [Blazorade.StaticPages](https://www.nuget.org/packages/Blazorade.StaticPages/).

## Core contract

Static Pages uses `StaticPageAttribute` to identify static routable components, `StaticMetadata` to define generated and optional live metadata, `StaticContent` to expose generated body content, and `InteractiveContent` to exclude a runtime-only subtree from static output.

`StaticContent` and `InteractiveContent` render no wrapper elements at runtime. A routable component contains `@attribute [StaticPage]`, one `StaticMetadata`, and one or more `StaticContent` regions. Only content inside `StaticContent` is included in generated page bodies.

See [the objective and design principles](docs/objective-and-design-principles.md) for the detailed contract, rendering model, constraints, and planned vertical slice.

## Status

The library is in early-stage development.
