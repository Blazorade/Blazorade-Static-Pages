# Configuration

Blazorade Static Pages is configured at build time. Configuration is not loaded by the runtime Blazor WebAssembly application.

## Configuration file

Create a file named `blazorade.config.json` in the root of the consuming Blazor WebAssembly application, alongside its project file:

```text
MyBlazorApp/
├── blazorade.config.json
├── MyBlazorApp.csproj
└── ...
```

The Static Pages build process reads this file from the consuming application's project directory. Each Blazorade library owns a dedicated section in the shared configuration file, so libraries can be configured independently without top-level property conflicts.

An optional configuration-specific file can be added using the active MSBuild build configuration:

```text
blazorade.config.json
blazorade.config.Debug.json
blazorade.config.Release.json
```

The build first reads `blazorade.config.json`, then reads `blazorade.config.$(Configuration).json` when that file exists. Values from the configuration-specific file take precedence. This also supports custom build configurations such as `Pre-Prod`:

```text
blazorade.config.Pre-Prod.json
```

No additional environment setting is required. The active configuration is selected through the normal Visual Studio or MSBuild configuration, for example `dotnet publish -c Pre-Prod`.

JSON objects are merged recursively. Scalar values and arrays in the configuration-specific file replace the corresponding values from the default file.

## Static Pages configuration

Static Pages reads its settings from the `staticPages` section. The `siteUrl` property defines the public origin of the generated site:

```json
{
  "staticPages": {
    "siteUrl": "https://www.example.com"
  }
}
```

`siteUrl` must be an absolute URL with a host. It should represent the production URL of the site rather than a local development, staging, or preview URL.

Other Blazorade libraries can add their own sections without conflicting with Static Pages:

```json
{
  "staticPages": {
    "siteUrl": "https://www.example.com"
  },
  "someOtherLibrary": {
    "someSetting": true
  }
}
```

Static Pages reads only the `staticPages` section and ignores sections owned by other libraries.

## Canonical URLs

Routable page components remain the source of truth for their routes:

```razor
@page "/products"
```

The generator combines the configured `siteUrl` with each discovered route:

```text
https://www.example.com + /products
= https://www.example.com/products
```

The resulting URL is used for generated canonical metadata, including:

```html
<link rel="canonical" href="https://www.example.com/products">
<meta property="og:url" content="https://www.example.com/products">
```

Routes do not need to be duplicated as properties on `StaticPage`.

## Optional configuration and validation

The configuration file is optional. If `blazorade.config.json` is missing, the build emits a warning and still generates pages. Metadata that requires `staticPages.siteUrl`, such as canonical URLs and sitemap locations, is omitted. A supplied relative `StaticPage.Image` value remains relative when no site URL is configured; it is resolved to an absolute URL only when `siteUrl` is available.

If the file contains a non-empty `staticPages.siteUrl`, the build reports an error when that value is invalid. An absent or empty value is treated like missing configuration. When supplied, the configured URL should:

- Be absolute.
- Include a host name.
- Use `https` for production sites.
- Not contain a fragment.
- Follow the generator's trailing-slash normalization rules.

The generator must not derive canonical URLs from the browser's runtime host because the same build can be served from local, preview, staging, and production hosts.

## Future configuration

See [Static generation](static-generation.md) for the complete list of generated metadata, output files, route behavior, and component usage. Additional build-time settings may be added to the `staticPages` section of `blazorade.config.json` as the generator grows. Possible areas include metadata defaults, output locations, sitemap generation, and static-hosting route configuration.
