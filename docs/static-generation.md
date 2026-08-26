# Static generation

Blazorade Static Pages generates crawler-visible HTML during the consuming Blazor WebAssembly application's build. The generator uses the application's Razor source files and `wwwroot/index.html` as inputs. It does not run the application, invoke lifecycle methods, render components, resolve services, or fetch runtime data.

## Build-time workflow

When the consuming application is built:

1. The MSBuild target runs after `Build` has completed.
2. The generator scans the project for `.razor` files, excluding `bin` and `obj`.
3. It finds routable components by reading `@page` directives.
4. A routable component is selected only when it has a `StaticPageAttribute`. Routable components without the attribute are ignored.
5. The generator analyzes the static markup and metadata, then writes generated files to `obj/.../Blazorade.StaticPages/generated`.
6. The generated files are copied to the build output's `wwwroot` directory. During publish they are copied to the publish output's `wwwroot` directory.

The generator is source-based and deterministic. It does not depend on browser APIs, JavaScript interop, authentication state, user state, service resolution, lifecycle code, property getters, or external runtime data.

## Page discovery and output paths

The normal Blazor `@page` directive supplies the route. Parameterized routes are not supported. Duplicate static routes and unsupported syntax fail the build with the source file and route in the diagnostic.

Generated page paths are mapped as follows:

| Route | Generated file |
| --- | --- |
| `/` | `<PageComponentName>.html` |
| `/products` | `products.html` |
| `/products/warranty` | `products/warranty.html` |

The root page uses the component name to avoid writing an ambiguous `index.html`.

## Static content extraction

The source analyzer builds a markup tree and applies these rules:

- Only content inside `StaticContent` is included in the generated body.
- `StaticContent` is transparent in the generated fragment; its tag is not emitted.
- An `InteractiveContent` element and its entire descendant subtree are omitted.
- A reusable component contributes only its direct `StaticContent` regions. Its ordinary markup is not included automatically.
- A reusable component with no `StaticContent` contributes nothing.
- `InteractiveContent` inside a reusable component is omitted even when it is inside a static region.
- Ordinary HTML elements, attributes, comments, and text are copied into the generated fragment.
- Razor expressions in static text are limited to known compile-time string constants. Values are HTML-encoded when inserted.
- `StaticMetadata` supplies the generated title and supported metadata.

For example, a reusable component can provide a crawlable representation while keeping its runtime behavior separate:

```razor
<StaticContent>
    <section class="profile-summary">
        <h2>@Name</h2>
        <p>Additional details are available interactively.</p>
    </section>
</StaticContent>

<InteractiveContent>
    <ProfileEditor />
</InteractiveContent>
```

The referenced component's `StaticContent` is analyzed with the parameters supplied at its call site. A parameter value must be a literal or a supported compile-time constant. Arbitrary component execution is never used to obtain static output.

## Compile-time values

Metadata and static text must be resolvable without executing code. The current analyzer supports string constants declared in the `.razor` file or its matching `.razor.cs` file, including references passed as component attributes:

```razor
@page "/"

@code {
    private const string PageTitle = "Welcome";
}

@attribute [StaticPage]

<StaticMetadata Title="@PageTitle" />

<StaticContent>
    <h1>@PageTitle</h1>
</StaticContent>
```

Service calls, method calls, property getters, fields that are not recognized string constants, lifecycle state, and runtime expressions are not evaluated. Unsupported expressions produce a build error rather than silently producing incomplete content.

## Generated HTML document

Each generated page starts with the consuming application's `wwwroot/index.html` as its template. The generator:

1. Replaces the template's `<title>` element content with `StaticMetadata.Title`.
2. Replaces the contents of `<div id="app">` with the extracted static fragment.
3. Replaces the fingerprinted Blazor WebAssembly bootstrapper placeholder with `_framework/blazor.webassembly.js`, or with the configured generator bootstrapper argument.
4. Inserts generated metadata immediately before `</head>`.

The rest of the template is retained, including its language, viewport, base URL, stylesheets, import map, loading UI, and other existing markup. The generated document therefore contains both the static page and the normal Blazor application shell for runtime enhancement.

### Generated metadata

The following table describes every metadata element currently created by the generator. Values are HTML-encoded before insertion.

| Element | Created when | Value source and transformation |
| --- | --- | --- |
| `<title>` | Always | `StaticMetadata.Title` |
| `<meta property="og:type" content="website">` | Always | Fixed value `website` |
| `<meta property="og:title">` | Always | `StaticMetadata.Title` |
| `<meta name="twitter:card" content="summary_large_image">` | Always | Fixed value `summary_large_image` |
| `<meta name="twitter:title">` | Always | `StaticMetadata.Title` |
| `<meta name="description">` | When `Description` is supplied | `StaticMetadata.Description` |
| `<meta property="og:description">` | When `Description` is supplied | `StaticMetadata.Description` |
| `<meta name="twitter:description">` | When `Description` is supplied | `StaticMetadata.Description` |
| `<meta name="author">` | When `Author` is supplied | `StaticMetadata.Author` |
| `<link rel="canonical">` | When `staticPages.siteUrl` is configured | Configured site URL combined with the page route |
| `<meta property="og:url">` | When `staticPages.siteUrl` is configured | The same canonical URL |
| `<meta property="og:image">` | When `Image` is supplied | `StaticMetadata.Image`; relative values are resolved against `siteUrl` when available |
| `<meta name="twitter:image">` | When `Image` is supplied | The same resolved image URL |
| `<meta property="og:locale">` | When `Locale` is supplied | `StaticMetadata.Locale` with `-` replaced by `_` |
| `<meta property="article:published_time">` | When `Date` is supplied and valid | The UTC-normalized date/time in concise ISO 8601 format, such as `2026-08-24T00:00:00Z` |
| `<meta name="date">` | When `Date` is supplied and valid | The UTC-normalized date in ISO 8601 format, such as `2026-08-24` |

`StaticMetadata.Date` accepts a date or date/time string that can be parsed as a `DateTimeOffset`. Values without an explicit time-zone offset are interpreted as UTC, and values with an offset are normalized to UTC. Invalid values produce a build warning and are omitted from generated date metadata. The `date` name is a commonly supported convention for publication dates; `article:published_time` remains the more specific article metadata property. Author, keywords, schema, Open Graph site name, and other metadata are not generated unless they already exist in the application HTML template.

## Sitemap

When `staticPages.siteUrl` is configured, the generator creates `sitemap.xml` in the generated output. It emits one `<url><loc>...</loc></url>` entry for every generated page except pages whose `IncludeInSitemap` value is explicitly `false`.

The location is the configured site URL combined with the page route. It is XML-escaped. If the configuration file or site URL is unavailable, no sitemap is created.

## Static Web Apps routing

The generator creates `staticwebapp.config.json` with one explicit rewrite for each generated route, mapping the route to its generated `.html` file. It also adds a navigation fallback to `/index.html`. The fallback exclusions are:

```text
/*.html
/css/*
/js/*
/lib/*
/sitemap.xml
/*.{png,ico,svg,gif,woff,woff2,ttf,json}
/*.pdf
/*.svg
/*.{css,scss,js,png,gif,ico,jpg,svg,wasm,dll,dat,blat,pdb,woff,woff2,ttf,eot}
/assets/*
/_content/*
/_framework/*
```

During publish, the generated files are copied again after the publish output is finalized. If the published Blazor bootstrapper has a fingerprinted filename, the build integration also copies it to `_framework/blazor.webassembly.js` (and the fingerprinted .NET bootstrapper to `_framework/dotnet.js`) so the generated documents can use stable script paths.

## Component usage

### `StaticPageAttribute` and `StaticMetadata`

Mark a routable component with `StaticPageAttribute` and place one `StaticMetadata` component in it. The route remains on the page component through `@page`:

```razor
@page "/products"
@attribute [StaticPage]

<StaticMetadata
    Title="Products"
    Description="Explore our products."
    Image="images/products.jpg"
    Locale="en-US" />

<StaticContent>
    <h1>Products</h1>
    <p>Browse our product catalogue.</p>
</StaticContent>
```

`StaticMetadata.Title` is required. `Description`, `Author`, `Image`, `Locale`, and `Date` are optional, but every supplied value must resolve to a compile-time value. `RenderInBrowser` defaults to `true`, controls only live metadata rendering, and does not control build-time extraction. `IncludeInSitemap` belongs to `StaticPageAttribute`.

### `StaticContent`

Use `StaticContent` in a reusable component to define the representation that may be copied into a generated page:

```razor
<!-- ProductTable.razor -->
<StaticContent>
    <table>
        <tr><th>Name</th><th>Price</th></tr>
        <tr><td>Product A</td><td>$25</td></tr>
    </table>
</StaticContent>

<InteractiveContent>
    <ProductTableEditor />
</InteractiveContent>
```

Use it directly in a page to mark body markup for generation. It emits no wrapper element. Static fragments should be complete, valid HTML; do not expose structurally incomplete fragments such as an isolated `<tr>` unless the surrounding structure is deliberately preserved.

### `InteractiveContent`

Wrap content that requires runtime behavior or data:

```razor
<InteractiveContent>
    <ProductConfigurator />
</InteractiveContent>
```

The complete descendant subtree is excluded from generated HTML, but it renders normally in the browser. The component emits no wrapper element and nested `InteractiveContent` has no additional effect.

## Errors and limitations

The build fails for invalid Razor syntax, parameterized routes, duplicate static routes, multiple `StaticPageAttribute` or `StaticMetadata` declarations in one routable component, unresolved reusable components, cyclic reusable components, missing required `Title`, and unsupported dynamic expressions. Keep runtime-only behavior inside `InteractiveContent` and provide a deterministic `StaticContent` representation for reusable components that need crawlable output.