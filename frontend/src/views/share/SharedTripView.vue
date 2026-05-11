<template>
  <div>
    <div v-if="loading" class="loading">載入中...</div>
    <div v-else-if="error" class="card" style="text-align:center; padding:3rem;">
      <p style="color:var(--danger);">{{ error }}</p>
      <RouterLink to="/" class="btn btn-primary" style="margin-top:1rem;">回首頁</RouterLink>
    </div>
    <template v-else-if="trip">
      <div class="shared-header">
        <h1>{{ trip.title }}</h1>
        <p v-if="trip.description" style="color:var(--gray-600);">{{ trip.description }}</p>
        <p v-if="trip.startDate" style="font-size:.875rem; color:var(--gray-600);">
          {{ trip.startDate }} ~ {{ trip.endDate }}
        </p>
      </div>

      <div v-for="day in groupedByDay" :key="day.dayNumber" class="day-group">
        <h3 class="day-label">第 {{ day.dayNumber }} 天</h3>
        <div class="day-items">
          <div v-for="item in day.items" :key="item.id" class="item-card card">
            <strong>{{ item.customName || item.attractionName || '景點' }}</strong>
            <span v-if="item.startTime" class="badge">{{ item.startTime }}</span>
            <p v-if="item.notes" style="font-size:.875rem; color:var(--gray-600);">{{ item.notes }}</p>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { shareService } from '@/services/share.service'
import type { Trip } from '@/types/trip.types'

const route = useRoute()
const trip = ref<Trip | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    trip.value = await shareService.getSharedTrip(route.params.token as string)
  } catch {
    error.value = '找不到此分享行程，連結可能已失效'
  } finally {
    loading.value = false
  }
})

const groupedByDay = computed(() => {
  if (!trip.value) return []
  const map = new Map<number, typeof trip.value.items>()
  for (const item of trip.value.items) {
    if (!map.has(item.dayNumber)) map.set(item.dayNumber, [])
    map.get(item.dayNumber)!.push(item)
  }
  return [...map.entries()].sort((a, b) => a[0] - b[0])
    .map(([dayNumber, items]) => ({ dayNumber, items }))
})
</script>

<style scoped>
.shared-header { margin-bottom: 2rem; }
.day-group { margin-bottom: 1.5rem; }
.day-label { color: var(--primary); margin-bottom: .75rem; }
.day-items { display: flex; flex-direction: column; gap: .75rem; }
.item-card { display: flex; align-items: center; gap: .75rem; flex-wrap: wrap; }
.loading { text-align: center; padding: 2rem; color: var(--gray-600); }
</style>
