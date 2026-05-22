import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'

export function Layout() {
  return (
    <div className="flex min-h-screen w-full">
      <Sidebar />
      <main
        className="min-h-screen w-full pb-20 md:ml-[220px] md:pb-0"
        style={{ background: 'var(--cream)' }}
      >
        <div className="p-6">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
