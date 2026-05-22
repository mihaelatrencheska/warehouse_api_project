import { api } from '../lib/api'
import type { Category, CreateCategoryPayload, UpdateCategoryPayload } from '../types'

/** Category service: list, create, update, and delete product categories. */
export const categoryService = {
  /** Returns all categories ordered by name. */
  async getAll(): Promise<Category[]> {
    const { data } = await api.get<Category[]>('/api/categories')
    return data
  },

  /** Creates a new category and returns the persisted entity. */
  async create(payload: CreateCategoryPayload): Promise<Category> {
    const { data } = await api.post<Category>('/api/categories', payload)
    return data
  },

  /** Updates a category's name and optional description. */
  async update(id: string, payload: UpdateCategoryPayload): Promise<Category> {
    const { data } = await api.put<Category>(`/api/categories/${id}`, payload)
    return data
  },

  /** Deletes a category (fails if still assigned to any products). */
  async delete(id: string): Promise<void> {
    await api.delete(`/api/categories/${id}`)
  },
}
