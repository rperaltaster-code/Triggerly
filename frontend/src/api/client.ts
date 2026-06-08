import axios from 'axios'

export const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.response.use(
  (r) => r,
  (error) => {
    const message = error.response?.data?.message ?? error.message
    return Promise.reject(new Error(message))
  }
)
