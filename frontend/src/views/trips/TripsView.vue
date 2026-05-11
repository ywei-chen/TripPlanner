<template>
  <div>
    <div class="page-header">
      <h1>我的行程</h1>
      <button class="btn btn-primary" @click="showCreate = true">+ 新增行程</button>
    </div>

    <div v-if="store.loading" class="loading">載入中...</div>

    <div v-else-if="store.trips.length === 0" class="empty-state card">
      <p>還沒有行程，立即建立你的第一個行程！</p>
      <button class="btn btn-primary" @click="showCreate = true">建立行程</button>
    </div>

    <div v-else class="trips-grid">
      <div
        v-for="trip in store.trips"
        :key="trip.id"
        class="trip-card card"
        @click="router.push(`/trips/${trip.id}`)"
      >
        <div class="trip-cover" :style="trip.coverImage ? `background-image:url(${trip.coverImage})` : ''">
          <span class="badge">{{ trip.status }}</span>
        </div>
        <div class="trip-info">
          <h3>{{ trip.title }}</h3>
          <p v-if="trip.startDate">{{ trip.startDate }} ~ {{ trip.endDate }}</p>
          <p class="item-count">{{ trip.items.length }} 個景點</p>
        </div>
      </div>
    </div>

    <!-- 建立行程 Modal -->
    <div v-if="showCreate" class="modal-overlay" @click.self="showCreate = false">
      <div class="card modal-box">
        <h3>建立新行程</h3>
        <form @submit.prevent="handleCreate">
          <div class="form-group">
            <label class="form-label">行程名稱</label>
            <input v-model="newTrip.title" class="form-input" required />
          </div>
          <div class="form-group">
            <label class="form-label">描述（選填）</label>
            <textarea v-model="newTrip.description" class="form-input" rows="3" />
          </div>
          <div style="display:grid; grid-template-columns: 1fr 1fr; gap: .75rem;">
            <div class="form-group">
              <label class="form-label">出發日期</label>
              <input v-model="newTrip.startDate" type="date" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">回程日期</label>
              <input v-model="newTrip.endDate" type="date" class="form-input" />
            </div>
          </div>
          <div style="display:flex; gap:.75rem; justify-content:flex-end;">
            <button type="button" class="btn btn-outline" @click="showCreate = false">取消</button>
            <button type="submit" class="btn btn-primary">建立</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTripsStore } from '@/stores/trips.store'

const store = useTripsStore()
const router = useRouter()
const showCreate = ref(false)
const newTrip = ref({ title: '', description: '', startDate: '', endDate: '' })

onMounted(() => store.fetchAll())

async function handleCreate() {
  const trip = await store.create({
    title: newTrip.value.title,
    description: newTrip.value.description || undefined,
    startDate: newTrip.value.startDate || undefined,
    endDate: newTrip.value.endDate || undefined
  })
  showCreate.value = false
  newTrip.value = { title: '', description: '', startDate: '', endDate: '' }
  router.push(`/trips/${trip.id}`)
}
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
.trips-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1.25rem; }
.trip-card { padding: 0; cursor: pointer; overflow: hidden; transition: transform .15s; }
.trip-card:hover { transform: translateY(-2px); }
.trip-cover {
  height: 140px;
  background: var(--gray-100);
  background-size: cover;
  background-position: center;
  display: flex;
  align-items: flex-start;
  padding: .75rem;
}
.trip-info { padding: 1rem; }
.trip-info h3 { margin-bottom: .25rem; }
.trip-info p { font-size: .875rem; color: var(--gray-600); }
.item-count { margin-top: .25rem; }
.empty-state { text-align: center; padding: 3rem; display: flex; flex-direction: column; align-items: center; gap: 1rem; }
.loading { text-align: center; padding: 2rem; color: var(--gray-600); }
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  display: flex; align-items: center; justify-content: center; z-index: 200;
}
.modal-box { width: 100%; max-width: 480px; }
.modal-box h3 { margin-bottom: 1.25rem; }
</style>
