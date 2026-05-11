import api from './api'
import type { AuthResponse, LoginRequest, RegisterRequest, User } from '@/types/auth.types'

export const authService = {
  register: (data: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', data).then(r => r.data),

  login: (data: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', data).then(r => r.data),

  logout: (refreshToken: string) =>
    api.post('/auth/logout', { refreshToken }),

  refresh: (refreshToken: string) =>
    api.post<AuthResponse>('/auth/refresh', { refreshToken }).then(r => r.data),

  me: () =>
    api.get<User>('/auth/me').then(r => r.data)
}
