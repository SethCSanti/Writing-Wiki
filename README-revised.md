# Worldbuilding Wiki

A private, owner-authored reference site for organizing the world, characters, history, and lore behind a series of books. It uses a wiki-like structure to connect and search information, but it is not a community wiki: the owner is the only person who can create or edit worldbuilding content. The site is the primary product. An Author Studio is a possible later phase and is outside the initial scope.

## Project goals

- Keep worldbuilding information in one structured, searchable place.
- Connect characters, nations, groups, locations, technologies, and events so related lore is easy to follow.
- Let the owner customize the organization while preserving essential starter categories.
- Keep the site and all story material private behind authentication.

## Technology

- **Frontend:** Next.js and TypeScript
- **Backend:** ASP.NET Core and C#
- **Data:** PostgreSQL with Entity Framework Core
- **Hosting direction:** Microsoft Azure

Azure is the planned hosting direction; production hosting is not configured. See Project status for what is implemented.

## Planned capabilities

- Protected starter categories for characters, locations, nations and civilizations, cultures, species, religions, factions and groups, artifacts, Weaving, languages, events, terms, and technologies.
- Customizable dashboards, tiles, sections, fields, and additional categories, while preserving protected base categories.
- Lore entries with images, tags, custom fields, links to related entries, and revision history.
- Search across entry content and supported custom fields.
- A relationship graph connecting lore entries.
- One universal-year timeline. Events link to their lore entries; national eras can appear as optional labels.
- An atlas for maps created elsewhere and uploaded by the owner, with era and map-type selectors, aligned image overlays, and location markers.
- Settings history for entry changes, archives, and restores.
- Private access with email/password login and required TOTP multi-factor authentication.

## Worldbuilding model

The current story notes describe three nations—Lykr, Nagøn, and Zisa—and a shared world timeline spanning multiple eras. The power system is called **Weaving**, with five fixed disciplines: Matter, Energy, Fields, Light, and Shadow. Subbranches can be added or removed. Entries will also organize conduits, technology, political groups, languages and dialects, trade, laws, conflicts, and characters.

The site is intended to hold and connect the author's original worldbuilding notes. Some private notes reference existing franchises as inspiration; those references are not a substitute for original story material and should be reviewed before any public release.

## Privacy and security

The initial release is for one owner account, with no public registration, contributor accounts, or community editing. All pages, APIs, search results, timeline and graph data, maps, images, downloads, and archives must require authentication. Search-engine directives are an additional precaution; authorization is the privacy boundary. No analytics, advertising trackers, or external AI processing are planned for the initial release.

Security requirements, cookie and privacy planning, backup targets, and release gates are recorded in the [pre-build architecture and security plan](Worldbuilding-Wiki-Prebuild-Architecture-Plan.md). The README and plan are not legal advice or a claim of legal compliance.

## Documentation

- [Pre-build architecture and security plan](Worldbuilding-Wiki-Prebuild-Architecture-Plan.md)

## Local development

The repository contains separate `web` (Next.js) and `api` (ASP.NET Core) services, plus PostgreSQL local development configuration. Production hosting is not configured.

### Prerequisites

- Node.js 20+ and Bun 1.3.14 (the package manager is pinned in `web/package.json`).
- .NET SDK 10.0.302 (pinned in `global.json`).
- Docker Compose.

### Start locally

1. Copy `.env.example` to `.env` at the repository root and `api/.env.example` to `api/.env`; set the same local database password in both. Copy `web/.env.local.example` to `web/.env.local`. Do not commit `.env` files. Email is required for owner verification and password reset.
2. Start PostgreSQL with `docker compose up -d postgres`.
3. In `api/`, export the API example settings with `set -a; . ./.env; set +a`, run `dotnet tool restore`, `dotnet restore`, then `dotnet ef database update --project WorldbuildingWiki.Api` to apply migrations. Set a long random `OwnerSetup__Key` and working SMTP settings, then start the API with `dotnet run --project WorldbuildingWiki.Api`.
4. In `web/`, run `bun install --frozen-lockfile`, then `bun dev`.
5. Open `http://localhost:3000/setup` and provision the single owner. Enroll TOTP and save the one-time recovery codes offline. Sign in at `/login`. Password reset requires configured SMTP settings.

The API listens on `http://localhost:5080` in development and exposes `/health` and `/health/ready`. The readiness endpoint checks PostgreSQL. The web server proxies `/api/*` to the API; do not expose the API listener to browsers in a deployed topology. The gateway forwards the browser scheme so secure antiforgery cookies work across internal HTTP hops. The API trusts forwarded headers only from loopback and explicitly configured proxy IPs; configure exact trusted ingress addresses in production. Local configuration examples contain placeholders only.

Session defaults (30-minute idle, 8-hour absolute), SMTP wiring, proxy addresses, and Data Protection key persistence are provisional pending architecture review. Identity authenticator and recovery token values are Data Protection encrypted in the Identity token table; production requires a durable, access-controlled key ring.

### Checks

- Frontend: `bun run lint`, `bun run typecheck`, `bun run build`.
- API: `dotnet build api/WorldbuildingWiki.sln` and `dotnet test api/WorldbuildingWiki.sln`.
- CI runs the frontend static checks/build and the API test suite. It does not deploy.

## Project status

**Private access phase.** Email/password identity, required TOTP, recovery codes, one-time owner setup, password reset, server-managed cookies, CSRF validation, and authorization are implemented. Domain features and production hosting remain future phases. Author Studio features, including themes and subplots, are deferred.
