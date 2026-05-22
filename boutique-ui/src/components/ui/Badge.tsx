import type { ReactNode } from 'react'

type BadgeVariant = 'ok' | 'warn' | 'danger' | 'inactive' | 'info'

const styles: Record<BadgeVariant, { bg: string; text: string }> = {
  ok: { bg: 'var(--ok-bg)', text: 'var(--ok-text)' },
  warn: { bg: 'var(--warn-bg)', text: 'var(--warn-text)' },
  danger: { bg: 'var(--danger-bg)', text: 'var(--danger-text)' },
  inactive: { bg: 'var(--inactive-bg)', text: 'var(--inactive-text)' },
  info: { bg: 'var(--info-bg)', text: 'var(--info-text)' },
}

interface BadgeProps {
  variant: BadgeVariant
  children: ReactNode
  className?: string
}

export function Badge({ variant, children, className = '' }: BadgeProps) {
  const { bg, text } = styles[variant]
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${className}`}
      style={{ background: bg, color: text }}
    >
      {children}
    </span>
  )
}
