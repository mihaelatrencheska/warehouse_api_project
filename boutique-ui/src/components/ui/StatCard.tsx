import type { ReactNode } from 'react'

type StatVariant = 'blue' | 'teal' | 'amber' | 'red'

const iconBg: Record<StatVariant, string> = {
  blue: 'var(--info-bg)',
  teal: '#e0f4f0',
  amber: 'var(--warn-bg)',
  red: 'var(--danger-bg)',
}

const iconColor: Record<StatVariant, string> = {
  blue: 'var(--info-text)',
  teal: '#0d6b5c',
  amber: 'var(--warn-text)',
  red: 'var(--danger-text)',
}

interface StatCardProps {
  icon: ReactNode
  value: number | string
  label: string
  variant: StatVariant
}

export function StatCard({ icon, value, label, variant }: StatCardProps) {
  return (
    <div
      className="flex items-center gap-4 rounded-xl border border-[var(--cream-border)] bg-white px-5 py-[18px]"
      style={{ borderWidth: '0.5px' }}
    >
      <div
        className="flex h-[38px] w-[38px] shrink-0 items-center justify-center rounded-lg"
        style={{ background: iconBg[variant], color: iconColor[variant] }}
      >
        {icon}
      </div>
      <div className="min-w-0 text-left">
        <p className="stat-number m-0 text-3xl leading-none text-[var(--text-primary)]">{value}</p>
        <p className="m-0 mt-1 text-sm text-[var(--text-muted)]">{label}</p>
      </div>
    </div>
  )
}
