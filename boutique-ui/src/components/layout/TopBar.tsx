import type { ReactNode } from 'react'
import { authService } from '../../services/authService'
import { IconLogout } from '@tabler/icons-react'

interface TopBarProps {
  title: string
  action?: ReactNode
}

export function TopBar({ title, action }: TopBarProps) {
  return (
    <header className="mb-6 flex flex-wrap items-center justify-between gap-4">
      <h1 className="page-title m-0 text-3xl text-[var(--text-primary)]">{title}</h1>
      <span className="flex items-center gap-3">
        {action}
        <button
          type="button"
          className="btn-ghost flex items-center gap-2"
          onClick={() => authService.logout()}
          title="Sign out"
        >
          <IconLogout size={18} />
          <span className="hidden sm:inline">Sign out</span>
        </button>
      </span>
    </header>
  )
}
