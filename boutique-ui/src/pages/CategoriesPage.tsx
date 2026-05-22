import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { IconEdit, IconTrash } from '@tabler/icons-react'
import { TopBar } from '../components/layout/TopBar'
import { Modal } from '../components/ui/Modal'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { categoryService } from '../services/categoryService'
import { getErrorMessage } from '../lib/errors'
import type { Category } from '../types'

/** Categories management page: list, create, rename, and delete product categories. */
export function CategoriesPage() {
  const qc = useQueryClient()
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [editing, setEditing] = useState<Category | null>(null)
  const [editName, setEditName] = useState('')
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const { data } = useQuery({ queryKey: ['categories'], queryFn: categoryService.getAll })

  const create = useMutation({
    mutationFn: () => categoryService.create({ name: name.trim() }),
    onSuccess: () => {
      toast.success('Category created')
      qc.invalidateQueries({ queryKey: ['categories'] })
      setName('')
      setCreating(false)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const update = useMutation({
    mutationFn: () => categoryService.update(editing!.id, { name: editName.trim() }),
    onSuccess: () => {
      toast.success('Category updated')
      qc.invalidateQueries({ queryKey: ['categories'] })
      setEditing(null)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const remove = useMutation({
    mutationFn: (id: string) => categoryService.delete(id),
    onSuccess: () => {
      toast.success('Category deleted')
      qc.invalidateQueries({ queryKey: ['categories'] })
      setDeleteId(null)
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  return (
    <>
      <TopBar
        title="Categories"
        action={
          <button type="button" className="btn-primary" onClick={() => setCreating(true)}>
            New category
          </button>
        }
      />

      {creating && (
        <form
          className="card mb-6 flex flex-wrap gap-2"
          onSubmit={(e) => {
            e.preventDefault()
            if (name.trim()) create.mutate()
          }}
        >
          <input
            className="input-field min-w-[200px] flex-1"
            placeholder="Category name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            autoFocus
          />
          <button type="submit" className="btn-primary" disabled={create.isPending}>
            Save
          </button>
          <button type="button" className="btn-ghost" onClick={() => setCreating(false)}>
            Cancel
          </button>
        </form>
      )}

      <section
        className="grid gap-4"
        style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))' }}
      >
        {data?.map((c) => (
          <article key={c.id} className="card relative">
            <span className="absolute right-3 top-3 flex gap-1">
              <button
                type="button"
                className="p-1"
                onClick={() => {
                  setEditing(c)
                  setEditName(c.name)
                }}
                title="Rename"
              >
                <IconEdit size={16} className="text-[var(--text-muted)]" />
              </button>
              <button
                type="button"
                className="p-1 disabled:opacity-30"
                disabled={c.productCount > 0}
                onClick={() => setDeleteId(c.id)}
                title={c.productCount > 0 ? 'Category in use' : 'Delete'}
              >
                <IconTrash size={16} className="text-[var(--text-muted)]" />
              </button>
            </span>
            <h2 className="page-title m-0 pr-16 text-xl">{c.name}</h2>
            <p className="mt-2 text-xs text-[var(--text-muted)]">{c.productCount} products</p>
          </article>
        ))}
      </section>

      <Modal open={Boolean(editing)} onClose={() => setEditing(null)} title="Rename category">
        <form
          onSubmit={(e) => {
            e.preventDefault()
            if (editName.trim()) update.mutate()
          }}
          className="space-y-4"
        >
          <input
            className="input-field"
            value={editName}
            onChange={(e) => setEditName(e.target.value)}
            autoFocus
          />
          <button type="submit" className="btn-primary w-full" disabled={update.isPending}>
            {update.isPending ? 'Saving…' : 'Save'}
          </button>
        </form>
      </Modal>

      <ConfirmDialog
        open={Boolean(deleteId)}
        title="Delete category"
        message="This category will be permanently removed."
        confirmLabel="Delete"
        loading={remove.isPending}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && remove.mutate(deleteId)}
      />
    </>
  )
}
