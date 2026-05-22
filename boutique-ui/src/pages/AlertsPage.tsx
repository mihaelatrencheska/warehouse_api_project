import { useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { IconDroplet, IconCircleCheck } from '@tabler/icons-react'
import { TopBar } from '../components/layout/TopBar'
import { EmptyState } from '../components/ui/EmptyState'
import { alertService } from '../services/alertService'
import { productService } from '../services/productService'
import { getAccessToken } from '../lib/api'
import { getErrorMessage } from '../lib/errors'
import { expiryLabel, formatExpiryDate } from '../lib/expiry'
import type { Alert, ExpirationAlertNotification, ProductSummary } from '../types'

const RECEIVE_ALERT = 'ReceiveAlert'

function notificationToAlert(payload: ExpirationAlertNotification): Alert {
  return {
    id: payload.alertId,
    productId: payload.productId,
    productName: payload.productName,
    productSku: payload.sku,
    expirationDate: payload.expirationDate,
    alertDate: payload.alertDate,
    daysUntilExpiration: payload.daysUntilExpiration,
    isAcknowledged: false,
  }
}

function AlertSection({
  title,
  color,
  items,
  onAcknowledge,
  acknowledgingId,
}: {
  title: string
  color: string
  items: { alert: Alert; product?: ProductSummary }[]
  onAcknowledge: (id: string) => void
  acknowledgingId: string | null
}) {
  if (!items.length) return null
  return (
    <section className="mb-8">
      <h2 className="page-title m-0 mb-4 text-xl" style={{ color }}>
        {title}
      </h2>
      <ul className="card m-0 list-none space-y-0 p-0">
        {items.map(({ alert, product }) => (
          <li
            key={alert.id}
            className="flex flex-wrap items-center gap-3 border-b border-[var(--cream-border)] px-1 py-3 last:border-0"
          >
            <IconDroplet size={20} style={{ color }} />
            <span className="min-w-0 flex-1">
              <p className="m-0 font-medium">{alert.productName}</p>
              <p className="m-0 text-xs text-[var(--text-muted)]">
                {formatExpiryDate(alert.expirationDate)}
                {product && ` · ${product.warehouseName} · ${product.sectionName}`}
              </p>
            </span>
            <span className="text-sm" style={{ color }}>
              {expiryLabel(alert.daysUntilExpiration)}
            </span>
            <button
              type="button"
              className="btn-ghost text-sm"
              disabled={acknowledgingId === alert.id}
              onClick={() => onAcknowledge(alert.id)}
            >
              {acknowledgingId === alert.id ? 'Dismissing…' : 'Dismiss'}
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}

/** Alerts page: real-time expiration alerts via SignalR with acknowledge and manual scan actions. */
export function AlertsPage() {
  const qc = useQueryClient()
  const alerts = useQuery({ queryKey: ['alerts'], queryFn: alertService.getAlerts })
  const expiring = useQuery({
    queryKey: ['products', 'expiring', 30],
    queryFn: () => productService.getExpiring(30),
  })

  useEffect(() => {
    const token = getAccessToken()
    if (!token) return

    const hubBase = import.meta.env.VITE_API_URL || ''
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubBase}/hubs/alerts`, {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build()

    const onReceiveAlert = (payload: ExpirationAlertNotification) => {
      const alert = notificationToAlert(payload)
      qc.setQueryData<Alert[]>(['alerts'], (current) => {
        if (!current?.some((a) => a.id === alert.id)) {
          return [alert, ...(current ?? [])]
        }
        return current
      })
      const location =
        payload.warehouseName && payload.sectionName
          ? ` · ${payload.warehouseName} · ${payload.sectionName}`
          : ''
      toast.warning(`New alert: ${payload.productName}${location}`)
    }

    connection.on(RECEIVE_ALERT, onReceiveAlert)

    connection
      .start()
      .catch(() => {
        /* hub optional when API notifications disabled */
      })

    return () => {
      connection.stop()
    }
  }, [qc])

  const scan = useMutation({
    mutationFn: () => alertService.scan(30),
    onSuccess: (r) => {
      toast.success(`Scan complete — ${r.created} new alert(s)`)
      qc.invalidateQueries({ queryKey: ['alerts'] })
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const acknowledge = useMutation({
    mutationFn: (id: string) => alertService.acknowledge(id),
    onSuccess: () => {
      toast.success('Alert dismissed')
      qc.invalidateQueries({ queryKey: ['alerts'] })
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const productMap = new Map(expiring.data?.map((p) => [p.id, p]))

  const enriched =
    alerts.data?.map((a) => ({ alert: a, product: productMap.get(a.productId) })) ?? []

  const expired = enriched.filter((x) => x.alert.daysUntilExpiration < 0)
  const critical = enriched.filter(
    (x) => x.alert.daysUntilExpiration >= 0 && x.alert.daysUntilExpiration <= 7,
  )
  const soon = enriched.filter(
    (x) => x.alert.daysUntilExpiration > 7 && x.alert.daysUntilExpiration <= 30,
  )

  const hasAny = expired.length + critical.length + soon.length > 0
  const acknowledgingId = acknowledge.isPending ? (acknowledge.variables ?? null) : null

  return (
    <>
      <TopBar
        title="Alerts"
        action={
          <button type="button" className="btn-primary" onClick={() => scan.mutate()} disabled={scan.isPending}>
            Scan now
          </button>
        }
      />

      {!hasAny && !alerts.isLoading && (
        <EmptyState
          icon={<IconCircleCheck size={48} />}
          title="All clear"
          description="All products are within safe date ranges."
        />
      )}

      <AlertSection
        title="Expired"
        color="var(--danger-text)"
        items={expired}
        onAcknowledge={(id) => acknowledge.mutate(id)}
        acknowledgingId={acknowledgingId}
      />
      <AlertSection
        title="Critical — within 7 days"
        color="var(--warn-text)"
        items={critical}
        onAcknowledge={(id) => acknowledge.mutate(id)}
        acknowledgingId={acknowledgingId}
      />
      <AlertSection
        title="Expiring soon — within 30 days"
        color="var(--warn-text)"
        items={soon}
        onAcknowledge={(id) => acknowledge.mutate(id)}
        acknowledgingId={acknowledgingId}
      />
    </>
  )
}
