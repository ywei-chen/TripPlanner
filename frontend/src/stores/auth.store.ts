import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authService } from '@/services/auth.service'
import { tokenManager } from '@/utils/tokenManager'
import type { User, LoginRequest, RegisterRequest } from '@/types/auth.types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const isAuthenticated = computed(() => !!user.value)

  async function login(data: LoginRequest) {
    const res = await authService.login(data)
    tokenManager.setTokens(res.accessToken, res.refreshToken)
    user.value = res.user
  }

  async function register(data: RegisterRequest) {
    const res = await authService.register(data)
    tokenManager.setTokens(res.accessToken, res.refreshToken)
    user.value = res.user
  }

  async function logout() {
    const refreshToken = tokenManager.getRefreshToken()
    if (refreshToken) await authService.logout(refreshToken).catch(() => {})
    tokenManager.clearTokens()
    user.value = null
  }

  async function fetchMe() {
    if (!tokenManager.getAccessToken()) return
    try {
      user.value = await authService.me()
    } catch {
      tokenManager.clearTokens()
    }
  }

  return { user, isAuthenticated, login, register, logout, fetchMe }
})
