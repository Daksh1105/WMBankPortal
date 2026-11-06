import React, { createContext, useContext, useState, useEffect } from 'react'
import type { UserDto, AuthResponse } from '../types/auth'
import { apiClient, setInMemoryAccessToken, setOnSessionExpired } from '../api/client'

interface AuthContextType {
  user: UserDto | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, confirmPassword: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserDto | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)

  useEffect(() => {
    setOnSessionExpired(() => {
      setUser(null)
    })

    // Check if an active session exists via HttpOnly refresh cookie
    const checkExistingSession = async () => {
      try {
        const response = await apiClient.post<AuthResponse>('/api/v1/auth/refresh', {})
        setInMemoryAccessToken(response.data.accessToken)
        setUser(response.data.user)
      } catch {
        // No existing session or token expired; user remains guest
        setInMemoryAccessToken(null)
        setUser(null)
      } finally {
        setIsLoading(false)
      }
    }

    checkExistingSession()
  }, [])

  const login = async (email: string, password: string) => {
    const response = await apiClient.post<AuthResponse>('/api/v1/auth/login', {
      email,
      password,
    })

    setInMemoryAccessToken(response.data.accessToken)
    setUser(response.data.user)
  }

  const register = async (email: string, password: string, confirmPassword: string) => {
    const response = await apiClient.post<AuthResponse>('/api/v1/auth/register', {
      email,
      password,
      confirmPassword,
    })

    setInMemoryAccessToken(response.data.accessToken)
    setUser(response.data.user)
  }

  const logout = async () => {
    try {
      await apiClient.post('/api/v1/auth/logout', {})
    } finally {
      setInMemoryAccessToken(null)
      setUser(null)
    }
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
