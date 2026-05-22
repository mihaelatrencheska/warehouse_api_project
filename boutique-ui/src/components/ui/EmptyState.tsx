import type { ReactNode } from 'react'

interface EmptyStateProps {
  icon: ReactNode
  title: string
  description: string
  action?: ReactNode
}

export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <div className="mb-4 text-[var(--text-faint)]">{icon}</div>
      <h3 className="page-title m-0 text-xl text-[var(--text-primary)]">{title}</h3>
      <p className="mt-2 max-w-sm text-sm text-[var(--text-muted)]">{description}</p>
      {action && <div className="mt-6">{action}</div>}
    </div>
  )
}
