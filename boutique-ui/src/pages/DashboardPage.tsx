import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  IconBuildingWarehouse,
  IconClock,
  IconDroplet,
  IconShirt,
  IconCircleX,
  IconSearch,
} from '@tabler/icons-react'
import { TopBar } from '../components/layout/TopBar'
import { StatCard } from '../components/ui/StatCard'
import { Badge } from '../components/ui/Badge'
import { warehouseService } from '../services/warehouseService'
import { productService } from '../services/productService'
import { formatExpiryDate, getDaysUntilExpiration, getExpiryStatus } from '../lib/expiry'

/** Dashboard overview: warehouse count, product stats, expiring items, and recent products. */
export function DashboardPage() {
  const warehouses = useQuery({ queryKey: ['warehouses'], queryFn: () => warehouseService.getAll() })
  const browse = useQuery({ queryKey: ['products', 'browse', 1], queryFn: () => productService.browse(1, 10) })
  const expiring = useQuery({
    queryKey: ['products', 'expiring', 30],
    queryFn: () => productService.getExpiring(30),
  })
  const expiredCountQuery = useQuery({
    queryKey: ['products', 'expired-count'],
    queryFn: () => productService.countExpired(),
  })

  const activeWarehouses = warehouses.data?.filter((w) => w.isActive).length ?? 0
  const totalProducts = browse.data?.totalCount ?? 0
  const expiringCount = expiring.data?.length ?? 0
  const expiredCount = expiredCountQuery.data ?? 0

  return (
  <>
      <TopBar
        title="Dashboard"
        action={
          <Link to="/products" className="btn-primary no-underline">
            Add product
          </Link>
        }
      />

      {expiringCount > 0 && (
        <Link
          to="/alerts"
          className="mb-6 flex items-center justify-between rounded-lg px-4 py-3 no-underline"
          style={{ background: 'var(--warn-bg)', color: 'var(--warn-text)' }}
        >
          <span className="text-sm font-medium">
            {expiringCount} product{expiringCount === 1 ? '' : 's'} expiring within 30 days
          </span>
          <span className="text-sm">View alerts →</span>
        </Link>
      )}

      <section className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard icon={<IconBuildingWarehouse size={20} />} value={activeWarehouses} label="Active warehouses" variant="blue" />
        <StatCard icon={<IconShirt size={20} />} value={totalProducts} label="Total products" variant="teal" />
        <StatCard icon={<IconClock size={20} />} value={expiringCount} label="Expiring ≤30 days" variant="amber" />
        <StatCard icon={<IconCircleX size={20} />} value={expiredCount} label="Expired products" variant="red" />
      </section>

      <section className="mb-6 grid gap-6 lg:grid-cols-2">
        <article className="card">
          <header className="mb-4 flex items-center justify-between">
            <h2 className="page-title m-0 text-xl">Warehouses</h2>
            <Link to="/warehouses" className="text-sm no-underline" style={{ color: 'var(--gold)' }}>
              Manage all →
            </Link>
          </header>
          <ul className="m-0 list-none space-y-3 p-0">
            {warehouses.data?.slice(0, 5).map((w) => (
              <li key={w.id} className="flex items-center justify-between gap-2 border-b border-[var(--cream-border)] pb-3 last:border-0">
                <span className="flex items-center gap-2">
                  <span
                    className="h-2 w-2 rounded-full"
                    style={{ background: w.isActive ? '#3b6d11' : 'var(--text-faint)' }}
                  />
                  <span>
                    <p className="m-0 font-medium">{w.name}</p>
                    <p className="m-0 text-xs text-[var(--text-muted)]">{w.sectionCount} sections</p>
                  </span>
                </span>
                <Badge variant={w.isActive ? 'ok' : 'inactive'}>{w.isActive ? 'Active' : 'Inactive'}</Badge>
              </li>
            ))}
          </ul>
        </article>

        <article className="card">
          <header className="mb-4 flex items-center justify-between">
            <h2 className="page-title m-0 text-xl">Expiring soon</h2>
            <Link to="/alerts" className="text-sm no-underline" style={{ color: 'var(--gold)' }}>
              All alerts →
            </Link>
          </header>
          <ul className="m-0 list-none space-y-3 p-0">
            {expiring.data?.slice(0, 6).map((p) => {
              const days = getDaysUntilExpiration(p.expirationDate)
              const variant = days !== null && days <= 7 ? 'danger' : 'warn'
              return (
                <li key={p.id} className="flex items-center gap-3 border-b border-[var(--cream-border)] pb-3 last:border-0">
                  <IconDroplet size={18} style={{ color: 'var(--warn-text)' }} />
                  <span className="min-w-0 flex-1">
                    <p className="m-0 truncate font-medium">{p.name}</p>
                    <p className="m-0 truncate text-xs text-[var(--text-muted)]">
                      {p.warehouseName} · {p.sectionName}
                    </p>
                  </span>
                  <Badge variant={variant}>{days ?? '—'}d</Badge>
                </li>
              )
            })}
            {!expiring.data?.length && (
              <p className="m-0 text-sm text-[var(--text-muted)]">No products expiring within 30 days.</p>
            )}
          </ul>
        </article>
      </section>

      <article className="card">
        <Link
          to="/products"
          className="mb-4 flex items-center gap-2 rounded-lg border border-[var(--cream-border)] px-4 py-2.5 no-underline"
          style={{ background: 'var(--cream-hover)' }}
        >
          <IconSearch size={18} className="text-[var(--text-faint)]" />
          <span className="text-sm text-[var(--text-faint)]">Search products by name, SKU, category…</span>
        </Link>
        <h2 className="page-title mb-4 mt-0 text-xl">Recent products</h2>
        <ul className="m-0 list-none space-y-2 p-0">
          {browse.data?.items.map((p, idx) => {
            const status = getExpiryStatus(p.expirationDate)
            const badge =
              status === 'expired' ? 'danger' : status === 'expiring' ? 'warn' : status === 'ok' ? 'ok' : 'inactive'
            return (
              <li
                key={p.id}
                className="flex flex-wrap items-center gap-3 rounded-lg px-2 py-2 hover:bg-[var(--cream-hover)]"
              >
                <span
                  className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-xs text-[var(--text-muted)]"
                  style={{ background: 'var(--cream-hover)' }}
                >
                  {idx + 1}
                </span>
                <span className="min-w-0 flex-1">
                  <p className="m-0 font-medium">{p.name}</p>
                  <p className="m-0 text-xs text-[var(--text-muted)]">
                    {p.warehouseName} · {p.sectionName}
                    {p.size ? ` · ${p.size}` : ''}
                  </p>
                </span>
                <Badge variant={badge}>
                  {status === 'none' ? 'No expiry' : formatExpiryDate(p.expirationDate)}
                </Badge>
              </li>
            )
          })}
        </ul>
      </article>
    </>
  )
}
