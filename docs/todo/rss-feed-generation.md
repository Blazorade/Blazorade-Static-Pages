# RSS feed generation

**Priority:** 1

Support generating an RSS 2.0 feed from the same statically analyzable page model that
is used to generate static HTML pages.

## Proposed scope

- Generate the feed during the existing Static Pages build step, alongside the HTML
	pages, sitemap, and static-hosting configuration.
- Write the feed to the generated output directory so it is copied to both build and
	publish `wwwroot` output like the other generated files.
- Configure the feed in the consuming application's root `blazorade.config.json` file,
	under the `staticPages` configuration section. Configuration-specific overrides such
	as `blazorade.config.Release.json` continue to apply:

	```json
	{
		"staticPages": {
			"siteUrl": "https://www.example.com",
			"rss": {
				"file": "feed.xml",
				"route": "/feed",
				"itemCount": 20,
				"includeContent": true
			}
		}
	}
	```

	- `rss.file` defines the physical generated file name in the generated web root. The
	  default is `feed.xml` when RSS is enabled. The `.xml` suffix is a useful convention
	  for the physical file, but is not an RSS requirement.
	- `rss.route` defines the public friendly route that Static Web Apps rewrites to
	  `rss.file`. It must be an absolute site-relative path and defaults to `/feed`.
	- With the defaults, both `/feed.xml` (the physical file) and `/feed` (the rewrite route)
		return the same feed. The feed's canonical channel link should use `rss.route`.
	- `rss.file` and `rss.route` may be configured independently. The file name must not
	  escape the generated web root, and the resulting file and route must not conflict
	  with generated page paths unless intentionally configured to the same value.
	- `rss.itemCount` defines the maximum number of feed items. It must be a positive
	  integer; the default is 20.
	- `rss.includeContent` controls whether each item includes the complete static
	  article content in `content:encoded`. The default is `true`.
- RSS generation is opt-in. No feed is generated when the `rss` configuration object
	is absent.
- Require `staticPages.siteUrl` when RSS is enabled because RSS item links and the
	channel link must be absolute URLs. Report a clear build error when it is missing or
	invalid.

## Feed item selection and content

- Include static pages unless `StaticPageAttribute.IncludeInRss` is explicitly
	`false`. This is separate from `IncludeInSitemap` because sitemap and RSS audiences
	have different purposes.
- Treat the statically generated pages with valid `StaticMetadata.Date` values as posts
	eligible for the feed. Pages without a publication date are not RSS items.
- Sort eligible posts by their `StaticMetadata.Date` value descending, so the newest
	posts are first, and take the configured `rss.itemCount` items. The default feed
	therefore contains the 20 latest dated posts.
- Use the existing static metadata for the RSS item fields:
	- `title` from `StaticMetadata.Title`.
	- `description` from `StaticMetadata.Description` when supplied. This excerpt is
	  always included, regardless of `rss.includeContent`.
	- `link` and `guid` from the absolute canonical page URL.
- `pubDate` from the UTC-normalized `StaticMetadata.Date`.

- Always include the standard RSS item metadata and the excerpt. When
	`rss.includeContent` is `true`, also emit the complete `StaticContent` fragment
	in a `content:encoded` element wrapped in CDATA. Interactive content is never
	included.
- Safely serialize CDATA content that contains the `]]>` sequence.
- Generate a channel title and description from new RSS configuration values, with
	sensible defaults based on the site URL. The channel's `link` is the configured site
	URL.
- XML-escape all values and emit valid RSS 2.0 XML with UTF-8 encoding.
- Do not execute components, fetch runtime data, or include interactive content while
	generating the feed. The feed must remain deterministic and build-time only.

## Output and hosting

- Normalize the configured path consistently with generated static page paths and
	prevent path traversal outside the generated output directory.
- Add the generated feed path to the Static Web Apps fallback exclusions so a request
	for the feed is served as an XML file rather than rewritten to `index.html`.
- Add an explicit Static Web Apps rewrite from `rss.route` to `rss.file`. Both paths
	must be excluded from the navigation fallback.
- Replace the feed on every generation and ensure stale output is not silently served
	when RSS is disabled or its path changes.

## Acceptance criteria

- A configured feed is generated during both build and publish.
- The configured path and item limit are honored.
- Items are selected, ordered, truncated, and serialized according to the rules above.
- Missing site URL, invalid RSS settings, invalid paths, and invalid limits produce
	actionable build diagnostics.
- Pages excluded with `IncludeInRss = false` and pages without valid dates are omitted.
- Output is valid RSS 2.0 XML, uses absolute URLs, escapes XML content, and is excluded
	from Static Web Apps navigation fallback.
- Existing HTML, sitemap, and route-generation behavior remains unchanged.

## Deliberately deferred

- Atom or JSON Feed output.
- Multiple feeds.
- Categories, enclosures, and media namespaces.
- Runtime or externally supplied feed items.
- Automatic feed discovery markup in generated HTML pages.

- [ ] Support generating RSS feeds from content used to generate static HTML.
