import { IconSearch } from '@tabler/icons-react'

interface SearchBarProps {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  onSubmit?: () => void
}

export function SearchBar({ value, onChange, placeholder = 'Search…', onSubmit }: SearchBarProps) {
  return (
    <form
      className="relative flex-1"
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit?.()
      }}
    >
      <IconSearch size={18} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-faint)]" />
      <input
        type="search"
        className="input-field"
        style={{ paddingLeft: '2.25rem' }}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
      />
    </form>
  )
}
