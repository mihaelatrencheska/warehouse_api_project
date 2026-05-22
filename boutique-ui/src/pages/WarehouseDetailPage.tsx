import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { format, parseISO } from 'date-fns'
import { IconEdit, IconTrash } from '@tabler/icons-react'
import { TopBar } from '../components/layout/TopBar'
import { Modal } from '../components/ui/Modal'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { Badge } from '../components/ui/Badge'
import { warehouseService } from '../services/warehouseService'
import { getErrorMessage } from '../lib/errors'
import type { WarehouseSection } from '../types'

const sectionSchema = z.object({ name: z.string().min(1).max(80) })

/** Warehouse detail page: shows warehouse info, its sections, and the products stored within. */
export function WarehouseDetailPage() {
  const { id = '' } = useParams()
  const qc = useQueryClient()
  const [sectionModal, setSectionModal] = useState(false)
  const [renameSection, setRenameSection] = useState<WarehouseSection | null>(null)
  const [deleteSection, setDeleteSection] = useState<WarehouseSection | null>(null)
  const form = useForm<{ name: string }>({ resolver: zodResolver(sectionSchema) })
  const renameForm = useForm<{ name: string }>({ resolver: zodResolver(sectionSchema) })

  const warehouse = useQuery({
    queryKey: ['warehouse', id],
    queryFn: () => warehouseService.getById(id),
    enabled: Boolean(id),
  })

  const products = useQuery({
    queryKey: ['warehouse-products', id],
    queryFn: () => warehouseService.getProducts(id),
    enabled: Boolean(id),
  })

  const addSection = useMutation({
    mutationFn: (name: string) => warehouseService.createSection(id, { name }),
    onSuccess: () => {
      toast.success('Section added')
      qc.invalidateQueries({ queryKey: ['warehouse', id] })
      setSectionModal(false)
      form.reset()
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const rename = useMutation({
    mutationFn: ({ sectionId, name }: { sectionId: string; name: string }) =>
      warehouseService.updateSection(id, sectionId, { name }),
    onSuccess: () => {
      toast.success('Section renamed')
      qc.invalidateQueries({ queryKey: ['warehouse', id] })
      setRenameSection(null)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const removeSection = useMutation({
    mutationFn: (sectionId: string) => warehouseService.deleteSection(id, sectionId),
    onSuccess: () => {
      toast.success('Section removed')
      qc.invalidateQueries({ queryKey: ['warehouse', id] })
      setDeleteSection(null)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const toggleActive = useMutation({
    mutationFn: (wasActive: boolean) =>
      wasActive ? warehouseService.deactivate(id) : warehouseService.reactivate(id),
    onSuccess: (_data, wasActive) => {
      toast.success(wasActive ? 'Warehouse deactivated' : 'Warehouse reactivated')
      qc.invalidateQueries({ queryKey: ['warehouse', id] })
      qc.invalidateQueries({ queryKey: ['warehouses'] })
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const w = warehouse.data

  return (
    <>
      <p className="mb-4 text-sm text-[var(--text-muted)]">
        <Link to="/warehouses" style={{ color: 'var(--gold)' }}>
          Warehouses
        </Link>
        {' → '}
        <span>{w?.name ?? '…'}</span>
      </p>

      <TopBar
        title={w?.name ?? 'Warehouse'}
        action={
          <span className="flex gap-2">
            {w && (
              <button
                type="button"
                className="btn-ghost"
                disabled={toggleActive.isPending}
                onClick={() => w && toggleActive.mutate(w.isActive)}
              >
                {w.isActive ? 'Deactivate' : 'Reactivate'}
              </button>
            )}
            <button type="button" className="btn-primary" onClick={() => setSectionModal(true)}>
              Add section
            </button>
          </span>
        }
      />

      {w && (
        <article className="card mb-6">
          {w.location && <p className="m-0 text-[var(--text-muted)]">{w.location}</p>}
          <span className="mt-3 flex flex-wrap gap-3 text-sm">
            <Badge variant={w.isActive ? 'ok' : 'inactive'}>{w.isActive ? 'Active' : 'Inactive'}</Badge>
            <span className="text-[var(--text-muted)]">
              Created {format(parseISO(w.createdAt), 'MMM d, yyyy')}
            </span>
            <span className="text-[var(--text-muted)]">{w.productCount} products</span>
          </span>
        </article>
      )}

      <section className="grid gap-4 md:grid-cols-2">
        {w?.sections?.map((section) => {
          const sectionProducts = products.data?.filter((p) => p.sectionId === section.id) ?? []
          return (
            <article key={section.id} className="card">
              <span className="flex items-start justify-between gap-2">
                <h3 className="page-title m-0 text-xl">{section.name}</h3>
                <span className="flex shrink-0 gap-1">
                  <button
                    type="button"
                    className="p-1"
                    title="Rename section"
                    onClick={() => {
                      setRenameSection(section)
                      renameForm.reset({ name: section.name })
                    }}
                  >
                    <IconEdit size={16} className="text-[var(--text-muted)]" />
                  </button>
                  <button
                    type="button"
                    className="p-1 disabled:opacity-30"
                    disabled={section.productCount > 0}
                    title={section.productCount > 0 ? 'Section has products' : 'Delete section'}
                    onClick={() => setDeleteSection(section)}
                  >
                    <IconTrash size={16} className="text-[var(--text-muted)]" />
                  </button>
                </span>
              </span>
              <p className="text-xs text-[var(--text-muted)]">{section.productCount} products</p>
              <ul className="mt-3 list-none space-y-2 p-0">
                {sectionProducts.slice(0, 5).map((p) => (
                  <li key={p.id} className="text-sm">
                    <Link to="/products" className="no-underline" style={{ color: 'var(--text-primary)' }}>
                      {p.name}
                    </Link>
                  </li>
                ))}
              </ul>
              {sectionProducts.length > 5 && (
                <Link to="/products" className="mt-2 inline-block text-sm" style={{ color: 'var(--gold)' }}>
                  + {sectionProducts.length - 5} more
                </Link>
              )}
              <Link
                to="/products"
                className="btn-ghost mt-4 inline-block text-sm no-underline"
              >
                Add product to section
              </Link>
            </article>
          )
        })}
      </section>

      <Modal open={sectionModal} onClose={() => setSectionModal(false)} title="Add section">
        <form onSubmit={form.handleSubmit((v) => addSection.mutate(v.name))} className="space-y-4">
          <label className="block text-sm">
            Section name *
            <input className="input-field mt-1" {...form.register('name')} />
          </label>
          <button type="submit" className="btn-primary w-full" disabled={addSection.isPending}>
            Add section
          </button>
        </form>
      </Modal>

      <Modal open={Boolean(renameSection)} onClose={() => setRenameSection(null)} title="Rename section">
        <form
          onSubmit={renameForm.handleSubmit((v) =>
            renameSection && rename.mutate({ sectionId: renameSection.id, name: v.name }),
          )}
          className="space-y-4"
        >
          <input className="input-field" {...renameForm.register('name')} />
          <button type="submit" className="btn-primary w-full" disabled={rename.isPending}>
            Save
          </button>
        </form>
      </Modal>

      <ConfirmDialog
        open={Boolean(deleteSection)}
        title="Delete section"
        message={`Remove "${deleteSection?.name}"? Only empty sections can be deleted.`}
        confirmLabel="Delete"
        loading={removeSection.isPending}
        onClose={() => setDeleteSection(null)}
        onConfirm={() => deleteSection && removeSection.mutate(deleteSection.id)}
      />
    </>
  )
}
