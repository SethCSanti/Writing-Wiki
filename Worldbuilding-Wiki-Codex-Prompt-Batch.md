# Codex Prompt Batch — Private Worldbuilding Wiki

Use these prompts in order in the project repository. Each prompt is a separate Codex task. The first is planning-only; do not start implementation until its pre-build gate is complete.

## Prompt 0 — Repository audit and pre-build architecture gate

Read the repository instructions, existing code, source story notes, and the approved “Worldbuilding Wiki — Pre-Build Architecture and Security Plan.” If that plan is not in the repository, ask me to add it or use the requirements below as the source of truth. Do not modify application code yet.

This is a private, owner-only worldbuilding wiki. The agreed stack is Next.js with TypeScript, ASP.NET Core with C#, PostgreSQL with EF Core/Npgsql, and Azure as the provisional host. The first release has one manually provisioned owner account, no public registration, email/password plus required TOTP, offline recovery codes, no SMS fallback, no analytics/ad tracking, and no external AI processing. Author Studio, themes, and subplots are deferred.

Inspect the repo and produce or update planning documents for:
1. Current-state architecture and a compact component/data-flow diagram.
2. Domain model and entry/category schema.
3. Threat model and security/privacy requirements.
4. Data inventory, cookie inventory, retention/deletion plan, and processor list.
5. Architecture decisions and a phased implementation/test plan.
6. A clear open-decision register.

Do not silently resolve unknown legal or operational details. Record recommendations for jurisdiction, Azure region/budget, domain/DNS, email delivery, session lifetime, log retention, file upload limits, and permanent deletion. For legal text, prepare requirements and factual placeholders, not claims of legal compliance. This gate is complete only when the architecture and remaining decisions are reviewable.

## Prompt 1 — Repository foundation and design system

Using the approved architecture documents, adapt the existing repository rather than replacing it. If it is empty, establish the Next.js TypeScript frontend and ASP.NET Core C# API as separate deployable services, PostgreSQL/EF Core/Npgsql, local development configuration, migrations, and a basic CI check. Keep versions compatible and pinned; follow the repository’s package manager and conventions.

Create the shared application shell, navigation, loading/error states, and design tokens. Use a restrained palette of light wood, dark green, beige, and white with accessible text contrast, keyboard-visible focus, and responsive layouts. Do not add public content routes or author-studio features.

Keep secrets out of source control. Add example configuration with placeholders, health checks, and developer documentation. Do not make production deployment changes in this phase. Run the existing checks and report the files changed, verification performed, and any unresolved decisions.

## Prompt 2 — Authentication and private access boundary

Implement ASP.NET Core Identity email/password authentication with required TOTP MFA and one-time recovery codes. Do not implement SMS MFA or public registration. Provision the first owner through a one-time setup path and disable that path after setup. Implement verified-email password reset with short-lived single-use tokens, rate limits, generic responses, login/MFA throttling, and session revocation.

Use server-managed cookies only; do not store session tokens in browser local storage. Set Secure, HttpOnly, host-only, and an appropriate SameSite policy. Protect state-changing requests against CSRF. Rotate sessions after login/MFA and require reauthentication for security-sensitive account changes. Keep the browser on one HTTPS origin, with the frontend acting as the gateway to an API that is not directly exposed to browsers where feasible.

Enforce authorization in both frontend server routes and every backend endpoint. No unauthenticated access to pages, search, graph/timeline data, map layers, images, downloads, exports, or archived revisions. Add noindex/noarchive headers and avoid a sitemap as defense in depth; do not treat robots.txt as access control. Add tests proving unauthenticated requests are denied across all resource types and authenticated owner requests work. Never log passwords, codes, tokens, session IDs, or story content.

## Prompt 3 — Domain model, protected defaults, and change records

Design and implement the core EF Core model and migrations for users, categories, category templates, fields, entries, content blocks, tags, typed relationships, media references, universal dates, archives, and revisions. Use flexible typed fields without making the entire wiki an unvalidated JSON blob. Preserve referential integrity and support future schema evolution.

Seed these protected starter categories: Characters, Locations, Nations/Civilizations, Cultures, Species, Religions, Factions/Groups, Artifacts, Weaving, Languages, Events, Terms, and Technologies. The base categories cannot be deleted. User-created categories and fields can be removed; when they contain data, archive them and preserve entry links/history. Allow names, layouts, and field settings to be customized within the agreed protected-template rules.

Weaving has five fixed branches: Matter, Energy, Fields, Light, Shadow. Branch names cannot be removed. Subbranches are addable/removable and initially seeded from the story notes:
- Matter: Earth > Metal; Water; Air; Organic matter.
- Energy: Heat; Motion; Chemical reactions; Nuclear energy; Pure energy.
- Fields: Gravity; Magnetism; Electricity; Force manipulation.
- Light: Illumination; Lasers; Optical illusions.
- Shadow: Concealment; Darkness manipulation; Shadow constructs; Supernatural shadow travel.

Common entry fields: title, summary, images, rich-text body, tags, and linked entries. Support field types for text, rich text, number, date, checkbox, dropdown, entry link, and image/gallery. Each custom field can be marked searchable, filterable, and sortable.

Seed the agreed starter fields: Nation entries include region, language/dialect, imports/exports, economy, entry/exit laws, dominant conduit type, groups, maps, and conflicts. Group entries include type, purpose/ideology, membership, territory, leadership, alliances/opposition, and history. Character entries include aliases, nation/culture, universal birth/death years, affiliations, relationships, Weaving/conduit information, and Avatar/Marked status. Technology entries include type, era, creator, operation, compatible disciplines, uses/limits, and social impact. Location entries include type, nation/region, map markers, population, and historical changes. Language entries support a shared parent language and linked dialect entries (Lykaya, Mønga, Zisan). Culture, Religion, Species, Artifact, and Terms entries use the field sets in the approved architecture plan.

Create an auditable change model for entry edits, category/layout changes, archivals, restores, and deletions. Do not build all UI in this phase. Add meaningful migration and model tests.

## Prompt 4 — Wiki entry editor, category customization, and media library

Build authenticated category dashboards and entry pages on top of the approved domain model. Start with the protected default category layouts. Let the owner customize tiles, dashboard layout, section order, and field presentation; add custom categories, fields, and subbranches; and remove user-added items. Never allow removal of protected categories or the five Weaving branches. Preserve data and links when removing an item that has content.

Use TipTap for rich text with a constrained, safe extension set. Support reorderable blocks such as headings, text, images/galleries, and linked entries. Add internal links and backlinks, tags, captions, and alt text. Render rich text safely and prevent stored XSS.

Build a shared private media library. Keep media files inaccessible unless the owner is authenticated. On entry removal, remove the reference only; make permanent file deletion a separate explicit action. Accept only the approved raster image formats, verify actual file content, enforce the approved size/dimension limits, use safe random storage names, strip EXIF data, and generate safe thumbnails. Keep maps and images private in storage and private through every delivery path.

Use the agreed light-wood, dark-green, beige/white visual system and accessible keyboard interactions. Add component and integration tests for customization permissions, field validation, editor rendering, and media authorization.

## Prompt 5 — Settings history, archive, compare, and restore

Build Settings > History. Show entry edits, archivals/restores/deletions, category and layout changes, and their timestamps. For entry revisions, allow the owner to inspect a revision, compare it with the current version, and restore it as a new revision. Preserve the audit trail when restoring; do not erase intervening history.

Archived entries/categories remain recoverable with their links and media references intact. Deleting an image from an entry removes the link only; permanent media deletion is separate and clearly labeled. Require confirmation for irreversible deletion. Add authorization and restore tests, including linked-entry and map-marker integrity.

## Prompt 6 — Search and navigation

Implement authenticated global search over titles, summaries, rich-text body, tags, and custom fields explicitly marked searchable. Add category, tag, and filterable-field filters; sortable fields; useful empty/loading/error states; and pagination for larger result sets. Do not send story content to external search services. Search results and snippets must follow the same authorization rules as the source entries.

Add navigation between categories and linked entries, including backlinks. Ensure archived entries behave consistently with the approved visibility rules. Add tests for field flags, filters, sorting, archived records, authorization, and search-snippet leakage.

## Prompt 7 — Relationship graph

Implement the relationship graph with Sigma.js and Graphology. This library is only for the knowledge graph, not the timeline or Atlas. Represent wiki entries as nodes and typed relationships as edges. Provide a focused graph from an entry and an optional full-universe view, with category and relationship filters. Selecting a node opens its entry. Keep the graph responsive for larger datasets and respect all entry authorization rules.

Do not add analytics or export endpoints that leak graph data. Test graph construction from relationship records, filters, navigation, and unauthorized access.

## Prompt 8 — Universal timeline

Build one shared interactive timeline ordered by the universal year. A nation’s era name is an optional date label on that same chronology, never a separate timeline. Timeline items are lore Event entries; clicking an item opens its own entry. Universal start/end years support approximate dates and ranges. Render multi-year events as bars across their range and represent uncertain dates clearly without inventing precision.

Use a TypeScript/React implementation compatible with the calendar model rather than coercing fictional years into Gregorian JavaScript dates. Support pan/zoom, keyboard navigation, useful empty states, and filters by nation, category, and linked group/character where appropriate. Add tests for sorting, negative/large years if supported by the chosen model, ranges, uncertainty, labels, and authorization.

## Prompt 9 — Atlas and uploaded maps

Build an authenticated Atlas for maps created elsewhere and uploaded by the owner. Support multiple map sets for different eras, each with its own base image and uploaded aligned overlays. Provide an era selector and a map-type selector. Keep the base map visible and show only one selected overlay at a time: territory, terrain, roads/infrastructure, or population density. Require overlay dimensions to match the base image and give a clear validation error when they do not.

Allow markers to be placed on the map and linked to Location entries. Keep marker positions attached to the selected map set. Opening a marker opens the location lore entry. Support past and modern map sets without modifying the uploaded source image. Keep every image, overlay, and marker API private and authenticated. Add tests for dimensions, selectors, markers, location links, and access denial.

## Prompt 10 — Security, privacy, deployment, and launch gate

Review the implemented app against the approved threat model and architecture plan. Confirm no page, API, search result, graph/timeline endpoint, export, thumbnail, media file, or map overlay is publicly accessible. Verify HTTPS, private database/blob access, managed identity/Key Vault usage, secure cookies, CSRF protection, safe rich-text rendering, upload validation, rate limits, and secret/dependency scanning.

Use the approved backup baseline: PostgreSQL point-in-time recovery for 35 days; private media with 30-day soft-delete recovery and bounded version history; encrypted monthly database/media copies in a separate private location for 12 months. Test a full restore and document the runbook. Add alerts for repeated authentication failures and service/backup failures. Keep logs free of story content and secrets.

Prepare a factual privacy/cookie notice that matches actual cookies and data flows. With only strictly necessary cookies and no analytics, do not add a generic tracking-consent banner by default; leave jurisdiction-specific legal copy for review. Do not claim legal compliance or draft definitive legal advice. Document processors, retention, account deletion/export, uploaded-file rights, incident response, and future collaborator onboarding. Review the story’s franchise references before any sharing or public release.

Finish with a launch checklist, deployment/rollback instructions, recovery runbook, security test results, and a list of remaining decisions (jurisdiction, Azure region/budget, domain, email provider, session lifetime, log retention, upload limits, and deletion semantics). Do not deploy publicly or enable additional users. Report any failed checks and fix them before calling the build launch-ready.

## Global instructions for every implementation prompt

- Read and follow repository instructions first. Preserve existing conventions and generated-code workflows.
- Make the smallest coherent change for the phase; do not silently broaden scope.
- Keep the whole wiki private. No public registration, publishing mode, external AI processing, or Author Studio in this phase.
- Never put secrets, story content, or uploaded media into logs, public URLs, test fixtures, analytics, or third-party services.
- Do not claim compliance based only on headers or robots directives.
- Run focused meaningful tests and the repo’s required checks. Report changes, tests, risks, and any remaining decisions.
- Stop and ask before making a decision that conflicts with an agreed requirement or changes data/security behavior.
