import axios from 'axios'
import { api, REFRESH_TOKEN_KEY, TOKEN_KEY } from '../lib/api'
import type { LoginRequest, LoginResponse } from '../types'

const baseURL = import.meta.env.VITE_API_URL || ''

function storeTokens(response: LoginResponse) {
  localStorage.setItem(TOKEN_KEY, response.accessToken)
  if (response.refreshToken) {
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken)
  }
}

/** Authentication service: login, token refresh, logout, and token accessors. */
export const authService = {
  /** Submits credentials to the API and stores the returned token pair. */
  async login(data: LoginRequest): Promise<LoginResponse> {
    const { data: response } = await api.post<LoginResponse>('/api/auth/login', data)
    storeTokens(response)
    return response
  },

  /** Uses the stored refresh token to obtain a new access token pair. */
  async refresh(): Promise<LoginResponse> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
    if (!refreshToken) {
      throw new Error('No refresh token')
    }
    const { data: response } = await axios.post<LoginResponse>(
      `${baseURL}/api/auth/refresh`,
      { refreshToken },
      { headers: { 'Content-Type': 'application/json' } },
    )
    storeTokens(response)
    return response
  },

  /** Clears stored tokens and redirects to the login page. */
  logout(): void {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    window.location.href = '/login'
  },

  /** Returns the current JWT access token from local storage, or null. */
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  },

  /** Returns true if an access token is present in local storage. */
  isAuthenticated(): boolean {
    return Boolean(localStorage.getItem(TOKEN_KEY))
  },
}
