import { isAxiosError } from 'axios'

export function getErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
  if (!isAxiosError(error)) {
    return error instanceof Error ? error.message : fallback
  }

  const data = error.response?.data as
    | { detail?: string; title?: string; message?: string; errors?: Record<string, string[]> }
    | undefined

  if (data?.detail) return data.detail
  if (data?.message) return data.message
  if (data?.errors) {
    const first = Object.values(data.errors).flat()[0]
    if (first) return first
  }
  if (data?.title) return data.title
  return fallback
}
