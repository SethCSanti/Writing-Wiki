# Worldbuilding Wiki

A private, login-protected wiki for organizing the world, characters, history, and lore behind an original story. The wiki is the primary product: it is designed to make a large fictional universe easy to document, connect, search, and maintain. The Author Studio is a possible later phase and is outside the initial scope.

## Project goals

- Keep worldbuilding information in one structured, searchable place.
- Link characters, nations, groups, locations, technologies, and events so related lore is easy to follow.
- Make the wiki flexible to the author while protecting its essential starter structure.
- Keep the complete site and its content private behind authentication.

## AI-driven development

This is an AI-driven development project built through an iterative workflow with Codex. AI assistance is used to turn the author's requirements into architecture, data models, implementation tasks, documentation, and verification plans. The author directs the product, supplies the story material, and approves its worldbuilding decisions. The project is also intended to demonstrate practical AI-assisted software development for a résumé; the writing wiki remains its primary purpose.

## Planned technology

The architecture is planned around:

- **Frontend:** Next.js and TypeScript
- **Backend:** ASP.NET Core and C#
- **Data:** PostgreSQL with Entity Framework Core
- **Hosting direction:** Microsoft Azure

These are planned choices. This documentation describes the intended design; it does not claim that the application has already been implemented or deployed.

## Planned wiki capabilities

- Protected default categories for characters, locations, nations and civilizations, cultures, species, religions, factions and groups, artifacts, Weaving, languages, events, terms, and technologies.
- Customizable dashboards, tiles, sections, fields, and add-on categories, while preserving the protected base categories.
- Rich lore entries with images, tags, custom fields, entry links, and revision history.
- Search across entry content and supported custom fields.
- A relationship graph connecting lore entries.
- One universal-year timeline. Events link to their lore entries; national eras can appear as optional labels.
- An atlas for maps made elsewhere and uploaded to the wiki, with era and map-type selectors, aligned image overlays, and location markers.
- Settings history for entry changes, archives, and restores.
- Private access with email/password login and required TOTP multi-factor authentication.

## Worldbuilding model

The current story notes describe three nations—Lykr, Nagøn, and Zisa—and a shared world timeline spanning multiple eras. The power system is called **Weaving**, with five fixed disciplines: Matter, Energy, Fields, Light, and Shadow. Subbranches can be added or removed. Entries will also organize conduits, technology, political groups, languages and dialects, trade, laws, conflicts, and characters.

The wiki is meant to hold and connect the author's original worldbuilding notes. Some notes reference existing franchises as inspiration; those references are not a substitute for original story content and should be reviewed before any public release.

## Privacy and security direction

The initial release is intended for one owner account, with no public registration. All pages, APIs, search results, timeline and graph data, maps, images, downloads, and archives must require authentication. Search-engine directives are only an extra precaution; authorization is the privacy boundary. No analytics, advertising trackers, or external AI processing are planned for the initial release.

Security requirements, cookie and privacy planning, backup targets, and release gates are recorded in the [pre-build architecture and security plan](Worldbuilding-Wiki-Prebuild-Architecture-Plan.md). This README and the plan are not legal advice or a claim of legal compliance.

## Documentation

- [Pre-build architecture and security plan](Worldbuilding-Wiki-Prebuild-Architecture-Plan.md)
- [Codex implementation prompt batch](Worldbuilding-Wiki-Codex-Prompt-Batch.md)

## Project status

**Planning phase.** Architecture and implementation prompts are documented. Application implementation should begin after the pre-build decisions and security gate in the architecture plan are reviewed. The Author Studio, including themes and subplots, is deferred until the wiki is complete and working.
