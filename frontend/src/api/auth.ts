import { api } from './client'
import type { AuthUser } from '../types'

interface AuthResponse {
  token: string
  user: AuthUser
}

export const authApi = {
  login: (email: string, password: string) =>
    api.post<AuthResponse>('/auth/login', { email, password }).then((r) => r.data),

  register: (name: string, email: string, password: string) =>
    api.post<AuthResponse>('/auth/register', { name, email, password }).then((r) => r.data),
}
