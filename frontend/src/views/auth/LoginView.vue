<template>
  <div class="auth-page">
    <div class="card auth-card">
      <h2>登入</h2>
      <form @submit.prevent="handleSubmit">
        <div class="form-group">
          <label class="form-label">Email</label>
          <input v-model="form.email" type="email" class="form-input" required autocomplete="email" />
        </div>
        <div class="form-group">
          <label class="form-label">密碼</label>
          <input v-model="form.password" type="password" class="form-input" required autocomplete="current-password" />
        </div>

        <!-- 錯誤訊息 -->
        <div v-if="errors.length" class="error-box">
          <p v-for="e in errors" :key="e">{{ e }}</p>
        </div>

        <!-- 後端未啟動提示 -->
        <div v-if="isNetworkError" class="warn-box">
          ⚠️ 無法連線到伺服器，請確認後端服務已啟動（<code>dotnet run</code>）
        </div>

        <button type="submit" class="btn btn-primary w-full" :disabled="loading">
          {{ loading ? '登入中...' : '登入' }}
        </button>
      </form>
      <p class="switch-link">還沒有帳號？ <RouterLink to="/register">立即註冊</RouterLink></p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import axios from 'axios'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const form = ref({ email: '', password: '' })
const loading = ref(false)
const errors = ref<string[]>([])
const isNetworkError = ref(false)

async function handleSubmit() {
  loading.value = true
  errors.value = []
  isNetworkError.value = false
  try {
    await auth.login(form.value)
    const redirect = route.query.redirect as string || '/trips'
    router.push(redirect)
  } catch (e: unknown) {
    if (axios.isAxiosError(e)) {
      if (!e.response) {
        isNetworkError.value = true
      } else {
        const data = e.response.data
        if (Array.isArray(data?.errors) && data.errors.length) {
          errors.value = data.errors
        } else if (data?.message) {
          errors.value = [data.message]
        } else {
          errors.value = [`伺服器錯誤（${e.response.status}），請稍後再試`]
        }
      }
    } else {
      errors.value = ['發生未預期的錯誤，請稍後再試']
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.auth-page { display: flex; justify-content: center; padding: 3rem 1rem; }
.auth-card { width: 100%; max-width: 420px; }
.auth-card h2 { margin-bottom: 1.5rem; }
.w-full { width: 100%; justify-content: center; }

.error-box {
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: var(--radius);
  padding: .625rem .875rem;
  margin-bottom: 1rem;
  display: flex;
  flex-direction: column;
  gap: .25rem;
}
.error-box p { color: var(--danger); font-size: .875rem; margin: 0; }

.warn-box {
  background: #fffbeb;
  border: 1px solid #fde68a;
  border-radius: var(--radius);
  padding: .625rem .875rem;
  margin-bottom: 1rem;
  font-size: .875rem;
  color: #92400e;
  line-height: 1.5;
}
.warn-box code {
  background: rgba(0,0,0,.08);
  padding: .1rem .35rem;
  border-radius: 4px;
  font-size: .8125rem;
}

.switch-link { margin-top: 1rem; font-size: .875rem; text-align: center; }
</style>
