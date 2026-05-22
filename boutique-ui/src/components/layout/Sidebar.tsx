import { NavLink } from 'react-router-dom'
import {
  IconBuildingWarehouse,
  IconCategory,
  IconDroplet,
  IconHome,
  IconPackage,
  IconPhotoSearch,
} from '@tabler/icons-react'

const links = [
  { to: '/', label: 'Dashboard', icon: IconHome, short: 'Home', end: true },
  { to: '/warehouses', label: 'Warehouses', icon: IconBuildingWarehouse, short: 'Warehouses', end: false },
  { to: '/products', label: 'Products', icon: IconPackage, short: 'Products', end: true },
  { to: '/products/search-by-image', label: 'Image search', icon: IconPhotoSearch, short: 'Image', end: true },
  { to: '/categories', label: 'Categories', icon: IconCategory, short: 'Categories', end: false },
  { to: '/alerts', label: 'Alerts', icon: IconDroplet, short: 'Alerts', end: false },
]

const navClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors ${
    isActive ? 'font-medium' : 'opacity-75 hover:opacity-100'
  }`

export function Sidebar() {
  return (
    <>
      <aside
        className="fixed left-0 top-0 hidden h-screen w-[220px] flex-col md:flex"
        style={{ background: 'var(--charcoal)', color: 'var(--cream)', zIndex: 30 }}
      >
        <header className="border-b border-white/10 px-5 py-6">
          <p className="brand m-0 text-2xl leading-none">
            boutique<span style={{ color: 'var(--gold)' }}>iq</span>
          </p>
          <p className="m-0 mt-1 text-xs opacity-60">inventory · warehouse</p>
        </header>
        <nav className="flex flex-1 flex-col gap-1 p-3">
          {links.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={navClass}
              style={({ isActive }) => (isActive ? { background: 'rgba(201, 169, 110, 0.15)', color: 'var(--gold)' } : {})}
            >
              <Icon size={20} stroke={1.5} />
              {label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <nav
        className="fixed bottom-0 left-0 right-0 z-40 flex border-t border-[var(--cream-border)] bg-white md:hidden"
        style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
      >
        {links.slice(0, 5).map(({ to, short, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className="flex flex-1 flex-col items-center gap-0.5 py-2 text-[10px]"
            style={({ isActive }) => ({ color: isActive ? 'var(--gold)' : 'var(--text-muted)' })}
          >
            <Icon size={22} stroke={1.5} />
            {short}
          </NavLink>
        ))}
      </nav>
    </>
  )
}
