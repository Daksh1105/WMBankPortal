export type UserRole = 'Customer' | 'SupportAgent' | 'Officer' | 'Admin'

export interface UserDto {
  id: string
  email: string
  role: UserRole
}

export interface AuthResponse {
  accessToken: string
  expiresIn: number
  user: UserDto
}

export interface ProblemDetails {
  type?: string
  title: string
  status: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
  correlationId?: string
  errorCode?: string
}
