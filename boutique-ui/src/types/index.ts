export interface WarehouseSection {
  id: string
  warehouseId: string
  name: string
  productCount: number
}

export interface Warehouse {
  id: string
  name: string
  location?: string
  isActive: boolean
  createdAt: string
  deactivatedAt?: string
  sectionCount: number
  productCount: number
  sections?: WarehouseSection[]
}

export interface Category {
  id: string
  name: string
  description?: string
  productCount: number
  createdAt?: string
}

export interface ProductLocation {
  warehouseId: string
  warehouseName: string
  warehouseLocation?: string
  sectionId: string
  sectionName: string
}

export interface Product {
  id: string
  name: string
  sku: string
  description?: string
  size?: string
  type?: string
  expirationDate?: string
  imageUrl?: string
  imageMetadata?: string
  createdAt: string
  updatedAt?: string
  warehouseId?: string
  warehouseName?: string
  sectionId?: string
  sectionName?: string
  location?: ProductLocation
  categories: Category[]
}

export interface ProductSummary {
  id: string
  name: string
  sku: string
  size?: string
  type?: string
  expirationDate?: string
  imageUrl?: string
  warehouseId: string
  warehouseName: string
  sectionId: string
  sectionName: string
  categoryNames?: string
}

export interface ProductImageMatch extends ProductSummary {
  hammingDistance: number
}

export interface Alert {
  id: string
  productId: string
  productName: string
  productSku: string
  expirationDate?: string
  alertDate: string
  daysUntilExpiration: number
  isAcknowledged: boolean
  acknowledgedAt?: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken?: string
  tokenType: string
  expiresInSeconds: number
}

export interface ExpirationAlertNotification {
  alertId: string
  productId: string
  productName: string
  sku: string
  daysUntilExpiration: number
  expirationDate?: string
  alertDate: string
  warehouseName?: string
  sectionName?: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface CreateWarehousePayload {
  name: string
  location?: string
}

export interface CreateSectionPayload {
  name: string
}

export interface CreateCategoryPayload {
  name: string
  description?: string
}

export interface UpdateCategoryPayload {
  name: string
  description?: string
}

export interface UpdateWarehousePayload {
  name: string
  location?: string
}

export interface CreateProductPayload {
  name: string
  sku: string
  description?: string
  size?: string
  type?: string
  expirationDate?: string
  warehouseSectionId: string
  categoryIds: string[]
}

export interface UpdateProductPayload extends CreateProductPayload {}

export interface ProductSearchParams {
  query?: string
  categoryId?: string
  warehouseId?: string
  sectionId?: string
  size?: string
  type?: string
  expiringWithinDays?: number
  page?: number
  pageSize?: number
}
