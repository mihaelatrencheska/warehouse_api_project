import { api } from '../lib/api'
import type {
  CreateSectionPayload,
  CreateWarehousePayload,
  ProductSummary,
  UpdateWarehousePayload,
  Warehouse,
  WarehouseSection,
} from '../types'

/** Warehouse service: CRUD for warehouses and their sections, plus product listing. */
export const warehouseService = {
  /** Returns all warehouses, optionally filtered by active status. */
  async getAll(isActive?: boolean): Promise<Warehouse[]> {
    const params = isActive === undefined ? {} : { isActive }
    const { data } = await api.get<Warehouse[]>('/api/warehouses', { params })
    return data
  },

  /** Fetches a single warehouse including its sections. */
  async getById(id: string): Promise<Warehouse> {
    const { data } = await api.get<Warehouse>(`/api/warehouses/${id}`)
    return data
  },

  /** Creates a new warehouse and returns the persisted entity. */
  async create(payload: CreateWarehousePayload): Promise<Warehouse> {
    const { data } = await api.post<Warehouse>('/api/warehouses', payload)
    return data
  },

  /** Updates a warehouse's name and location. */
  async update(id: string, payload: UpdateWarehousePayload): Promise<Warehouse> {
    const { data } = await api.put<Warehouse>(`/api/warehouses/${id}`, payload)
    return data
  },

  /** Marks a warehouse as inactive so it no longer accepts new products. */
  async deactivate(id: string): Promise<void> {
    await api.patch(`/api/warehouses/${id}/deactivate`)
  },

  /** Restores a previously deactivated warehouse to active status. */
  async reactivate(id: string): Promise<void> {
    await api.patch(`/api/warehouses/${id}/reactivate`)
  },

  /** Adds a new named section to a warehouse. */
  async createSection(warehouseId: string, payload: CreateSectionPayload): Promise<WarehouseSection> {
    const { data } = await api.post<WarehouseSection>(
      `/api/warehouses/${warehouseId}/sections`,
      payload,
    )
    return data
  },

  /** Renames an existing warehouse section. */
  async updateSection(
    warehouseId: string,
    sectionId: string,
    payload: CreateSectionPayload,
  ): Promise<WarehouseSection> {
    const { data } = await api.put<WarehouseSection>(
      `/api/warehouses/${warehouseId}/sections/${sectionId}`,
      payload,
    )
    return data
  },

  /** Deletes a warehouse section (only when it contains no products). */
  async deleteSection(warehouseId: string, sectionId: string): Promise<void> {
    await api.delete(`/api/warehouses/${warehouseId}/sections/${sectionId}`)
  },

  /** Lists all products stored in the given warehouse. */
  async getProducts(warehouseId: string): Promise<ProductSummary[]> {
    const { data } = await api.get<ProductSummary[]>(`/api/warehouses/${warehouseId}/products`)
    return data
  },
}
