import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'

export const TOKEN_KEY = 'boutique_token'
export const REFRESH_TOKEN_KEY = 'boutique_refresh_token'

const baseURL = import.meta.env.VITE_API_URL || ''

export const api = axios.create({
  baseURL,
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let isRefreshing = false
let refreshWaiters: Array<{ resolve: (token: string) => void; reject: (err: unknown) => void }> = []

function subscribeTokenRefresh(resolve: (token: string) => void, reject: (err: unknown) => void) {
  refreshWaiters.push({ resolve, reject })
}

function onRefreshed(token: string) {
  refreshWaiters.forEach(({ resolve }) => resolve(token))
  refreshWaiters = []
}

function onRefreshFailed(err: unknown) {
  refreshWaiters.forEach(({ reject }) => reject(err))
  refreshWaiters = []
}

function clearAuthAndRedirect() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  if (window.location.pathname !== '/login') {
    window.location.href = '/login'
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }
    if (!originalRequest || error.response?.status !== 401) {
      return Promise.reject(error)
    }

    const url = originalRequest.url ?? ''
    if (url.includes('/api/auth/login') || url.includes('/api/auth/refresh')) {
      clearAuthAndRedirect()
      return Promise.reject(error)
    }

    if (originalRequest._retry) {
      clearAuthAndRedirect()
      return Promise.reject(error)
    }

    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
    if (!refreshToken) {
      clearAuthAndRedirect()
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        subscribeTokenRefresh(
          (token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            resolve(api(originalRequest))
          },
          reject,
        )
      })
    }

    originalRequest._retry = true
    isRefreshing = true

    try {
      const { data } = await axios.post<{
        accessToken: string
        refreshToken: string
      }>(`${baseURL}/api/auth/refresh`, { refreshToken }, { headers: { 'Content-Type': 'application/json' } })

      localStorage.setItem(TOKEN_KEY, data.accessToken)
      localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken)
      onRefreshed(data.accessToken)
      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
      return api(originalRequest)
    } catch (refreshError) {
      onRefreshFailed(refreshError)
      clearAuthAndRedirect()
      return Promise.reject(refreshError)
    } finally {
      isRefreshing = false
    }
  },
)

export function getAccessToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}
