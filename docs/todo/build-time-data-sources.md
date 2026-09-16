# Build-time publishing, data sources, and data-driven routes

**Priority:** 2

Introduce a separate `Blazorade.StaticPages.Publishing` package that transforms deterministic external content or structured data into ordinary `.razor` pages before `Blazorade.StaticPages` analyzes the application.

The primary architectural boundary is:

```text
Markdown / data / external content
	↓
Blazorade.StaticPages.Publishing
	↓
generated .razor pages
	↓
Blazorade.StaticPages
	↓
static HTML and other BSP output
```

Publishing is an input-stage tool, not a second static site generator. The generated Razor source remains part of the consuming Blazor application and is the input that BSP processes.

## Architectural decisions

- Keep publishing in the separate `Blazorade.StaticPages.Publishing` package; do not add Markdown, CMS, JSON, YAML, or authoring concerns to the BSP core.
- Make Markdown the first concrete publishing workflow, based on the existing blog conversion process.
- Generate inspectable Razor files through a provider model that allows application developers to plug in their own content-generation pipeline and choose where generated files are stored.
- Provide a default provider implementation with a configurable target folder, defaulting to the `Pages` folder in the target Blazor WebAssembly application.
- Generate concrete routes such as `/articles/article-one` rather than teaching BSP to materialize instances of `/articles/{slug}` from arbitrary external data.
- Ensure generated pages use normal BSP constructs, such as `@page`, `[StaticPage]`, `StaticMetadata`, and `StaticContent`; BSP must not need to know how they were produced.
- Keep the transformation deterministic and independent of the consuming application's runtime DI container, browser execution, JavaScript interop, authentication, or user-specific state.
- Define the provider model around the concrete publishing workflow, while allowing providers to own their content-generation pipeline and output storage.

## Proposed scope

### Initial implementation: Markdown publishing

- Read Markdown source documents and front matter or equivalent metadata.
- Convert Markdown content to HTML suitable for inclusion in generated Razor source.
- Map source metadata to BSP metadata constructs.
- Generate a normal routable `.razor` page containing the required BSP declarations.
- Generate concrete routes from the source path, slug, or explicit metadata.
- Support headings, links, code blocks, and image references without requiring BSP-specific Markdown handling.
- Report malformed Markdown, invalid metadata, unsupported content, duplicate routes, and other publishing errors before BSP processing begins.

### Assets

Treat referenced assets as part of publishing, independently of BSP:

- identify local assets referenced by Markdown;
- detect missing assets and path traversal or invalid references;
- copy assets to a configured static asset location where appropriate;
- rewrite generated references when the destination path differs; and
- detect or define behavior for collisions and duplicate assets.

Asset handling may be implemented after the basic Markdown-to-Razor transformation, but it must remain within Publishing's responsibility.

### Later data-driven publishing

Once the Markdown workflow establishes the common model, evaluate support for JSON, YAML, CMS exports, and other deterministic sources. These sources may materialize pages such as:

```text
products.json
	↓
Publishing
	↓
Products/product-a.razor
Products/product-b.razor
	↓
BSP
```

If a normalized page model or provider contract is introduced, it should supply only the concepts demonstrated to be common and should remain build-time-only. It may eventually support route, metadata, content, assets, and output path, but should not become a general content framework prematurely.

## Build integration

Publishing must run early enough that generated `.razor` files participate in the ordinary Blazor/Razor compilation and are visible to BSP's static-page analysis:

1. Read publishing inputs.
2. Generate Razor pages and process configured assets.
3. Include generated files in the Blazor application build.
4. Run BSP static-page analysis and generation.
5. Generate static HTML and other BSP artifacts.
6. Publish the Blazor application.

Investigate the cleanest MSBuild/Razor integration point, including incremental builds, clean builds, generated-file cleanup, design-time builds, and whether generated files should be committed or ignored. The default provider should expose its target folder as configuration, defaulting to the target application's `Pages` folder. Custom providers may store generated Razor pages elsewhere, provided they make the files available to Razor compilation and BSP analysis.

Build-time execution is the intended model for the entire package family. BSP generates static HTML during the build, and related packages—including BSPP—should follow the same principle: read deterministic inputs, produce deterministic build artifacts, and pass those artifacts to the next build stage. Publishing should therefore integrate into the consuming project's build through an MSBuild target or equivalent build integration rather than requiring runtime execution or application startup.

## Diagnostics and determinism

All publishing, Razor compilation, and BSP analysis failures are build-time errors. They do not need to be classified by which package detected them. The combined build output should provide clear, actionable diagnostics with enough context for a developer to understand the problem and determine how to fix it.

Where applicable, diagnostics should include the source path, generated file path, route, and line and column information for:

- duplicate routes;
- invalid or missing required metadata;
- malformed source documents;
- unsupported Markdown or source features;
- missing or conflicting assets; and
- output-path or route collisions.

Given the same sources, configuration, templates, and assets, Publishing must produce byte-for-byte stable Razor output where practical. Avoid timestamps, machine-specific paths, unordered traversal, and other nondeterministic output.

## Deliberately excluded

- Final static HTML generation; that remains the responsibility of `Blazorade.StaticPages`.
- Runtime Blazor rendering or replacing the consuming application's website architecture.
- A site-wide content model, runtime content repository, or Scraibe-style static site generator.
- Arbitrary application code execution.
- Authentication-dependent or user-specific content.
- Browser APIs or JavaScript interop during generation.

## Output ownership and source of truth

The configured publishing inputs are authoritative. Generated Razor files are derived build artifacts, even when a project chooses to keep them in source control for inspection. The provider that generates them owns their output location and lifecycle, including removing stale generated files where that is safe. The default provider must define this behavior for its configured target folder; custom providers may apply their own lifecycle rules.

Publishing the same content to multiple sites is not an intended publishing scenario. Separate site outputs can avoid filesystem collisions, but duplicating identical content across hosts creates ambiguity about the authoritative source and can negatively affect how search crawlers and AI systems interpret and rank the content. Multi-site publishing should therefore not be encouraged by the package design; any future syndication scenario should require explicit canonical-source and content-distribution decisions.

The provider model owns output placement. The default provider uses a configurable target folder and defaults to `Pages` in the target Blazor WebAssembly application. A custom provider may choose another storage location or generation pipeline, but its output must still be included in the target application's Razor compilation so that BSP can process it.

`Blazorade.StaticPages.Publishing` (BSPP) should reference `Blazorade.StaticPages` (BSP) directly. BSP is a stable release, so BSPP can rely on its established components, attributes, and metadata constructs. The package reference also communicates the exact BSP version that BSPP is compatible with and ensures that the Razor source generated by BSPP targets constructs supported by that BSP version.

If BSP later introduces breaking changes to those constructs, the corresponding BSPP release must also be treated as breaking or updated to target the new BSP version. This keeps the dependency one-way—`BSPP → BSP`—while making compatibility explicit through normal package versioning. Publishing remains responsible for transforming source material into Razor; BSP remains responsible for analyzing those generated pages and producing static output.

## Viewpoints to address during implementation

- Prefer a small Markdown-specific vertical slice first: source document → generated Razor page → normal Razor compilation → BSP output. This will expose the real integration contract before abstractions are frozen.
- Keep the generated `.razor` source inspectable and make it possible to run Publishing without running the application. This gives a clear boundary for debugging and reproducibility.
- Treat the generated Razor format as an internal contract initially. Avoid promising a broad normalized page model until at least one non-Markdown source demonstrates that it reduces duplication.
- Make route and asset collision validation happen before files are written, or use a staging directory and commit outputs only after the complete operation succeeds, to avoid partial and misleading generated output.
- Validate the MSBuild ordering with a minimal sample application early. Correct ordering is the main risk: generated files that appear after Razor compilation or BSP discovery will make the feature appear intermittently or silently incomplete.
