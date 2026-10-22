import Link from "next/link";

export default function NotFound() {
  return <main className="state-page"><div className="loading-mark" aria-hidden="true">?</div><h1>That page isn’t here</h1><p>It may have moved, or this view has not been created yet.</p><Link className="button" href="/">Return to overview</Link></main>;
}
