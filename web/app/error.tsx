"use client";

export default function ErrorState({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return <main className="state-page" role="alert"><div className="loading-mark" aria-hidden="true">!</div><h1>This page needs a moment</h1><p>Something went wrong while opening this view. Your notes have not been changed.</p><button className="button" onClick={() => reset()}>Try again</button></main>;
}
