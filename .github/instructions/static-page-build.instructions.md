---
description: "Build-time rules for Blazorade Static Pages source analysis, static HTML generation, and NuGet build integration."
applyTo: "src/Blazorade.StaticPages.Generator/**/*.cs,src/Blazorade.StaticPages.Generator/**/*.csproj,src/Blazorade.StaticPages.Generator.Host/**/*.cs,src/Blazorade.StaticPages.Generator.Host/**/*.csproj,src/Blazorade.StaticPages/buildTransitive/**,**/Directory.Build.targets,docs/product-direction.md"
---
# Blazorade Static Pages build process

## Source model

- The consuming Blazor WebAssembly application is the source of truth.
- Static page generation is source-level analysis of `.razor` files.
- `HtmlRenderer` may be used when it produces the required static output, but generated pages must remain deterministic and must not depend on browser APIs, JavaScript interop, authentication state, user state, or external runtime data.
- Component rendering must not include descendants marked with `InteractiveContent`; the marker component must suppress that subtree in generated output.
- Do not execute arbitrary application code or use runtime service resolution merely to obtain application data during generation.
- Do not parse Razor files with regular expressions. Use Razor syntax/parser APIs or another syntax-tree-based implementation.

## Page analysis

- Discover routable components from `@page` directives in `.razor` source files.
- A page is static only when its markup contains `StaticPage`.
- Read `StaticPage` metadata from statically evaluable expressions, not only literal attributes. For example, a title variable initialized from a string literal may be used both as `Title="@title"` and as `@title` in descendant markup.
- Resolve local constants, fields, and variables whose values can be proven from literals and other statically evaluable expressions. The same resolved value must be used consistently wherever the symbol appears.
- Supported value propagation may include literal strings, numeric and Boolean literals, constant references, string concatenation, and interpolation whose operands are statically evaluable. The supported expression subset must remain explicit and deterministic.
- Do not execute methods, property getters, lifecycle code, DI, or arbitrary C# to resolve a value. Report expressions that cannot be proven statically instead of guessing or silently omitting them.
- Content directly inside `StaticPage` is included as static HTML.
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

- The runtime library contains transparent marker components and is the only normal compile-time dependency of the WASM application.
- The generator library may reference the runtime library so it can identify marker component types.
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
