import { api } from '../lib/api'
import type { Alert } from '../types'

/** Alert service: fetch unacknowledged expiration alerts, acknowledge them, and trigger manual scans. */
export const alertService = {
  /** Returns all unacknowledged expiration alerts ordered by urgency. */
  async getAlerts(): Promise<Alert[]> {
    const { data } = await api.get<Alert[]>('/api/alerts')
    return data
  },

  /** Marks a single alert as acknowledged so it no longer appears in the active list. */
  async acknowledge(id: string): Promise<void> {
    await api.patch(`/api/alerts/${id}/acknowledge`)
  },

  /** Triggers an on-demand expiration scan and returns the number of new alerts created. */
  async scan(withinDays = 30): Promise<{ created: number; windowDays: number }> {
    const { data } = await api.post<{ created: number; windowDays: number }>('/api/alerts/scan', null, {
      params: { withinDays },
    })
    return data
  },
}
