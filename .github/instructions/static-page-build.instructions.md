---
description: "Build-time rules for Blazorade Static Pages source analysis, static HTML generation, and NuGet build integration."
applyTo: "src/Blazorade.StaticPages.Generator/**/*.cs,src/Blazorade.StaticPages.Generator/**/*.csproj,src/Blazorade.StaticPages.Generator.Host/**/*.cs,src/Blazorade.StaticPages.Generator.Host/**/*.csproj,src/Blazorade.StaticPages/buildTransitive/**,src/Blazorade.StaticPages/README.md,**/Directory.Build.targets,docs/objective-and-design-principles.md"
---
# Blazorade Static Pages build process

## Source model

- The consuming Blazor WebAssembly application is the source of truth.
- Static page generation is source-level analysis of `.razor` files.
- Do not render the application or execute arbitrary application code during generation. Static output must not depend on browser APIs, JavaScript interop, authentication state, user state, or external runtime data.
- Do not use runtime service resolution merely to obtain application data during generation.
- `InteractiveContent` excludes its complete descendant subtree during source analysis.
- Do not parse Razor files with regular expressions. Use Razor syntax/parser APIs or another syntax-tree-based implementation.

## Page analysis

- Discover routable components from `@page` directives in `.razor` source files.
- A page is static only when its markup has a `StaticPageAttribute`.
- Read `StaticMetadata` values from statically evaluable expressions, not only literal attributes. For example, a title variable initialized from a string literal may be used both as `Title="@title"` and as `@title` in descendant markup.
- Resolve local constants, fields, and variables whose values can be proven from literals and other statically evaluable expressions. The same resolved value must be used consistently wherever the symbol appears.
- Supported value propagation may include literal strings, numeric and Boolean literals, constant references, string concatenation, and interpolation whose operands are statically evaluable. The supported expression subset must remain explicit and deterministic.
- Do not execute methods, property getters, lifecycle code, DI, or arbitrary C# to resolve a value. Report expressions that cannot be proven statically instead of guessing or silently omitting them.
- Only content inside `StaticContent` is included as static HTML. `StaticMetadata` supplies page metadata and does not contribute body markup.
- `InteractiveContent` excludes its complete descendant subtree.
- For reusable component tags, include only the component's `StaticContent` subtree.
- Never blindly include all output from a reusable component.
- Report unsupported syntax, ambiguous routes, parameterized routes, and invalid static fragments with the source file and route.

## Output mapping

- `/` maps to `<component-name>.html`; for example, `Home.razor` with `@page "/"` maps to `Home.html`.
- `/products` maps to `products.html`.
- `/products/warranty` maps to `products/warranty.html`.
- Generated pages are complete HTML documents containing the analyzed static content, page metadata, application shell, and Blazor WebAssembly bootstrapper.
- Generated files must not be written to the consuming project's source `wwwroot`.
- Generate into the intermediate output first, then copy into build and publish output `wwwroot`.

## Generated deployment files

- Generate `sitemap.xml` only for pages whose `IncludeInSitemap` value is true.
- Generate `staticwebapp.config.json` with explicit route rewrites before the navigation fallback.
- Preserve the agreed fallback exclusions for HTML files, assets, `_content`, and `_framework`.
- Use the configured production `staticPages.siteUrl` for canonical URLs and sitemap locations. Never derive canonical URLs from the browser host.

## Build and packaging

- Keep package version highlights in descending version order, with the latest release first.
- The runtime library contains the source-analysis marker components and is the only normal compile-time dependency of the WASM application.
- The generator identifies marker components by their Razor element names while analyzing source; it must not depend on runtime rendering or marker output.
- The generator and host are build-time tooling and must not become WASM application references.
- Package generator tooling under NuGet `tools/net10.0` and MSBuild integration under `buildTransitive`.
- MSBuild must invoke generation after the complete application build output is finalized, so later Blazor WebAssembly output steps cannot remove generated files.
- The repository-local build target may build the generator host automatically, but it must avoid project/build cycles and Visual Studio parallel-build races.
- Keep generated files in `obj/.../Blazorade.StaticPages/generated` before copying them to `bin/.../wwwroot` or the publish directory.

## Design constraints

- Keep static analysis deterministic and side-effect free.
- Preserve useful source locations in diagnostics.
- Prefer explicit failures over silently generating incomplete HTML.
- Update this instruction when the component contract, output mapping, build lifecycle, or packaging structure changes.
