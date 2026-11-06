import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import type { AuthResponse, ProblemDetails } from '../types/auth'

// In-Memory Token Holder (Never written to localStorage to protect against XSS token theft)
let inMemoryAccessToken: string | null = null
let onSessionExpiredCallback: (() => void) | null = null

export const setInMemoryAccessToken = (token: string | null) => {
  inMemoryAccessToken = token
}

export const getInMemoryAccessToken = (): string | null => {
  return inMemoryAccessToken
}

export const setOnSessionExpired = (callback: () => void) => {
  onSessionExpiredCallback = callback
}

export const apiClient = axios.create({
  baseURL: '/',
  headers: {
    'Content-Type': 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
  },
  withCredentials: true, // Required for HttpOnly SameSite=Strict cookies to be transmitted
})

// Request Interceptor: Attach in-memory JWT bearer token
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (inMemoryAccessToken && config.headers) {
    config.headers.Authorization = `Bearer ${inMemoryAccessToken}`
  }
  return config
})

// Concurrency queue for token refresh
let isRefreshing = false
let failedQueue: Array<{
  resolve: (token: string) => void
  reject: (err: unknown) => void
}> = []

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((promise) => {
    if (token) {
      promise.resolve(token)
    } else {
      promise.reject(error)
    }
  })
  failedQueue = []
}

// Response Interceptor: Silent token refresh on 401 Unauthorized
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ProblemDetails>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    // If request failed with 401 and is not an auth endpoint, attempt silent refresh
    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes('/api/v1/auth/login') &&
      !originalRequest.url?.includes('/api/v1/auth/register') &&
      !originalRequest.url?.includes('/api/v1/auth/refresh')
    ) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({
            resolve: (token: string) => {
              if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${token}`
              }
              resolve(apiClient(originalRequest))
            },
            reject: (err) => reject(err),
          })
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const refreshResponse = await axios.post<AuthResponse>(
          '/api/v1/auth/refresh',
          {},
          {
            withCredentials: true,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
          }
        )

        const newAccessToken = refreshResponse.data.accessToken
        setInMemoryAccessToken(newAccessToken)
        processQueue(null, newAccessToken)

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`
        }
        return apiClient(originalRequest)
      } catch (refreshErr) {
        processQueue(refreshErr, null)
        setInMemoryAccessToken(null)
        if (onSessionExpiredCallback) {
          onSessionExpiredCallback()
        }
        return Promise.reject(refreshErr)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(error)
  }
)
