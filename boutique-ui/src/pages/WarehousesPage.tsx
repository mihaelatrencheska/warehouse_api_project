import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { IconEdit } from '@tabler/icons-react'
import { TopBar } from '../components/layout/TopBar'
import { Modal } from '../components/ui/Modal'
import { Badge } from '../components/ui/Badge'
import { warehouseService } from '../services/warehouseService'
import { getErrorMessage } from '../lib/errors'
import type { Warehouse } from '../types'

const schema = z.object({
  name: z.string().min(1, 'Name is required').max(120),
  location: z.string().max(250).optional(),
})

type FormValues = z.infer<typeof schema>
type Filter = 'all' | 'active' | 'inactive'

/** Warehouses list page: view all warehouses, create new ones, edit, deactivate, or reactivate. */
export function WarehousesPage() {
  const qc = useQueryClient()
  const [filter, setFilter] = useState<Filter>('all')
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Warehouse | null>(null)

  const isActive = filter === 'all' ? undefined : filter === 'active'
  const { data, isLoading } = useQuery({
    queryKey: ['warehouses', filter],
    queryFn: () => warehouseService.getAll(isActive),
  })

  const form = useForm<FormValues>({ resolver: zodResolver(schema) })
  const editForm = useForm<FormValues>({ resolver: zodResolver(schema) })

  const create = useMutation({
    mutationFn: warehouseService.create,
    onSuccess: () => {
      toast.success('Warehouse created')
      qc.invalidateQueries({ queryKey: ['warehouses'] })
      setModalOpen(false)
      form.reset()
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const update = useMutation({
    mutationFn: ({ id, values }: { id: string; values: FormValues }) =>
      warehouseService.update(id, { name: values.name, location: values.location || undefined }),
    onSuccess: () => {
      toast.success('Warehouse updated')
      qc.invalidateQueries({ queryKey: ['warehouses'] })
      setEditing(null)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const toggleActive = useMutation({
    mutationFn: async ({ id, active }: { id: string; active: boolean }) =>
      active ? warehouseService.deactivate(id) : warehouseService.reactivate(id),
    onSuccess: () => {
      toast.success('Warehouse updated')
      qc.invalidateQueries({ queryKey: ['warehouses'] })
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const openEdit = (w: Warehouse) => {
    setEditing(w)
    editForm.reset({ name: w.name, location: w.location ?? '' })
  }

  const pills: { key: Filter; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'active', label: 'Active' },
    { key: 'inactive', label: 'Inactive' },
  ]

  return (
    <>
      <TopBar
        title="Warehouses"
        action={
          <button type="button" className="btn-primary" onClick={() => setModalOpen(true)}>
            New warehouse
          </button>
        }
      />

      <span className="mb-6 inline-flex gap-2 rounded-full border border-[var(--cream-border)] bg-white p-1">
        {pills.map((p) => (
          <button
            key={p.key}
            type="button"
            className="rounded-full px-4 py-1.5 text-sm"
            style={
              filter === p.key
                ? { background: 'var(--charcoal)', color: 'var(--cream)' }
                : { color: 'var(--text-muted)' }
            }
            onClick={() => setFilter(p.key)}
          >
            {p.label}
          </button>
        ))}
      </span>

      {isLoading && <p className="text-sm text-[var(--text-muted)]">Loading…</p>}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {data?.map((w) => (
          <article key={w.id} className="card flex flex-col">
            <Link to={`/warehouses/${w.id}`} className="no-underline" style={{ color: 'inherit' }}>
              <h2 className="page-title m-0 text-2xl">{w.name}</h2>
              {w.location && <p className="mt-1 text-sm text-[var(--text-muted)]">{w.location}</p>}
            </Link>
            <span className="mt-4 flex flex-wrap gap-2">
              <span className="rounded-full bg-[var(--cream-hover)] px-2.5 py-0.5 text-xs">
                {w.sectionCount} sections
              </span>
              <span className="rounded-full bg-[var(--cream-hover)] px-2.5 py-0.5 text-xs">
                {w.productCount} products
              </span>
              <Badge variant={w.isActive ? 'ok' : 'inactive'}>{w.isActive ? 'Active' : 'Inactive'}</Badge>
            </span>
            <span className="mt-auto flex flex-wrap gap-2 pt-4">
              <Link to={`/warehouses/${w.id}`} className="btn-ghost text-sm no-underline">
                View
              </Link>
              <button type="button" className="btn-ghost text-sm" onClick={() => openEdit(w)}>
                <IconEdit size={16} className="mr-1 inline" />
                Edit
              </button>
              <button
                type="button"
                className="btn-ghost text-sm"
                onClick={() => toggleActive.mutate({ id: w.id, active: w.isActive })}
              >
                {w.isActive ? 'Deactivate' : 'Reactivate'}
              </button>
            </span>
          </article>
        ))}
      </section>

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="New warehouse">
        <form
          onSubmit={form.handleSubmit((v) =>
            create.mutate({ name: v.name, location: v.location || undefined }),
          )}
          className="space-y-4"
        >
          <label className="block text-sm">
            Name *
            <input className="input-field mt-1" {...form.register('name')} />
          </label>
          <label className="block text-sm">
            Location / description
            <input className="input-field mt-1" {...form.register('location')} />
          </label>
          <button type="submit" className="btn-primary w-full" disabled={create.isPending}>
            Create
          </button>
        </form>
      </Modal>

      <Modal open={Boolean(editing)} onClose={() => setEditing(null)} title="Edit warehouse">
        <form
          onSubmit={editForm.handleSubmit((v) =>
            editing && update.mutate({ id: editing.id, values: v }),
          )}
          className="space-y-4"
        >
          <label className="block text-sm">
            Name *
            <input className="input-field mt-1" {...editForm.register('name')} />
          </label>
          <label className="block text-sm">
            Location / description
            <input className="input-field mt-1" {...editForm.register('location')} />
          </label>
          <button type="submit" className="btn-primary w-full" disabled={update.isPending}>
            {update.isPending ? 'Saving…' : 'Save changes'}
          </button>
        </form>
      </Modal>
    </>
  )
}
