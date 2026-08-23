# Blazorade Scribe product direction

Blazorade Scribe is an application-first static publishing layer for ordinary Blazor WebAssembly applications. It adds build-time static publishing without imposing a Markdown-first project structure or replacing standard Blazor development practices.

The application remains the source of truth. Developers use normal Blazor tooling, routing, layouts, navigation, and components. Scribe publishes explicitly declared static content and allows the running Blazor application to enhance that content with normal runtime interactivity.

## Product objective

Scribe generates crawler-visible static HTML from explicitly declared content in ordinary Blazor WebAssembly applications. The generated output is intended to support:

- Static HTML page generation.
- Page metadata.
- Canonical URLs.
- Sitemap generation.
- Static-hosting route configuration.
- Runtime Blazor interactivity layered on top of static output.

The static artifact is the baseline. Runtime enhancement must not be required for crawlers or users to receive the page's meaningful static content.

## Component contract

### `StaticPage`

`StaticPage` identifies the start of a statically publishable page and carries page metadata.

```razor
@page "/products"

<StaticPage
    Title="Products"
    Description="Explore our products."
    IncludeInSitemap="true">

    <h1>Products</h1>
    <p>Browse our product catalogue.</p>

    <ProductTable />

    <InteractiveContent>
        <ProductConfigurator />
    </InteractiveContent>
</StaticPage>
```

Rules:

- A routable Blazor component should normally contain one `StaticPage`.
- The route comes from the normal `@page` directive.
- `StaticPage` supplies metadata such as `Title`, `Description`, and `IncludeInSitemap`.
- Future metadata may include canonical URL, change frequency, priority, author, date, keywords, and schema type.
- Content directly inside `StaticPage` is static by default.
- `StaticPage` renders no wrapper element at runtime.
- `StaticPage` is primarily a publishing signal and metadata contract.

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

`StaticContent` is used inside reusable components to expose their static representation:

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

- `StaticContent` is not required directly inside `StaticPage`.
- Reusable components use it to expose a safe static representation.
- It is transparent at runtime and renders no wrapper element.
- A reusable component with no static representation contributes no static content unless another explicit rule defines its behavior.
- Static fragments should normally be complete, valid HTML fragments. Structurally incomplete fragments, such as standalone `<tr>` elements, are unsupported unless ancestor preservation is deliberately designed.

## Static extraction semantics

The intended extraction model is:

```text
StaticPage
 ├── ordinary page markup       → include as static
 ├── reusable component
 │    └── StaticContent          → include exposed fragment
 └── InteractiveContent          → exclude entire subtree
```

Scribe may inspect or execute components during analysis, but only content permitted by this contract becomes generated HTML. It must not blindly publish every output produced by every component.

Components containing browser-only or runtime-only behavior must expose only a safe `StaticContent` representation, with interactive portions under `InteractiveContent`.

## Build and publishing workflow

The conceptual workflow is:

```text
dotnet build
    ↓
Compiled Blazor assemblies
    ↓
Scribe static-publishing target or command
    ↓
Static page discovery and component-tree analysis
    ↓
Generated HTML, sitemap, and route configuration
```

The publisher should use a dedicated static-publishing host or renderer after compilation. It must operate on compiled components and a controlled render tree rather than parsing `.razor` source files as plain text.

The initial renderer should:

1. Discover routable components containing `StaticPage`.
2. Read or capture their metadata.
3. Render or analyse the component tree in a static extraction context.
4. Treat direct page content as static.
5. Traverse reusable components to find `StaticContent`.
6. Skip `InteractiveContent` subtrees completely.
7. Compose the collected fragments into an HTML page shell.
8. Generate sitemap entries only for pages with `IncludeInSitemap="true"`.
9. Generate route configuration and other static output.

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

If static rendering fails, the publisher should report the page route, the involved component, the reason for failure, and guidance to move runtime-only behavior into `InteractiveContent`.

Data access during static publishing is intentionally not defined yet. Future options include build-time service registration, static data providers, explicit build configuration, or a strict no-runtime-data rule.

## Initial implementation slice

The first implementation should establish the smallest end-to-end vertical slice:

1. Create the basic project structure.
2. Define transparent `StaticPage`, `StaticContent`, and `InteractiveContent` components.
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

The old Markdown publisher must not be copied blindly. Scribe's primary source model is compiled Blazor components and page metadata, not Markdown files. A possible long-term architecture is:

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