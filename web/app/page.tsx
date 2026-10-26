import Link from "next/link";
import { LogoutButton } from "./auth/logout-button";

const sections = ["Characters", "Locations", "Nations & Civilizations", "Cultures", "Species", "Religions", "Factions & Groups", "Artifacts", "Weaving", "Languages", "Events", "Terms", "Technologies"];

export default function HomePage() {
  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Main navigation">
        <Link className="brand" href="/" aria-label="Worldbuilding Wiki home"><span className="brand-mark" aria-hidden="true">W</span><span>Worldbuilding<br />Wiki</span></Link>
        <p className="nav-label">YOUR WORLD</p>
        <nav><Link className="nav-link active" href="/" aria-current="page"><span aria-hidden="true">⌂</span> Overview</Link>
          <p className="nav-label nav-label-spaced">CATEGORIES</p>
          {sections.map((section) => <a className="nav-link" href="#categories" key={section}><span className="nav-dot" aria-hidden="true" />{section}</a>)}
        </nav>
        <div className="sidebar-bottom"><span className="status-dot" /> Private workspace</div>
      </aside>
      <main className="main-content">
        <header className="topbar"><div className="breadcrumbs">Workspace <span>/</span> Overview</div><div className="owner-chip"><span className="avatar">O</span><span>Owner</span><LogoutButton /></div></header>
        <section className="welcome"><p className="eyebrow">YOUR STORY, ORGANIZED</p><h1>A home for your world.</h1><p className="welcome-copy">Gather the people, places, histories, and ideas that make your story yours.</p></section>
        <section className="stats" aria-label="Workspace summary"><article className="stat-card"><span className="stat-icon">◫</span><div><strong>13</strong><span>Starter categories</span></div></article><article className="stat-card"><span className="stat-icon">⌘</span><div><strong>1</strong><span>Shared timeline</span></div></article><article className="stat-card"><span className="stat-icon">◇</span><div><strong>Private</strong><span>Only you can access</span></div></article></section>
        <section className="category-section" id="categories"><div className="section-heading"><div><p className="eyebrow">THE BUILDING BLOCKS</p><h2>Explore your world</h2></div><p className="section-note">Your categories will appear here as your wiki takes shape.</p></div>
          <div className="category-grid">{sections.map((section, index) => <article className="category-card" key={section}><span className="category-number">{String(index + 1).padStart(2, "0")}</span><h3>{section}</h3><p>Entries and connections for your {section.toLowerCase()}.</p><span className="card-arrow" aria-hidden="true">↗</span></article>)}</div>
        </section>
        <footer className="footer-note">A quiet place to build something only you know.</footer>
      </main>
    </div>
  );
}
