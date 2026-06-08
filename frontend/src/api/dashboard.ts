import { api } from './client'
import type { DashboardStats } from '../types'

export const dashboardApi = {
  getStats: () => api.get<DashboardStats>('/dashboard/stats').then((r) => r.data),
}
