import { useCallback, useState } from 'react'
import { useDropzone } from 'react-dropzone'
import { useMutation } from '@tanstack/react-query'
import { IconDroplet, IconPhoto } from '@tabler/icons-react'
import { TopBar } from '../components/layout/TopBar'
import { Badge } from '../components/ui/Badge'
import { productService } from '../services/productService'
import { getErrorMessage } from '../lib/errors'
import { resolveImageUrl } from '../lib/media'
import { toast } from 'sonner'

/** Image search page: drop or select a product photo to find visually similar items via perceptual hashing. */
export function ImageSearchPage() {
  const [preview, setPreview] = useState<string | null>(null)

  const { mutate: searchByImage, isPending, data: matches } = useMutation({
    mutationFn: (file: File) => productService.searchByImage(file),
    onError: (e) => toast.error(getErrorMessage(e)),
  })

  const onDrop = useCallback(
    (files: File[]) => {
      const file = files[0]
      if (!file) return
      setPreview(URL.createObjectURL(file))
      searchByImage(file)
    },
    [searchByImage],
  )

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'image/*': ['.jpg', '.jpeg', '.png', '.webp', '.gif'] },
    maxFiles: 1,
  })

  return (
    <>
      <TopBar title="Image search" />

      <section
        {...getRootProps()}
        className="card mx-auto mb-8 max-w-2xl cursor-pointer border-dashed text-center"
        style={{
          borderWidth: '2px',
          background: isDragActive ? 'var(--cream-hover)' : 'var(--cream)',
        }}
      >
        <input {...getInputProps()} />
        <IconDroplet size={48} className="mx-auto mb-3" style={{ color: 'var(--gold)' }} />
        <p className="page-title m-0 text-xl">Drop a product image to find matches</p>
        <p className="mt-2 text-sm text-[var(--text-muted)]">JPG, PNG, WebP or GIF</p>
      </section>

      {preview && (
        <figure className="mx-auto mb-8 max-w-xs text-center">
          <img src={preview} alt="Query" className="mx-auto max-h-48 rounded-xl object-contain" />
        </figure>
      )}

      {isPending && <p className="text-center text-sm text-[var(--text-muted)]">Searching…</p>}

      {matches && (
        <section className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {matches.map((p) => {
            const img = resolveImageUrl(p.imageUrl)
            return (
              <article key={p.id} className="card">
                {img ? (
                  <img src={img} alt="" className="mb-3 h-32 w-full rounded-lg object-cover" />
                ) : (
                  <span
                    className="mb-3 flex h-32 items-center justify-center rounded-lg"
                    style={{ background: 'var(--cream-hover)' }}
                  >
                    <IconPhoto size={32} className="text-[var(--text-faint)]" />
                  </span>
                )}
                <h3 className="page-title m-0 text-lg">{p.name}</h3>
                <p className="text-xs text-[var(--text-muted)]">
                  {p.warehouseName} · {p.sectionName}
                </p>
                <Badge variant={p.hammingDistance === 0 ? 'ok' : p.hammingDistance <= 3 ? 'ok' : 'warn'} className="mt-2">
                  {p.hammingDistance === 0 ? 'Exact match' : `${p.hammingDistance} bits apart`}
                </Badge>
              </article>
            )
          })}
          {matches.length === 0 && (
            <p className="col-span-full text-center text-[var(--text-muted)]">No similar products found.</p>
          )}
        </section>
      )}
    </>
  )
}
