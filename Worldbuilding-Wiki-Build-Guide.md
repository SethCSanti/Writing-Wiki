# Worldbuilding Wiki — Project and Build Guide

This guide consolidates the project overview, architecture and security plan, and implementation sequence into one working specification. It is intended to guide the owner’s own development of the application. It does not assume AI-driven development.

## 1. Product definition

The Worldbuilding Wiki is a private, owner-authored reference site for organizing the world, characters, history, and lore behind a series of books. It uses wiki-like linking and navigation, but it is not a community wiki: one owner account can create and edit the worldbuilding content. There is no public registration, contributor access, or community editing in the first release.

The primary purpose is to make a large fictional universe easy to document, connect, search, and maintain. The site is also a software development project the owner will implement. An Author Studio—including themes and subplot tools—is a possible later phase and is out of scope for the first release.

### Goals

- Keep worldbuilding information in one structured, searchable place.
- Link characters, nations, groups, locations, technologies, and events.
- Let the owner customize dashboards, fields, layouts, and additional categories while protecting the core starter structure.
- Keep all pages and story materials private behind authentication.
- Keep the codebase and content under the owner’s control.

### Out of scope for the first release

- Multiple users, invitations, public registration, comments, publishing, or public lore pages.
- Author Studio, themes, and subplot management.
- Map creation tools; the Atlas displays map images created elsewhere and uploaded by the owner.
- Analytics, advertising trackers, external AI processing of story content, external search services, and third-party media processing.
- Public production deployment until the launch gates and open decisions in this document are resolved.

## 2. Technology and current project status

### Planned stack

- **Frontend:** Next.js and TypeScript.
- **Backend:** ASP.NET Core and C# with ASP.NET Core Identity.
- **Data:** PostgreSQL with Entity Framework Core and Npgsql.
- **Hosting direction:** Microsoft Azure. Proposed services include Azure Container Apps, Azure Database for PostgreSQL Flexible Server, private Azure Blob Storage, and Key Vault. Product SKUs, region, topology, and cost are not yet approved.

### Status reported in the revised README

The repository has separate `web` (Next.js) and `api` (ASP.NET Core) services and PostgreSQL local development configuration. The private-access phase is reported as implemented: email/password identity, required TOTP, recovery codes, one-time owner setup, password reset, server-managed cookies, CSRF validation, and authorization. Domain features and production hosting remain future phases. Azure production hosting is not configured.

The older pre-build plan contains an earlier repository audit saying that no application source existed. That audit is historical and is superseded by the status above. Verify the actual repository before relying on any implementation claim or starting a migration.

### Local development reference

Prerequisites listed in the README:

- Node.js 20+ and Bun 1.3.14.
- .NET SDK 10.0.302.
- Docker Compose.

The documented local flow is to configure the root, API, and web example environment files; start PostgreSQL; restore .NET tools and packages; apply the EF migration; configure a long random owner setup key and SMTP; start the API; install frontend dependencies with Bun; start the web app; and provision the single owner at `/setup`. TOTP enrollment and offline storage of the one-time recovery codes are part of setup. Password reset requires working SMTP.

The API is documented at `http://localhost:5080` in development with `/health` and `/health/ready`; the web server proxies `/api/*`. Do not expose the API listener directly to browsers in a deployed topology. Configure forwarded-header trust for exact production proxy addresses. Local configuration files must contain placeholders only; never commit `.env` files.

Documented checks:

- Frontend: `bun run lint`, `bun run typecheck`, and `bun run build`.
- API: `dotnet build api/WorldbuildingWiki.sln` and `dotnet test api/WorldbuildingWiki.sln`.
- CI runs frontend checks/build and the API test suite; it does not deploy.

Session defaults (30-minute idle and 8-hour absolute), SMTP wiring, proxy addresses, and Data Protection key persistence are provisional pending review. Production needs a durable, access-controlled Data Protection key ring.

## 3. Product behavior and worldbuilding model

The current story notes describe three nations—Lykr, Nagøn, and Zisa—and a shared world timeline spanning multiple eras. The power system is **Weaving**, with five fixed disciplines: Matter, Energy, Fields, Light, and Shadow. The wiki also organizes conduits, technology, political groups, languages and dialects, trade, laws, conflicts, locations, and characters.

Some private notes reference existing franchises as inspiration. Keep those as private inspiration references, not as a substitute for original story material. Review names, characters, text, maps, and images before any sharing or public release.

### Protected starter categories

Characters; Locations; Nations/Civilizations; Cultures; Species; Religions; Factions/Groups; Artifacts; Weaving; Languages; Events; Terms; Technologies.

These base categories cannot be deleted. The owner may customize names, layouts, and field presentation within the agreed template rules. Additional categories, fields, and subbranches can be removed; if they contain data or links, archive the data and preserve history rather than silently destroying it.

### Common lore entry features

Each entry can have a title, summary, images, rich-text body, tags, custom fields, links to other entries, and revision history. Rich content should use constrained blocks such as headings, text, image galleries, and linked entries. Support captions and alt text. Internal links should expose backlinks.

Custom field types: text, rich text, number, date, checkbox, dropdown, entry link, and image/gallery. Each custom field can independently be marked searchable, filterable, and sortable.

### Starter fields and relationships

- **Nations/Civilizations:** region, language/dialect, imports/exports, economy, entry/exit laws, dominant conduit type, groups, maps, and conflicts.
- **Factions/Groups:** type, purpose/ideology, membership, territory, leadership, alliances/opposition, and history. Political groups have their own linked entries.
- **Characters:** aliases, nation/culture, universal birth/death years, affiliations, relationships, Weaving/conduit information, and Avatar/Marked status.
- **Technologies:** type, era, creator, operation, compatible Weaving disciplines, uses/limits, and social impact.
- **Locations:** type, nation/region, map markers, population, and historical changes.
- **Languages:** shared parent languages and linked dialect entries, including Lykaya, Mønga, and Zisan.
- **Events:** universal start/end years, date certainty, optional nation-era labels, locations, people/groups, and consequences.
- **Cultures, Religions, Species, Artifacts, and Terms:** define their starter fields against the owner’s authoritative story notes before seeding. Do not invent missing story-specific fields from these documents.

### Weaving structure

The five top-level branches are fixed and cannot be removed: Matter, Energy, Fields, Light, and Shadow. Subbranches may be added or removed. Initial proposed subbranches:

- **Matter:** Earth → Metal; Water; Air; Organic matter.
- **Energy:** Heat; Motion; Chemical reactions; Nuclear energy; Pure energy.
- **Fields:** Gravity; Magnetism; Electricity; Force manipulation.
- **Light:** Illumination; Lasers; Optical illusions.
- **Shadow:** Concealment; Darkness manipulation; Shadow constructs; Supernatural shadow travel.

Verify these names and relationships against the source story notes before seeding durable data.

## 4. Architecture and domain model

### Request and deployment shape

Use one HTTPS browser origin. The Next.js server acts as a gateway to an API with internal ingress where feasible. The API is the final authorization boundary for every protected resource. The browser must not receive database credentials, blob credentials, API bearer tokens, or public blob URLs. Images and maps are returned only after authorization. Keep development, staging, and production resources separate, and use synthetic lore in non-production environments.

The database and blob storage must not be publicly readable. Prefer managed identities for service access, least-privilege roles, private service access, and Key Vault or managed-identity-backed configuration. Exact Azure products and networking remain open decisions.

### Proposed relational domain records

Use relational, typed core records and explicit typed values for extensible fields. Do not put an entire entry in an unvalidated JSON document.

| Record | Main data and rules |
|---|---|
| OwnerAccount | ID, normalized email, password hash, MFA state, and timestamps. One manually provisioned owner initially. Protect TOTP material at rest; store recovery codes as one-way hashes and consume each once. |
| Category | Stable key, display name, protected flag, and archive/deletion timestamps. Seed the 13 protected categories. Protected identity cannot be deleted. |
| CategoryTemplate | Category ID, template version, and default field definitions; supports later schema evolution. |
| FieldDefinition | Category, stable key, label, type, required flag, searchable/filterable/sortable flags, order, and options. Validate options and values by field type. |
| Entry | Category, title, summary, body/content blocks, relevant universal dates, status, and timestamps. Supports common tags, images, links, and revisions. Archive is recoverable; permanent deletion rules remain undecided. |
| FieldValue | Entry and field definition plus typed value columns or typed subtype. Enforce a value representation that matches the declared field type. |
| ContentBlock | Entry, ordinal, block type, and validated structured payload. Do not permit arbitrary executable HTML. |
| Tag / EntryTag | Normalized tag name and entry association; define normalization/uniqueness rules. |
| EntryRelationship | Source entry, target entry, relationship type, optional label/direction. Preserve referential integrity and backlinks. |
| MediaAsset | Private object key, detected type, dimensions, size, checksum, alt/caption, timestamp, and deletion state. Use random storage names and no public URL. References are separate from the asset. |
| Revision / AuditEvent | Entry revision number or snapshot/delta, actor, event type, and timestamp. Track edits, category/layout changes, archive/restore, and deletion. Never include secrets or story text in security event details. |
| UniversalDate / EventDetails | Fictional start/end year, certainty, optional era label. Keep years numeric and independent of Gregorian calendar conversion. Events remain normal lore entries. |
| MapSet / MapLayer / MapMarker | Era label, base and overlay media references, layer type, normalized coordinates, and linked Location entry. Preserve marker references; each overlay matches its base map dimensions. |
| WeavingBranch / WeavingSubbranch | Fixed branch keys/names for the five disciplines; configurable subbranches with parent relationships. |

### Search, graph, timeline, and Atlas

- **Search:** authenticated global search over titles, summaries, body, tags, and custom fields explicitly marked searchable. Filters include category, tags, and filterable fields; sortable fields and pagination are supported. Keep search local; do not send story content to an external search service. Snippets follow source-entry authorization.
- **Relationship graph:** use Sigma.js and Graphology for the graph only. Provide a focused graph from an entry and an optional full-universe view, with category and relationship filters. Selecting a node opens its entry. Graph data follows the same access controls as entries.
- **Timeline:** one shared timeline ordered by universal year. National era names are optional labels on that shared chronology, not separate timelines. Events open their lore entries; multi-year events render as bars; uncertain dates stay approximate. Support pan/zoom, keyboard navigation, and useful empty states. Filter by nation, category, and linked groups/characters where appropriate. Do not coerce fictional years into Gregorian JavaScript dates.
- **Atlas:** uploaded map images created elsewhere. Support multiple map sets by era, each with a base map and aligned uploaded overlays. Keep the base visible and show one selected overlay at a time: territory, terrain, roads/infrastructure, or population density. Validate that overlay dimensions match the base. Place markers linked to Location entries; opening a marker opens the location entry. Keep past and modern map sets separate and never modify the uploaded source image.
- **History:** Settings > History records entry edits, category/layout changes, archivals, restores, and deletions. Allow inspection, comparison, and restoration of entry revisions. Restore as a new revision; never erase intervening history.

## 5. Authentication and private access

### Required behavior

- Provision the first owner through a one-time setup path and disable that path after setup.
- Use email/password authentication with required TOTP. Provide one-time offline recovery codes; do not add SMS MFA.
- Disable public registration. Password reset uses a verified email address, short-lived single-use links, rate limits, and generic responses that do not disclose whether an address exists.
- Rate-limit login, MFA, password reset, email confirmation, and recovery-code attempts. Use progressive lockouts and alert on repeated failures.
- Use server-managed cookies only; never store session tokens in browser local storage. Cookies are Secure, HttpOnly where applicable, host-only, and SameSite=Lax or Strict as the chosen flow permits.
- Protect every state-changing request against CSRF. SameSite is additional protection, not a substitute for antiforgery validation.
- Rotate the session after login/MFA, expire sessions after inactivity and an absolute limit, revoke on credential/MFA changes, and require reauthentication for sensitive account changes.
- Enforce authorization in Next.js server routes and every API endpoint. All content, search, graph/timeline, maps, media, downloads, exports, and archived revisions require authentication.
- The unauthenticated surface is limited to login, bootstrap/recovery/CSRF flows, and non-content health checks. No public publishing mode.

### Implementation details reported by the architecture plan

These are reported as implemented in the private-access phase and should be checked against the repository before changing them:

- ASP.NET Core Identity uses PostgreSQL EF storage. Only data-protection and authenticator token providers are registered; no phone/SMS provider or registration endpoint.
- Bootstrap requires a configured one-time setup key and an empty owner table. It sends an email confirmation link before TOTP enrollment; the token is in the URL fragment. Setup closes after email confirmation and TOTP enrollment.
- `__Host-wiki-session` is Secure, HttpOnly, host-only, Path `/`, SameSite=Strict. Provisional lifetime is 30 minutes idle and 8 hours absolute.
- `__Host-wiki-mfa` is Secure, HttpOnly, host-only, Path `/`, SameSite=Strict, with a five-minute non-sliding lifetime during password-to-TOTP verification.
- `__Host-wiki-csrf` is Secure, host-only, Path `/`, SameSite=Strict, and intentionally browser-readable for the double-submit antiforgery flow. State-changing API requests require the antiforgery header.
- Identity lockout is five failures for 15 minutes plus an IP-keyed API auth limiter of 20 requests per five minutes. Reset and email confirmation tokens last 15 minutes. Security-stamp validation runs each minute; successful password changes/resets update the stamp so previous sessions expire after validation.
- Identity token-table values, including authenticator/recovery material, are encrypted through ASP.NET Data Protection with an EF value converter. A durable, access-controlled production key ring is still required.
- The Next.js gateway proxies `/api/*` and validates protected page sessions through the API session endpoint. API fallback authorization requires an authenticated owner unless an endpoint is explicitly anonymous.
- SMTP with TLS is configurable, but no provider/account is selected. Bootstrap and reset require a verified sender and working delivery configuration.
- Forwarded scheme/address headers are trusted only from loopback and configured proxy IPs. Production ingress addresses have not been configured.

## 6. Security and privacy requirements

### Threats and controls

| Threat | Required controls and verification |
|---|---|
| Account takeover, credential stuffing, reset abuse | Required TOTP, strong password hashing, one-time hashed recovery codes, throttling/lockouts, generic reset responses, session rotation/revocation, no SMS, and tests for replay and recovery paths. |
| Unauthorized lore or metadata access | Deny by default at frontend and API; internal API where feasible; authenticated media delivery; negative tests for every resource type. |
| Stored XSS and CSRF | Constrained rich-text schema, server validation/sanitization, output encoding, CSP, antiforgery tokens, SameSite cookies, and tests for malicious rich text and cross-origin mutations. |
| Malicious or identifying uploads | PNG/JPEG/WebP allowlist; check signatures as well as declared MIME; set size/dimension/pixel limits; random keys; strip EXIF; safe thumbnails; scan uploads where available; test malformed/boundary files. |
| Cloud credential compromise | Managed identity, Key Vault, least privilege, private networking, no public database/blob access, TLS/encryption, secret scans, and access review. |
| Deletion or provider failure | Database and media backups, separate encrypted copies, tested restore, and deletion/recovery runbooks. |
| Logging or processor disclosure | Minimize logs, redact secrets/request bodies, restrict access/retention, and verify no story content or credentials go to logs, analytics, AI, or external services. |
| Brute force or denial of service | Rate limits, bounded request/body sizes, upload limits, resource monitoring, and alerts. |

Baseline: HTTPS/TLS, HSTS, restrictive CSP, safe referrer/content-type headers, parameterized EF queries, input validation, output encoding, antiforgery on mutations, secure host-only cookies, private database/blob storage, and encryption in transit and at rest. Robots/noindex directives are defense in depth, never authorization. Log only minimal security metadata such as time, event type, outcome, and correlation data. Do not log passwords, TOTP/recovery/reset/session tokens, request bodies containing lore, or image contents. Minimize IP precision and retention.

### Data inventory

| Data | Purpose/location | Access and retention |
|---|---|---|
| Owner email, password hash, TOTP/recovery state | Authentication database | Owner/auth service only; account lifetime and account-deletion process require a decision. |
| Session and antiforgery identifiers | Browser cookies and server-side identity/session state | Necessary security state; define idle/absolute lifetime; revoke after logout or credential/MFA changes. |
| Lore entries, custom values, tags, links, revisions | PostgreSQL | Owner only; define archive, deletion, and history behavior. |
| Images/maps, metadata, thumbnails, markers | Private object storage plus database references | Authenticated delivery only; remove references separately from assets; strip EXIF. |
| Security/operational logs | Restricted platform logging service if selected | Minimize and redact; define retention, IP handling, and access roles; no story bodies. |
| Backups | Encrypted private backup location | Current baseline below; expiration must be reflected in deletion disclosures. |
| Reset email and single-use link | Selected transactional email provider | Used only for reset; review provider retention, region, and terms; never include lore or credentials. |

### Cookies

The proposed inventory includes an authentication/session cookie and an antiforgery cookie/token if required by the selected flow. The session cookie should be Secure, HttpOnly, host-only, and SameSite=Lax or Strict; the antiforgery cookie may be browser-readable for a double-submit flow and is not an authentication credential. Verify actual cookie names, flags, lifetimes, and browser behavior before writing a final notice. No analytics, advertising, personalization, or third-party embed cookies are planned. Do not assert that no other cookies exist without inspecting the running application.

### Processors and data flows

Provisional processor register: Microsoft Azure for compute, database, object storage, Key Vault, and possibly logging/backups; the email provider is not selected. No external AI processing, analytics, ad tracking, external search, or third-party media processing is approved. Before production, update this register to match actual services, Azure region, subprocessors, email provider terms, and data locations.

### Retention and deletion baseline

- PostgreSQL point-in-time recovery: 35 days.
- Media: private storage, 30-day soft-delete recovery, and bounded version history.
- Encrypted monthly database-and-media copy in a separate private location: 12-month retention.
- Test database and media restoration before launch and periodically afterward.
- Define separate security-log retention and permanent content-deletion semantics before production.

Archive is reversible and distinct from permanent deletion. Removing an image from an entry removes its reference, not the shared asset. Permanent entry/account/media deletion, revision/audit retention, linked-entry behavior, exports, and backup handling remain decisions. Do not promise immediate erasure from retained backups unless it is technically supported and verified. Document actual user-visible purge workflows and timelines after decisions are made.

## 7. Implementation roadmap and acceptance gates

Build in phases. Keep each change small, preserve repository conventions, and do not make infrastructure or data/security behavior decisions silently. Add focused tests with the feature and record what was actually verified. The gates below describe the expected evidence; they are not claims that the tests have already passed.

### Phase 0 — Reconcile requirements and approve the design

Inspect repository instructions, existing code, migrations, deployment files, and authoritative story notes. Reconcile starter fields, category names, Weaving subbranches, dates, and inspiration references with the source notes. Confirm the domain model, topology, retention, and open decisions. Replace the historical repository audit with an accurate current-state inventory.

**Gate:** architecture and open decisions are reviewable; legal materials contain factual placeholders only; no application change depends on an unresolved security or data-retention decision.

### Phase 1 — Foundation and design system

Preserve the existing repository. Confirm the separate Next.js/TypeScript and ASP.NET Core/C# services, PostgreSQL/EF Core/Npgsql setup, migrations, local configuration, health checks, and CI. Pin compatible versions and follow existing package-manager conventions. Build the shared shell, navigation, loading/error states, and responsive design tokens using light wood, dark green, beige, and white with accessible contrast and visible keyboard focus. Keep secrets out of source control and use placeholders in example configuration. Do not configure production deployment in this phase.

**Gate:** frontend lint/type/build checks pass; API build/tests pass; migration applies to a clean PostgreSQL database; no secrets are committed.

### Phase 2 — Authentication and private boundary

Complete or verify owner bootstrap, email/password, required TOTP, recovery codes, verified-email reset, generic responses, rate limits/lockouts, session/CSRF behavior, session revocation, and reauthentication for sensitive changes. Do not add registration or SMS. Protect every frontend route and API resource; only the explicitly named account and health flows remain anonymous. Add noindex/noarchive headers and omit a sitemap as additional defenses.

**Gate:** auth, lockout, reset, MFA, recovery-code, CSRF, and session tests pass; unauthenticated requests are denied for pages, APIs, search, graph/timeline, maps, images, downloads, exports, and archived revisions; cookie behavior is inspected.

### Phase 3 — Domain model and protected defaults

Implement migrations and models for owner, categories/templates, fields, entries/content blocks, tags, typed relationships, media references, universal dates, archives, revisions, and audit events. Seed all 13 protected categories and five Weaving branches. Add the approved starter fields. Preserve referential integrity and support schema evolution. User-created categories/fields/subbranches can be removed; if they contain data, archive the data and preserve relevant links/history. Do not build every UI feature in this phase.

**Gate:** migration/model/seed tests pass; typed field constraints and category protections work; reference integrity and archive/restore behavior are covered.

### Phase 4 — Entry editing, customization, and private media

Build authenticated category dashboards and entry pages. Let the owner customize tiles, dashboard layout, section order, and field presentation; add custom categories, fields, and Weaving subbranches; remove user-added items without deleting protected categories or fixed branches. Use TipTap with a constrained safe extension set and structured blocks. Support internal links/backlinks, tags, captions, alt text, and image galleries. Render safely and prevent stored XSS.

Build a shared private media library. Validate actual file content, allow only approved raster formats, enforce selected size/dimension/pixel limits, use random storage names, strip EXIF, generate safe thumbnails, and keep every storage/delivery path private. Removing an entry reference does not delete the asset; permanent deletion is an explicit separate action.

**Gate:** field validation, category customization, editor sanitization, upload validation, metadata stripping, and media authorization tests pass.

### Phase 5 — History, search, graph, timeline, and Atlas

Build Settings > History with review, compare, and restore-as-new-revision behavior. Preserve archives, links, and media references. Add authenticated global search with category/tag/filterable-field filters, sortable fields, pagination, and empty/loading/error states. Add graph, universal timeline, and map Atlas as specified in Section 4. Derived data, snippets, thumbnails, and overlays must follow the original entry’s authorization.

**Gate:** tests cover archived visibility, search field flags and snippets, unauthorized access, graph construction/filters, fictional-year sorting/ranges/uncertainty, map layer dimensions, selectors, markers, and location links.

### Phase 6 — Operations and launch review

Before production, choose the Azure region/topology and email provider; secure the domain; configure managed identities, Key Vault, private networking, TLS, logs, alerts, backups, and durable Data Protection keys. Verify the privacy/cookie notice against actual flows. Complete threat review, dependency/secret scans, restore drill, runbooks, rollback steps, and accessible keyboard support for editor, graph, timeline, and map selectors. Do not add users or publish content as part of this phase.

**Gate:** unauthenticated access fails across all resource types; database and media restore together; no public database/blob access; security and launch checklist has actual results; failed checks are resolved or clearly block launch.

## 8. Architecture decisions

| ID | Decision | Status |
|---|---|---|
| ADR-001 | Next.js + TypeScript UI, ASP.NET Core + C# API, PostgreSQL + EF Core/Npgsql | Agreed stack. |
| ADR-002 | Azure hosting; proposed Container Apps, PostgreSQL Flexible Server, private Blob Storage, Key Vault | Direction agreed; region, topology, and spend are open. |
| ADR-003 | Single manually provisioned owner, no registration; password + required TOTP + offline one-time recovery codes; no SMS | Agreed first-release boundary. |
| ADR-004 | Single HTTPS origin with Next.js server gateway to internal API; API enforces final authorization | Recommended; validate deployment and CSRF/session flow. |
| ADR-005 | No analytics/ad tracking or external AI processing of story content | Agreed product privacy requirement; distinct from how the owner writes the software. |
| ADR-006 | Protected starter categories and five Weaving branches; typed extensible fields; universal fictional-year timeline | Agreed direction; reconcile story-specific seed data with source notes. |
| ADR-007 | Author Studio, themes, and subplots deferred | Agreed scope boundary. |
| ADR-008 | PostgreSQL PITR 35 days; media soft delete 30 days; separate encrypted monthly copy for 12 months | Planning baseline; confirm cost and test restore. |

## 9. Open-decision register

Recommendations below are for review, not resolved facts or legal conclusions.

| ID | Decision | Recommendation / needed by |
|---|---|---|
| O-01 | Owner jurisdiction and any future users | Use the owner’s actual jurisdiction as the initial review baseline; obtain qualified review before final notices/deployment. |
| O-02 | Azure region | Choose based on jurisdiction, latency, availability, provider terms, and cost; decide before provisioning. |
| O-03 | Monthly budget ceiling | Include compute, database, storage, backups, logs, egress, domain, and email; price always-on and low-cost/paused profiles before selecting SKUs. |
| O-04 | Domain/DNS | Choose owner-controlled registrar/DNS account, renewal, and recovery controls before production. |
| O-05 | Email delivery | Select provider/account and sender domain; review delivery, region, retention, security, and terms; use authenticated sending (SPF/DKIM/DMARC), not marketing lists. |
| O-06 | Session lifetime | Current provisional defaults are 30-minute idle and 8-hour absolute, non-persistent. Confirm lifetimes and reauthentication threshold; retain revocation on credential/MFA changes. |
| O-07 | Log retention | Set security/platform retention, IP precision, access roles, and alerts. Keep only minimal security metadata for a documented period; never log lore. |
| O-08 | Upload limits | Choose per-file bytes, dimensions/pixel count, approved formats, and total storage budget. Proposed launch formats are PNG/JPEG/WebP; choose bounds based on map sizes and cost. |
| O-09 | Permanent deletion | Decide entry/account/media purge, history/audit retention, links, exports, backup expiration, and restore behavior. Separate reversible archive from explicit confirmed purge. |
| O-10 | Story-note reconciliation | Identify authoritative story notes and confirm fields, names, dates, and inspiration references before seed/migration. |
| O-11 | Exact Azure topology | Confirm Container Apps/internal ingress, private database networking, storage access, secrets, logs, and backup products; no public DB/blob. |
| O-12 | Cookie/session implementation and notice | Verify implemented Identity and antiforgery cookies in a browser. Reported cookies use Secure/host-only/Path `/`/SameSite=Strict; session is HttpOnly and CSRF cookie readable. Final notice must match observed behavior. |
| O-13 | Export and account recovery | Choose owner export format and recovery if TOTP device and all recovery codes are lost. Use documented identity-verified recovery with an audit event; avoid hidden bypass codes. |

### Legal and content-rights gate

Prepare factual placeholders until implementation confirms data collected, processors, region, cookies, retention/deletion, contact route, and recovery behavior. Final notices require jurisdiction-specific review. Do not claim legal compliance or make definitive legal claims based only on this plan. Require uploaded maps/images to be owned by the owner or licensed for their intended use. Keep source/rights notes for reference materials; do not import copyrighted passages or third-party images without permission. Before public release, review private franchise references including Avatar/ATLA, Black Panther, and LOTR and replace them with original content where needed.

## 10. Backup, incident response, and release checklist

### Before production

- Confirm owner jurisdiction, Azure region, budget ceiling, domain/DNS, and email provider.
- Confirm session lifetime, log retention, upload limits, permanent deletion, export, and account recovery decisions.
- Confirm actual cookie inventory and write factual privacy/cookie text for review.
- Complete threat review for credential theft, unauthorized crawlers, private-media leakage, malicious uploads, CSRF, stored XSS, cloud credential compromise, and accidental deletion.
- Verify denial of unauthenticated access for every page, API, search result, graph/timeline source, image, map layer, archive, and export. Confirm no indexing or public blob access.
- Run dependency/secret scans and security tests for MFA, lockouts, password reset, CSRF, rich-text rendering, uploads, authorization, and restore workflows.
- Confirm restore points and perform a database-and-media recovery exercise.
- Prepare incident runbook for session revocation, credential rotation, account recovery, and data restoration.
- Review accessibility and keyboard support for the editor, graph, timeline, and map selectors.
- Keep the site private. Do not enable additional accounts or public content.

### Backup baseline

Use PostgreSQL point-in-time recovery for 35 days; private media with 30-day soft-delete recovery and bounded version history; encrypted monthly database/media copies in a separate private location for 12 months. Test restoration before launch and periodically afterward. Define log retention separately. History/archives and disaster-recovery backups are separate controls.

### Launch evidence

Keep actual build/test/security results, deployment and rollback instructions, restore steps, privacy/cookie facts, and unresolved decisions with the release record. Do not call the service launch-ready until the phase 6 gate passes. No production deployment is authorized by this guide.

## 11. Reference materials

- OWASP Session Management Cheat Sheet.
- OWASP CSRF Prevention Cheat Sheet.
- EU ePrivacy Directive, Article 5(3).
- California Attorney General: CCPA overview.
- Azure PostgreSQL backup and restore documentation.
- Azure Blob soft delete and versioning documentation.

Confirm current applicability and implementation details against authoritative sources before launch.
