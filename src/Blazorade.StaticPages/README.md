# Blazorade Static Pages

Blazorade Static Pages is a library for generating crawler-visible static HTML from ordinary Blazor WebAssembly applications at build time. It works with standard Blazor routing, layouts, navigation, and components, allowing the generated static output to be enhanced with normal Blazor interactivity at runtime.

The application remains the source of truth. Static content is declared explicitly with transparent marker components:

- `StaticPage` identifies a static page and provides metadata such as its title, description, and sitemap inclusion.
- `StaticContent` exposes a safe static representation from a reusable component.
- `InteractiveContent` excludes runtime-only content from generated HTML while leaving it available to the running application.

The package also integrates the static-page generator into the consuming application's build and publish process. Generated output can include static HTML pages, canonical metadata, sitemap entries, and static-hosting route configuration.

## Getting started

Add the `Blazorade.StaticPages` package to a Blazor WebAssembly application. Declare static content in a routable component:

```razor
@page "/products"

<StaticPage Title="Products" Description="Explore our products." IncludeInSitemap="true">
    <h1>Products</h1>
    <p>Browse our product catalogue.</p>

    <InteractiveContent>
        <ProductConfigurator />
    </InteractiveContent>
</StaticPage>
```

For reusable components, place the static representation inside `StaticContent`. Runtime-only descendants should be placed inside `InteractiveContent`.

To configure the public site URL used for canonical URLs and sitemap generation, add `blazorade.config.json` next to the consuming application's project file:

```json
{
  "staticPages": {
    "siteUrl": "https://www.example.com"
  }
}
```

## Version highlights

### v1.0.0-preview.1

- First published preview of Blazorade Static Pages.
- Added transparent `StaticPage`, `StaticContent`, and `InteractiveContent` components.
- Added build-time static page discovery and HTML generation for Blazor WebAssembly applications.
- Added page metadata, canonical URL support, sitemap generation, and static-hosting route configuration.
- Added build and publish integration for generated static output.
