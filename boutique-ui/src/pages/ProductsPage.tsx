import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useDropzone } from 'react-dropzone'
import { toast } from 'sonner'
import { IconEdit, IconTrash, IconPhoto } from '@tabler/icons-react'
import { TopBar } from '../components/layout/TopBar'
import { SearchBar } from '../components/ui/SearchBar'
import { Modal } from '../components/ui/Modal'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { Badge } from '../components/ui/Badge'
import { EmptyState } from '../components/ui/EmptyState'
import { SkeletonRow } from '../components/ui/SkeletonRow'
import { productService } from '../services/productService'
import { warehouseService } from '../services/warehouseService'
import { categoryService } from '../services/categoryService'
import { getErrorMessage } from '../lib/errors'
import { formatExpiryDate, getExpiryStatus } from '../lib/expiry'
import { resolveImageUrl } from '../lib/media'
import type { Product, ProductSearchParams, ProductSummary } from '../types'

const productSchema = z.object({
  name: z.string().min(1).max(150),
  sku: z.string().min(1).max(60),
  description: z.string().max(1000).optional(),
  size: z.string().max(40).optional(),
  type: z.string().max(80).optional(),
  expirationDate: z.string().optional(),
  warehouseId: z.string().min(1),
  warehouseSectionId: z.string().min(1),
  categoryIds: z.array(z.string()),
})

type ProductFormValues = z.infer<typeof productSchema>

function statusBadge(date?: string) {
  const s = getExpiryStatus(date)
  if (s === 'expired') return <Badge variant="danger">Expired</Badge>
  if (s === 'expiring') return <Badge variant="warn">Expiring</Badge>
  if (s === 'ok') return <Badge variant="ok">OK</Badge>
  return <Badge variant="inactive">No expiry</Badge>
}

/** Product listing page with search, filter, pagination, and full CRUD modal. */
export function ProductsPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [warehouseId, setWarehouseId] = useState('')
  const [sectionId, setSectionId] = useState('')
  const [size, setSize] = useState('')
  const [type, setType] = useState('')
  const [expiringDays, setExpiringDays] = useState('')
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Product | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<ProductSummary | null>(null)
  const [tab, setTab] = useState<'details' | 'location' | 'categories' | 'image'>('details')
  const [imageFile, setImageFile] = useState<File | null>(null)

  const expiringWithinDays = useMemo(() => {
    if (!expiringDays.trim()) return undefined
    const n = Number(expiringDays)
    return Number.isFinite(n) && n > 0 ? n : undefined
  }, [expiringDays])

  const filterWarehouse = useQuery({
    queryKey: ['warehouse', warehouseId],
    queryFn: () => warehouseService.getById(warehouseId),
    enabled: Boolean(warehouseId),
  })
  const filterSections = filterWarehouse.data?.sections ?? []

  const params: ProductSearchParams = {
    query: query || undefined,
    categoryId: categoryId || undefined,
    warehouseId: warehouseId || undefined,
    sectionId: sectionId || undefined,
    size: size || undefined,
    type: type || undefined,
    expiringWithinDays,
    page,
    pageSize: 10,
  }

  const products = useQuery({
    queryKey: ['products', params],
    queryFn: () => productService.getAll(params),
  })

  const warehouses = useQuery({ queryKey: ['warehouses'], queryFn: () => warehouseService.getAll() })
  const categories = useQuery({ queryKey: ['categories'], queryFn: categoryService.getAll })

  const form = useForm<ProductFormValues>({
    resolver: zodResolver(productSchema),
    defaultValues: { categoryIds: [], warehouseId: '', warehouseSectionId: '' },
  })
  const { errors } = form.formState

  const selectedWarehouseId = form.watch('warehouseId')
  const warehouseDetail = useQuery({
    queryKey: ['warehouse', selectedWarehouseId],
    queryFn: () => warehouseService.getById(selectedWarehouseId),
    enabled: Boolean(selectedWarehouseId),
  })

  const sections = warehouseDetail.data?.sections ?? []

  const openCreate = () => {
    setEditing(null)
    setTab('details')
    setImageFile(null)
    form.reset({ name: '', sku: '', categoryIds: [], warehouseId: '', warehouseSectionId: '' })
    setModalOpen(true)
  }

  const openEdit = async (id: string) => {
    try {
      const p = await productService.getById(id)
      setEditing(p)
      setTab('details')
      setImageFile(null)
      form.reset({
        name: p.name,
        sku: p.sku,
        description: p.description ?? '',
        size: p.size ?? '',
        type: p.type ?? '',
        expirationDate: p.expirationDate?.slice(0, 10) ?? '',
        warehouseId: p.location?.warehouseId ?? '',
        warehouseSectionId: p.location?.sectionId ?? p.sectionId ?? '',
        categoryIds: p.categories.map((c) => c.id),
      })
      setModalOpen(true)
    } catch (e) {
      toast.error(getErrorMessage(e))
    }
  }

  const save = useMutation({
    mutationFn: async (values: ProductFormValues) => {
      const payload = {
        name: values.name,
        sku: values.sku,
        description: values.description || undefined,
        size: values.size || undefined,
        type: values.type || undefined,
        expirationDate: values.expirationDate
          ? `${values.expirationDate}T12:00:00.000Z`
          : undefined,
        warehouseSectionId: values.warehouseSectionId,
        categoryIds: values.categoryIds,
      }
      let product: Product
      if (editing) {
        product = await productService.update(editing.id, payload)
      } else {
        product = await productService.create(payload)
      }
      if (imageFile) {
        product = await productService.uploadImage(product.id, imageFile)
      }
      return product
    },
    onSuccess: () => {
      toast.success(editing ? 'Product updated' : 'Product created')
      qc.invalidateQueries({ queryKey: ['products'] })
      setModalOpen(false)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const remove = useMutation({
    mutationFn: (id: string) => productService.delete(id),
    onSuccess: () => {
      toast.success('Product deleted')
      qc.invalidateQueries({ queryKey: ['products'] })
      setDeleteTarget(null)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const { getRootProps, getInputProps } = useDropzone({
    onDrop: (f) => setImageFile(f[0] ?? null),
    maxFiles: 1,
    accept: { 'image/*': [] },
  })

  const totalPages = products.data?.totalPages ?? 1

  const clearFilters = () => {
    setQuery('')
    setCategoryId('')
    setWarehouseId('')
    setSectionId('')
    setSize('')
    setType('')
    setExpiringDays('')
    setPage(1)
  }

  const tabs = useMemo(
    () =>
      [
        ['details', 'Details'],
        ['location', 'Location'],
        ['categories', 'Categories'],
        ['image', 'Image'],
      ] as const,
    [],
  )

  return (
    <>
      <TopBar title="Products" action={<button type="button" className="btn-primary" onClick={openCreate}>Add product</button>} />

      <section className="card mb-6 flex flex-wrap gap-3">
        <SearchBar value={query} onChange={setQuery} placeholder="Search name, SKU…" />
        <select className="input-field w-auto min-w-[140px]" value={categoryId} onChange={(e) => { setCategoryId(e.target.value); setPage(1) }}>
          <option value="">All categories</option>
          {categories.data?.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <select
          className="input-field w-auto min-w-[140px]"
          value={warehouseId}
          onChange={(e) => {
            setWarehouseId(e.target.value)
            setSectionId('')
            setPage(1)
          }}
        >
          <option value="">All warehouses</option>
          {warehouses.data?.map((w) => (
            <option key={w.id} value={w.id}>{w.name}</option>
          ))}
        </select>
        <select
          className="input-field w-auto min-w-[140px]"
          value={sectionId}
          disabled={!warehouseId}
          title={!warehouseId ? 'Select a warehouse first' : undefined}
          onChange={(e) => { setSectionId(e.target.value); setPage(1) }}
        >
          <option value="">All sections</option>
          {filterSections.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>
        <input className="input-field w-24" placeholder="Size" value={size} onChange={(e) => { setSize(e.target.value); setPage(1) }} />
        <input className="input-field w-28" placeholder="Type" value={type} onChange={(e) => { setType(e.target.value); setPage(1) }} />
        <input className="input-field w-28" type="number" min={1} placeholder="Expiring days" value={expiringDays} onChange={(e) => { setExpiringDays(e.target.value); setPage(1) }} />
        <button type="button" className="btn-ghost" onClick={clearFilters}>Clear</button>
      </section>

      <section className="card overflow-x-auto p-0">
        <table className="product-table w-full min-w-[900px] border-collapse text-left text-sm">
          <thead>
            <tr className="border-b border-[var(--cream-border)] text-[var(--text-muted)]">
              <th className="w-12 py-3 text-center font-medium text-[var(--text-muted)]">#</th>
              <th className="px-3 py-3 font-medium">Product</th>
              <th className="px-3 py-3 font-medium">Categories</th>
              <th className="px-3 py-3 font-medium">Size</th>
              <th className="px-3 py-3 font-medium">Type</th>
              <th className="px-3 py-3 font-medium">Location</th>
              <th className="px-3 py-3 font-medium">Expires</th>
              <th className="px-3 py-3 font-medium">Status</th>
              <th className="px-3 py-3 font-medium"> </th>
            </tr>
          </thead>
          <tbody>
            {products.isLoading &&
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} />)}
            {!products.isLoading && !products.data?.items.length && (
              <tr>
                <td colSpan={9}>
                  <EmptyState icon={<IconPhoto size={40} />} title="No products" description="Try adjusting filters or add a new product." />
                </td>
              </tr>
            )}
            {products.data?.items.map((p, idx) => {
              const rowNumber = (page - 1) * 10 + idx + 1
              const expStatus = getExpiryStatus(p.expirationDate)
              const expClass =
                expStatus === 'expired'
                  ? 'var(--danger-text)'
                  : expStatus === 'expiring'
                    ? 'var(--warn-text)'
                    : 'var(--text-muted)'
              return (
                <tr
                  key={p.id}
                  className="border-b border-[var(--cream-border)] hover:bg-[var(--cream-hover)]"
                  style={{ borderLeft: '3px solid var(--gold-muted)' }}
                >
                  <td className="w-12 px-0 py-3 text-center text-xs text-[var(--text-muted)]">
                    {rowNumber}
                  </td>
                  <td className="px-3 py-3">
                    <p className="m-0 font-medium">{p.name}</p>
                    <p className="m-0 text-xs text-[var(--text-muted)]">{p.sku}</p>
                  </td>
                  <td className="px-3 py-3 text-xs">{p.categoryNames ?? '—'}</td>
                  <td className="px-3 py-3">{p.size ?? '—'}</td>
                  <td className="px-3 py-3">{p.type ?? '—'}</td>
                  <td className="px-3 py-3 text-xs">{p.warehouseName} · {p.sectionName}</td>
                  <td className="px-3 py-3" style={{ color: expClass }}>{formatExpiryDate(p.expirationDate)}</td>
                  <td className="px-3 py-3">{statusBadge(p.expirationDate)}</td>
                  <td className="px-3 py-3">
                    <span className="flex gap-1">
                      <button type="button" className="btn-ghost p-2" onClick={() => openEdit(p.id)} aria-label="Edit"><IconEdit size={16} /></button>
                      <button type="button" className="btn-ghost p-2" onClick={() => setDeleteTarget(p)} aria-label="Delete"><IconTrash size={16} /></button>
                    </span>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </section>

      {totalPages > 1 && (
        <span className="mt-4 flex items-center justify-center gap-3">
          <button type="button" className="btn-ghost" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
          <span className="text-sm text-[var(--text-muted)]">Page {page} of {totalPages}</span>
          <button type="button" className="btn-ghost" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next</button>
        </span>
      )}

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editing ? 'Edit product' : 'Add product'} size="lg">
        <span className="mb-4 flex gap-2 border-b border-[var(--cream-border)] pb-2">
          {tabs.map(([key, label]) => (
            <button
              key={key}
              type="button"
              className="rounded px-3 py-1 text-sm"
              style={tab === key ? { background: 'var(--charcoal)', color: 'var(--cream)' } : { color: 'var(--text-muted)' }}
              onClick={() => setTab(key)}
            >
              {label}
            </button>
          ))}
        </span>

        <form
          onSubmit={form.handleSubmit(
            (v) => save.mutate(v),
            (fieldErrors) => {
              const hasDetailError = fieldErrors.name || fieldErrors.sku || fieldErrors.description || fieldErrors.size || fieldErrors.type || fieldErrors.expirationDate
              const hasLocationError = fieldErrors.warehouseId || fieldErrors.warehouseSectionId
              if (hasDetailError) setTab('details')
              else if (hasLocationError) setTab('location')
            },
          )}
          className="space-y-4"
        >
          {tab === 'details' && (
            <>
              <label className="block text-sm">
                Name *
                <input className="input-field mt-1" {...form.register('name')} />
                {errors.name && <span className="mt-1 block text-xs text-[var(--danger-text)]">{errors.name.message}</span>}
              </label>
              <label className="block text-sm">
                SKU *
                <input className="input-field mt-1" {...form.register('sku')} />
                {errors.sku && <span className="mt-1 block text-xs text-[var(--danger-text)]">{errors.sku.message}</span>}
              </label>
              <label className="block text-sm">Description<textarea className="input-field mt-1 min-h-[80px]" {...form.register('description')} /></label>
              <span className="grid grid-cols-2 gap-3">
                <label className="block text-sm">Size<input className="input-field mt-1" {...form.register('size')} /></label>
                <label className="block text-sm">Type<input className="input-field mt-1" {...form.register('type')} /></label>
              </span>
              <label className="block text-sm">Expiration<input type="date" className="input-field mt-1" {...form.register('expirationDate')} /></label>
            </>
          )}

          {tab === 'location' && (
            <>
              <label className="block text-sm">
                Warehouse *
                <select className="input-field mt-1" {...form.register('warehouseId')} onChange={(e) => { form.setValue('warehouseId', e.target.value); form.setValue('warehouseSectionId', '') }}>
                  <option value="">Select warehouse</option>
                  {warehouses.data
                    ?.filter((w) => w.isActive || w.id === form.watch('warehouseId'))
                    .map((w) => (
                      <option key={w.id} value={w.id}>{w.name}{!w.isActive ? ' (inactive)' : ''}</option>
                    ))}
                </select>
                {errors.warehouseId && <span className="mt-1 block text-xs text-[var(--danger-text)]">Required</span>}
              </label>
              <label className="block text-sm">
                Section *
                <select className="input-field mt-1" {...form.register('warehouseSectionId')} disabled={!selectedWarehouseId}>
                  <option value="">Select section</option>
                  {sections.map((s) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select>
                {errors.warehouseSectionId && <span className="mt-1 block text-xs text-[var(--danger-text)]">Required</span>}
              </label>
            </>
          )}

          {tab === 'categories' && (
            <fieldset className="m-0 space-y-2 border-0 p-0">
              {categories.data?.map((c) => (
                <label key={c.id} className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    value={c.id}
                    checked={form.watch('categoryIds').includes(c.id)}
                    onChange={(e) => {
                      const cur = form.getValues('categoryIds')
                      form.setValue(
                        'categoryIds',
                        e.target.checked ? [...cur, c.id] : cur.filter((id) => id !== c.id),
                      )
                    }}
                  />
                  {c.name}
                </label>
              ))}
            </fieldset>
          )}

          {tab === 'image' && (
            <div className="space-y-4">
              {editing?.imageUrl && !imageFile && (() => {
                const src = resolveImageUrl(editing.imageUrl)
                return src ? (
                  <div>
                    <p className="mb-2 text-xs text-[var(--text-muted)]">Current image</p>
                    <img
                      src={src}
                      alt="Current product image"
                      style={{ width: 80, height: 80, objectFit: 'cover', borderRadius: 8, display: 'block' }}
                    />
                  </div>
                ) : null
              })()}
              <section {...getRootProps()} className="cursor-pointer rounded-lg border border-dashed border-[var(--cream-border)] p-8 text-center" style={{ background: 'var(--cream)' }}>
                <input {...getInputProps()} />
                <IconPhoto size={32} className="mx-auto text-[var(--text-faint)]" />
                <p className="mt-2 text-sm text-[var(--text-muted)]">{imageFile ? imageFile.name : 'Drop image or click to browse'}</p>
              </section>
            </div>
          )}

          <button type="submit" className="btn-primary w-full" disabled={save.isPending}>
            {save.isPending ? 'Saving…' : editing ? 'Update product' : 'Create product'}
          </button>
        </form>
      </Modal>

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title="Delete product"
        message={`Delete "${deleteTarget?.name}"? This cannot be undone.`}
        confirmLabel="Delete"
        loading={remove.isPending}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget && remove.mutate(deleteTarget.id)}
      />
    </>
  )
}
