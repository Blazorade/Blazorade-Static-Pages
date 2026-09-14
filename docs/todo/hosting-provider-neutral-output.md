# Hosting-provider-neutral output

**Priority:** 4

Separate provider-neutral static output from hosting-specific route configuration.

## Proposed scope

- Define a provider-neutral route manifest containing generated routes, files, redirects, and fallback behavior.
- Make `staticwebapp.config.json` generation explicitly configurable or opt-in.
- Keep Azure Static Web Apps support as a hosting adapter.
- Consider adapters for GitHub Pages, Netlify, and Cloudflare Pages.
- Ensure adapters consume the same analyzed page and route model.
- Document provider-specific limitations such as fallback and HTTP 404 behavior.
- Add diagnostics for conflicting provider output files and routes.
