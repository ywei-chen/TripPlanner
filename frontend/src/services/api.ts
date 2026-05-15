import axios from 'axios'
import { tokenManager } from '@/utils/tokenManager'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' }
})

api.interceptors.request.use(config => {
  const token = tokenManager.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let isRefreshing = false
let refreshQueue: ((token: string) => void)[] = []

api.interceptors.response.use(
  res => res,
  async error => {
    const original = error.config
    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise(resolve => {
        refreshQueue.push(token => {
          original.headers.Authorization = `Bearer ${token}`
          resolve(api(original))
        })
      })
    }

    original._retry = true
    isRefreshing = true

    try {
      const refreshToken = tokenManager.getRefreshToken()
      if (!refreshToken) throw new Error('No refresh token')

      const { data } = await axios.post('/api/auth/refresh', { refreshToken })
      tokenManager.setTokens(data.accessToken, data.refreshToken)

      refreshQueue.forEach(cb => cb(data.accessToken))
      refreshQueue = []

      original.headers.Authorization = `Bearer ${data.accessToken}`
      return api(original)
    } catch {
      tokenManager.clearTokens()
      window.location.href = '/login'
      return Promise.reject(error)
    } finally {
      isRefreshing = false
    }
  }
)

export default api
