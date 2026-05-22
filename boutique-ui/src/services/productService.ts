import { api } from '../lib/api'
import type {
  CreateProductPayload,
  PagedResult,
  Product,
  ProductImageMatch,
  ProductSearchParams,
  ProductSummary,
  UpdateProductPayload,
} from '../types'

function hasSearchFilters(params: ProductSearchParams): boolean {
  return Boolean(
    params.query ||
      params.categoryId ||
      params.warehouseId ||
      params.sectionId ||
      params.size ||
      params.type ||
      params.expiringWithinDays,
  )
}

/** Product service: CRUD, search, image upload, category assignment, and expiry queries. */
export const productService = {
  /** Fetches a paginated list of products without any search filters. */
  async browse(page = 1, pageSize = 10): Promise<PagedResult<ProductSummary>> {
    const { data } = await api.get<PagedResult<ProductSummary>>('/api/products', {
      params: { page, pageSize },
    })
    return data
  },

  /** Runs a full-text / filter search and returns a paginated result. */
  async search(params: ProductSearchParams): Promise<PagedResult<ProductSummary>> {
    const { data } = await api.get<PagedResult<ProductSummary>>('/api/products/search', {
      params,
    })
    return data
  },

  /** Routes to search or browse depending on whether any filter params are present. */
  async getAll(params: ProductSearchParams): Promise<PagedResult<ProductSummary>> {
    if (hasSearchFilters(params)) {
      return this.search(params)
    }
    return this.browse(params.page ?? 1, params.pageSize ?? 10)
  },

  /** Fetches a single product by ID, ensuring the categories array is never undefined. */
  async getById(id: string): Promise<Product> {
    const { data } = await api.get<Product>(`/api/products/${id}`)
    return { ...data, categories: data.categories ?? [] }
  },

  /** Creates a new product and returns the persisted entity. */
  async create(payload: CreateProductPayload): Promise<Product> {
    const { data } = await api.post<Product>('/api/products', payload)
    return data
  },

  /** Updates an existing product's fields and returns the updated entity. */
  async update(id: string, payload: UpdateProductPayload): Promise<Product> {
    const { data } = await api.put<Product>(`/api/products/${id}`, payload)
    return data
  },

  /** Permanently deletes a product by ID. */
  async delete(id: string): Promise<void> {
    await api.delete(`/api/products/${id}`)
  },

  /** Moves a product to a different warehouse section. */
  async move(id: string, warehouseSectionId: string): Promise<Product> {
    const { data } = await api.patch<Product>(`/api/products/${id}/move`, { warehouseSectionId })
    return data
  },

  /** Uploads a product image and returns the updated product with the new image URL. */
  async uploadImage(id: string, file: File): Promise<Product> {
    const form = new FormData()
    form.append('file', file)
    const { data } = await api.post<Product>(`/api/products/${id}/image`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },

  /** Replaces the full set of categories assigned to a product. */
  async updateCategories(id: string, categoryIds: string[]): Promise<Product> {
    const { data } = await api.patch<Product>(`/api/products/${id}/categories`, { categoryIds })
    return data
  },

  /** Returns products whose expiration date falls within the given day window. */
  async getExpiring(withinDays = 30): Promise<ProductSummary[]> {
    const { data } = await api.get<ProductSummary[]>('/api/products/expiring', {
      params: { withinDays },
    })
    return data
  },

  /** Returns the total count of already-expired products. */
  async countExpired(): Promise<number> {
    const { data } = await api.get<number>('/api/products/expired/count')
    return data
  },

  /** Performs perceptual-hash image search and returns ranked product matches. */
  async searchByImage(file: File, maxResults = 12): Promise<ProductImageMatch[]> {
    const form = new FormData()
    form.append('file', file)
    const { data } = await api.post<ProductImageMatch[]>('/api/products/search-by-image', form, {
      params: { maxResults, maxHammingDistance: 6 },
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },
}
