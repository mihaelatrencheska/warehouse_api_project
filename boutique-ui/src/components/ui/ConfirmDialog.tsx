import { Modal } from './Modal'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  onConfirm: () => void
  onClose: () => void
  loading?: boolean
}

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Confirm',
  onConfirm,
  onClose,
  loading,
}: ConfirmDialogProps) {
  return (
    <Modal open={open} onClose={onClose} title={title} size="sm">
      <p className="m-0 text-sm text-[var(--text-muted)]">{message}</p>
      <div className="mt-6 flex justify-end gap-2">
        <button type="button" className="btn-ghost" onClick={onClose} disabled={loading}>
          Cancel
        </button>
        <button
          type="button"
          className="btn-primary"
          style={{ background: 'var(--danger-text)' }}
          onClick={onConfirm}
          disabled={loading}
        >
          {confirmLabel}
        </button>
      </div>
    </Modal>
  )
}
