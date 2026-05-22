export function SkeletonRow() {
  const c = 'px-3 py-3'
  const s = 'skeleton-shimmer rounded'
  return (
    <tr className="border-b border-[var(--cream-border)]">
      <td className={c}><span className={`${s} block h-10 w-10 rounded-lg`} /></td>
      <td className={c}>
        <span className={`${s} mb-2 block h-4 w-32`} />
        <span className={`${s} block h-3 w-20`} />
      </td>
      <td className={c}><span className={`${s} block h-5 w-16 rounded-full`} /></td>
      <td className={c}><span className={`${s} block h-4 w-12`} /></td>
      <td className={c}><span className={`${s} block h-4 w-12`} /></td>
      <td className={c}><span className={`${s} block h-4 w-20`} /></td>
      <td className={c}><span className={`${s} block h-5 w-14 rounded-full`} /></td>
      <td className={c}><span className={`${s} block h-6 w-12`} /></td>
    </tr>
  )
}
