const base = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

export function resolveImageUrl(path?: string | null): string | undefined {
  if (!path) return undefined
  if (path.startsWith('http://') || path.startsWith('https://')) return path
  return `${base}${path.startsWith('/') ? path : `/${path}`}`
}
