import { NavLink } from 'react-router-dom'

const NAV_ITEMS = [
  { to: '/',             label: 'Dashboard' },
  { to: '/transactions', label: 'Transactions' },
  { to: '/categories',   label: 'Categories' },
  { to: '/budgets',      label: 'Budgets' },
  { to: '/reports',      label: 'Reports' },
]

export function Sidebar() {
  return (
    <aside className="w-64 bg-gray-900 text-white h-[100dvh] flex flex-col p-4">
      <h1 className="text-xl font-bold mb-8">Finance Tracker</h1>
      <nav className="flex flex-col gap-1">
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                isActive ? 'bg-indigo-600 text-white' : 'text-gray-300 hover:bg-gray-700'
              }`
            }
          >
            {item.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}