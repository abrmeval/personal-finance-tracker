import { Outlet } from 'react-router-dom'
import { Header } from '@/components/layout/Header.tsx'
import { Sidebar } from '@/components/layout/Sidebar.tsx'

export function MainLayout() {
  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />
      <div className="flex flex-col flex-1 overflow-hidden">
        <Header />
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}