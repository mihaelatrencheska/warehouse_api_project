import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'sonner'
import { queryClient } from './lib/queryClient'
import { Layout } from './components/layout/Layout'
import { PrivateRoute } from './components/layout/PrivateRoute'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { WarehousesPage } from './pages/WarehousesPage'
import { WarehouseDetailPage } from './pages/WarehouseDetailPage'
import { ProductsPage } from './pages/ProductsPage'
import { ImageSearchPage } from './pages/ImageSearchPage'
import { CategoriesPage } from './pages/CategoriesPage'
import { AlertsPage } from './pages/AlertsPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { authService } from './services/authService'

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route
            path="/login"
            element={authService.isAuthenticated() ? <Navigate to="/" replace /> : <LoginPage />}
          />
          <Route element={<PrivateRoute />}>
            <Route element={<Layout />}>
              <Route index element={<DashboardPage />} />
              <Route path="warehouses" element={<WarehousesPage />} />
              <Route path="warehouses/:id" element={<WarehouseDetailPage />} />
              <Route path="products" element={<ProductsPage />} />
              <Route path="products/search-by-image" element={<ImageSearchPage />} />
              <Route path="categories" element={<CategoriesPage />} />
              <Route path="alerts" element={<AlertsPage />} />
            </Route>
          </Route>
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </BrowserRouter>
      <Toaster position="bottom-right" richColors />
    </QueryClientProvider>
  )
}
