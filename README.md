# Blazorade Static Pages
Blazorade Static Pages — Static page generation for Blazor WebAssembly

Blazorade Static Pages is an application-first static page generation library for ordinary Blazor WebAssembly applications. Developers use standard Blazor tooling, routing, layouts, navigation, and components; Static Pages adds build-time generation of crawler-visible HTML.

The application remains the source of truth. Static Pages generates pages from explicitly declared static content and layers normal Blazor interactivity on top at runtime.

## NuGet

Blazorade Static Pages is available on NuGet: [Blazorade.StaticPages](https://www.nuget.org/packages/Blazorade.StaticPages/).

## Core contract

Static Pages defines three transparent marker components: `StaticPage` identifies a static page and its metadata, `StaticContent` exposes a reusable component's static representation, and `InteractiveContent` excludes a runtime-only subtree from static output.

All three components render no wrapper elements at runtime. A routable component normally contains one `StaticPage`; content directly inside it is static by default. Page-level `StaticContent` may be placed beside `StaticPage` when the page marker should not wrap the content.

See [the objective and design principles](docs/objective-and-design-principles.md) for the detailed contract, rendering model, constraints, and planned vertical slice.

## Status

The library is in early-stage development.
