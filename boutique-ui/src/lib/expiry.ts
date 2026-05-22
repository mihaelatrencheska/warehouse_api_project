import { differenceInCalendarDays, format, parseISO } from 'date-fns'

export type ExpiryStatus = 'ok' | 'expiring' | 'expired' | 'none'

export function getDaysUntilExpiration(date?: string | null): number | null {
  if (!date) return null
  return differenceInCalendarDays(parseISO(date), new Date())
}

export function getExpiryStatus(date?: string | null): ExpiryStatus {
  const days = getDaysUntilExpiration(date)
  if (days === null) return 'none'
  if (days < 0) return 'expired'
  if (days <= 30) return 'expiring'
  return 'ok'
}

export function formatExpiryDate(date?: string | null): string {
  if (!date) return '—'
  return format(parseISO(date), 'MMM d, yyyy')
}

export function expiryLabel(days: number | null): string {
  if (days === null) return 'No expiry'
  if (days < 0) return `Expired ${Math.abs(days)} day${Math.abs(days) === 1 ? '' : 's'} ago`
  if (days === 0) return 'Expires today'
  return `${days} day${days === 1 ? '' : 's'} remaining`
}
