<template>
  <div class="card" style="max-width:600px; margin: 0 auto;">
    <h2 style="margin-bottom:1.5rem;">編輯行程</h2>
    <form v-if="form" @submit.prevent="handleSubmit">
      <div class="form-group">
        <label class="form-label">行程名稱</label>
        <input v-model="form.title" class="form-input" required />
      </div>
      <div class="form-group">
        <label class="form-label">描述</label>
        <textarea v-model="form.description" class="form-input" rows="4" />
      </div>
      <div style="display:grid; grid-template-columns: 1fr 1fr; gap: .75rem;">
        <div class="form-group">
          <label class="form-label">出發日期</label>
          <input v-model="form.startDate" type="date" class="form-input" />
        </div>
        <div class="form-group">
          <label class="form-label">回程日期</label>
          <input v-model="form.endDate" type="date" class="form-input" />
        </div>
      </div>
      <div class="form-group" style="flex-direction:row; align-items:center; gap:.75rem;">
        <input v-model="form.isPublic" type="checkbox" id="isPublic" />
        <label for="isPublic" class="form-label" style="margin:0;">公開行程</label>
      </div>
      <p v-if="error" style="color:var(--danger); font-size:.875rem; margin-bottom:.75rem;">{{ error }}</p>
      <div style="display:flex; gap:.75rem; justify-content:flex-end;">
        <RouterLink :to="`/trips/${route.params.id}`" class="btn btn-outline">取消</RouterLink>
        <button type="submit" class="btn btn-primary" :disabled="saving">
          {{ saving ? '儲存中...' : '儲存' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTripsStore } from '@/stores/trips.store'

const store = useTripsStore()
const route = useRoute()
const router = useRouter()
const tripId = route.params.id as string
const saving = ref(false)
const error = ref('')

const form = ref<{
  title: string; description: string; coverImage: string
  startDate: string; endDate: string; isPublic: boolean
} | null>(null)

onMounted(async () => {
  await store.fetchById(tripId)
  const t = store.currentTrip!
  form.value = {
    title: t.title,
    description: t.description ?? '',
    coverImage: t.coverImage ?? '',
    startDate: t.startDate ?? '',
    endDate: t.endDate ?? '',
    isPublic: t.isPublic
  }
})

async function handleSubmit() {
  if (!form.value) return
  saving.value = true
  error.value = ''
  try {
    await store.update(tripId, {
      title: form.value.title,
      description: form.value.description || undefined,
      coverImage: form.value.coverImage || undefined,
      startDate: form.value.startDate || undefined,
      endDate: form.value.endDate || undefined,
      isPublic: form.value.isPublic
    })
    router.push(`/trips/${tripId}`)
  } catch {
    error.value = '儲存失敗，請稍後再試'
  } finally {
    saving.value = false
  }
}
</script>
