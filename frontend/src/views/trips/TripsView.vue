<template>
  <div>
    <div class="page-header">
      <h1>我的行程</h1>
      <button class="btn btn-primary" @click="showCreate = true">+ 新增行程</button>
    </div>

    <div v-if="store.loading" class="loading">載入中...</div>

    <div v-else-if="store.trips.length === 0" class="empty-state card">
      <div class="empty-icon">🗺️</div>
      <p>還沒有行程，立即規劃你的第一趟旅程！</p>
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
          <p v-if="trip.startDate" class="trip-dates">
            <span>{{ trip.startDate }}</span>
            <span class="trip-dates-sep">–</span>
            <span>{{ trip.endDate ?? '未定' }}</span>
          </p>
          <p class="item-count">{{ trip.items.length }} 個景點</p>
        </div>
      </div>
    </div>

    <!-- 建立行程 Modal -->
    <div v-if="showCreate" class="modal-overlay" @click.self="showCreate = false">
      <div class="card modal-box">
        <div class="modal-header">
          <h3>建立新行程</h3>
          <button class="modal-close" @click="showCreate = false">✕</button>
        </div>
        <form @submit.prevent="handleCreate">
          <div class="form-group">
            <label class="form-label">行程名稱</label>
            <input v-model="newTrip.title" class="form-input" placeholder="例：東京五日遊" required />
          </div>
          <div class="form-group">
            <label class="form-label">描述（選填）</label>
            <textarea v-model="newTrip.description" class="form-input" rows="2" placeholder="行程備注..." />
          </div>
          <div style="display:grid; grid-template-columns: 1fr 1fr; gap: .75rem;">
            <div class="form-group" style="margin-bottom:0">
              <label class="form-label">出發日期</label>
              <input v-model="newTrip.startDate" type="date" class="form-input" />
            </div>
            <div class="form-group" style="margin-bottom:0">
              <label class="form-label">回程日期</label>
              <input v-model="newTrip.endDate" type="date" class="form-input" />
            </div>
          </div>
          <div class="modal-footer" style="margin-top:1.25rem">
            <button type="button" class="btn btn-outline" @click="showCreate = false">取消</button>
            <button type="submit" class="btn btn-primary">建立行程</button>
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
.trips-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(295px, 1fr)); gap: 1.25rem; }

.trip-card {
  padding: 0; cursor: pointer; overflow: hidden;
  border-radius: var(--radius-lg);
  transition: transform .2s var(--ease), box-shadow .2s var(--ease);
}
.trip-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}
.trip-card:active { transform: translateY(0); }

.trip-cover {
  height: 156px;
  background: linear-gradient(155deg, #7a3a10 0%, #b35f20 40%, #d4892e 75%, #e8a045 100%);
  background-size: cover;
  background-position: center;
  position: relative;
  display: flex;
  align-items: flex-end;
  padding: .875rem;
}
.trip-cover::after {
  content: '';
  position: absolute; inset: 0;
  background: linear-gradient(to bottom, transparent 30%, rgba(10,3,5,.55) 100%);
}
.trip-cover .badge {
  position: relative; z-index: 1;
  background: rgba(255,255,255,.15);
  color: rgba(255,255,255,.9);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255,255,255,.2);
  font-size: .68rem;
}

.trip-info { padding: 1rem 1.125rem 1.125rem; }
.trip-info h3 {
  margin-bottom: .3rem;
  font-size: 1rem; font-weight: 700;
  color: var(--gray-900); letter-spacing: -.01em;
}
.trip-dates {
  font-size: .8rem; color: var(--gray-400);
  display: flex; align-items: center; gap: .375rem;
}
.trip-dates-sep { color: var(--gray-200); }
.item-count {
  margin-top: .625rem; font-size: .8rem; color: var(--primary); font-weight: 600;
  display: flex; align-items: center; gap: .3rem;
}
.item-count::before {
  content: ''; width: 5px; height: 5px;
  background: var(--primary); border-radius: 50%;
}

.empty-icon { font-size: 2.75rem; opacity: .4; }
.empty-state p { font-size: .9375rem; color: var(--gray-500); }

/* Modal */
.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 1.375rem; padding-bottom: 1rem; border-bottom: 1px solid var(--gray-200);
}
.modal-header h3 { font-size: 1.0625rem; font-weight: 700; color: var(--gray-900); margin: 0; letter-spacing: -.01em; }
.modal-close {
  background: none; border: none; font-size: 1rem; cursor: pointer; color: var(--gray-400);
  width: 32px; height: 32px; border-radius: 8px; display: flex; align-items: center; justify-content: center;
  transition: background .12s, color .12s;
}
.modal-close:hover { background: var(--gray-100); color: var(--gray-700); }
.modal-footer { display: flex; gap: .75rem; justify-content: flex-end; padding-top: .75rem; border-top: 1px solid var(--gray-100); margin-top: .5rem; }
</style>
