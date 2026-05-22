import { Link } from 'react-router-dom'

/** 404 fallback page shown when no route matches. */
export function NotFoundPage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-6 text-center" style={{ background: 'var(--cream)' }}>
      <p className="page-title m-0 text-8xl text-[var(--text-faint)]">404</p>
      <h1 className="page-title mt-4 text-2xl">This page doesn&apos;t exist</h1>
      <Link to="/" className="btn-primary mt-8 inline-block no-underline">
        Back to dashboard
      </Link>
    </main>
  )
}
