# TODO

## Configurable trailing-slash URLs

Add build-time support for applications whose public URLs use trailing slashes, while preserving the current behavior by default.

- [ ] Add `staticPages.trailingSlash` to the build-time configuration model.
- [ ] Keep the default value `false` for backward compatibility.
- [ ] Centralize route-to-public-URL construction in the generator.
- [ ] Apply the setting to canonical URLs.
- [ ] Apply the setting to `og:url`.
- [ ] Apply the setting to sitemap locations.
- [ ] Generate Static Web Apps route aliases for both slash and non-slash requests.
- [ ] Keep the Razor `@page` route as the route identity.
- [ ] Ensure root URLs are not given an additional slash.
- [ ] Prevent duplicate slashes when combining `siteUrl` and routes.
- [ ] Keep live `StaticMetadata` canonical behavior consistent with generated metadata.
- [ ] Add tests for root, nested, already-normalized, and slash-variant routes.
- [ ] Update `docs/configuration.md` and `docs/static-generation.md`.
- [ ] Update the package README and relevant build instructions.
