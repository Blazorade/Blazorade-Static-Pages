# Blazorade Static Pages objective and design principles

Blazorade Static Pages is an application-first static page generation library for ordinary Blazor WebAssembly applications. It adds build-time generation of crawler-visible pages without imposing a Markdown-first project structure or replacing standard Blazor development practices.

The application remains the source of truth. Developers use normal Blazor tooling, routing, layouts, navigation, and components. Static Pages generates pages from explicitly declared static content and allows the running Blazor application to enhance that content with normal runtime interactivity.

## Product objective

Static Pages generates crawler-visible static HTML from explicitly declared content in ordinary Blazor WebAssembly applications. The generated output is intended to support:

- Static HTML page generation.
- Page metadata.
- Canonical URLs.
- Sitemap generation.
- Static-hosting route configuration.
- Runtime Blazor interactivity layered on top of static output.

The static artifact is the baseline. Runtime enhancement must not be required for crawlers or users to receive the page's meaningful static content.

## Component contract

### `StaticPageAttribute` and `StaticMetadata`

`StaticPageAttribute` identifies a routable component for static generation. `StaticMetadata` defines the metadata used for generated HTML and optional live browser rendering.

```razor
@page "/products"
@attribute [StaticPage]

<StaticMetadata
    Title="Products"
    Description="Explore our products." />

<StaticContent>
    <h1>Products</h1>
    <p>Browse our product catalogue.</p>

    <ProductTable />

    <InteractiveContent>
        <ProductConfigurator />
    </InteractiveContent>
</StaticContent>
```

Rules:

- A routable Blazor component should contain one `StaticPageAttribute` and one `StaticMetadata` component.
- The route comes from the normal `@page` directive.
- `StaticPageAttribute` controls page selection and sitemap inclusion.
- `StaticMetadata` requires `Title`; its other metadata values are optional but must be compile-time-resolvable when supplied.
- Only content inside `StaticContent` is included in the generated body.
- `StaticMetadata.RenderInBrowser` controls only live metadata rendering.

### `InteractiveContent`

`InteractiveContent` marks a subtree as runtime-only:

```razor
<InteractiveContent>
    <ProductConfigurator />
    <ShoppingCart />
</InteractiveContent>
```

Rules:

- The complete descendant subtree is excluded from static output.
- The publisher must not traverse into the subtree.
- The child content renders normally when the Blazor application runs.
- `InteractiveContent` renders no wrapper element at runtime.
- Nested `InteractiveContent` has no additional effect.

### `StaticContent`

`StaticContent` is used to expose a safe static representation. It may be used inside reusable components:

```razor
<StaticContent>
    <table class="product-table">
        <thead>
            <tr>
                <th>Name</th>
                <th>Price</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>Product A</td>
                <td>$25</td>
            </tr>
        </tbody>
    </table>
</StaticContent>

<InteractiveContent>
    <ProductFilter />
</InteractiveContent>
```

Rules:

- Page-level `StaticContent` is included in the page's static output.
- Reusable components use it to expose a safe static representation.
- For reusable components, only their `StaticContent` subtree is included when the component is used by a page.
- It is transparent at runtime and renders no wrapper element.
- A reusable component with no static representation contributes no static content unless another explicit rule defines its behavior.
- Static fragments should normally be complete, valid HTML fragments. Structurally incomplete fragments, such as standalone `<tr>` elements, are unsupported unless ancestor preservation is deliberately designed.

## Static extraction semantics

The intended extraction model is:

```text
StaticPageAttribute
 ├── StaticMetadata             → generate head metadata
 ├── StaticContent              → include body content
 ├── reusable component
 │    └── StaticContent          → include exposed fragment
 └── InteractiveContent          → exclude entire subtree
```

Static Pages analyzes component source during the build, but only content permitted by this contract becomes generated HTML. It does not execute components or blindly include every output produced by every component.

Components containing browser-only or runtime-only behavior must expose only a safe `StaticContent` representation, with interactive portions under `InteractiveContent`.

## Build and generation workflow

The current workflow is:

```text
dotnet build
    ↓
Completed application build
    ↓
Static Pages MSBuild target
    ↓
Static page discovery and component-tree analysis
    ↓
Generated HTML, sitemap, and route configuration
```

The generator uses a dedicated build-time host, but the current implementation analyzes the consuming project's `.razor` source files with Razor syntax validation and a markup tree. It does not execute compiled components or perform runtime rendering.

The current analyzer:

1. Discover routable source components marked with `StaticPageAttribute` and containing exactly one `StaticMetadata`.
2. Read or capture their metadata.
3. Analyze the component tree using only supported compile-time values.
4. Treat direct page content and page-level `StaticContent` as static.
5. Traverse reusable components to find their `StaticContent`.
6. Skip `InteractiveContent` subtrees completely.
7. Compose the collected fragments into an HTML page shell.
8. Generate sitemap entries for pages unless `IncludeInSitemap="false"`.
9. Generate route configuration and other static output. See [Static generation](static-generation.md) for the complete output and metadata contract.

## Static rendering constraints

Static processing must not require arbitrary application components to execute successfully at build time. Static content must avoid or isolate:

- JavaScript interop.
- `window` and `document`.
- Browser storage.
- Authentication-dependent state.
- User-specific state.
- Browser-only APIs.
- Runtime-only services.
- Unavailable external data.
- Nondeterministic output.

If static rendering fails, the generator should report the page route, the involved component, the reason for failure, and guidance to move runtime-only behavior into `InteractiveContent`.

Data access during static generation is intentionally not defined yet. Future options include build-time service registration, static data providers, explicit build configuration, or a strict no-runtime-data rule.

## Initial implementation slice

The first implementation should establish the smallest end-to-end vertical slice:

1. Create the basic project structure.
2. Define `StaticMetadata`, `StaticContent`, and `InteractiveContent` components plus `StaticPageAttribute`.
3. Define page metadata types and sitemap inclusion metadata.
4. Create a minimal test Blazor application containing one routable static page, direct static markup, a reusable component exposing `StaticContent`, and an `InteractiveContent` region.
5. Implement page discovery.
6. Implement static extraction for the minimal supported contract.
7. Generate one static HTML page.
8. Generate a sitemap entry when requested.
9. Add tests for direct content, nested `StaticContent`, omitted `InteractiveContent`, page metadata, sitemap inclusion and exclusion, runtime transparency, and unsupported static rendering.
10. Document the current limitations.

The first slice does not attempt arbitrary Blazor prerendering, browser API support, authentication-aware rendering, runtime data access, or full application deployment.

## Relationship to Blazorade Scraibe

The existing Blazorade Scraibe repository is useful reference material for reusable publishing infrastructure, including static HTML generation, sitemap generation, canonical URLs, page templates, route rewrites, HTML validation, and component enhancement concepts.

The old Markdown publisher must not be copied blindly. Static Pages' primary source model is compiled Blazor components and page metadata, not Markdown files. A possible long-term architecture is:

```text
Source adapter
 ├── Razor/Blazor application adapter
 └── Optional Markdown adapter
          ↓
   Normalized static page model
          ↓
   Shared HTML publisher
          ↓
   HTML, sitemap, routes
```

Markdown support may remain a future compatibility adapter, but it is not the primary source model.