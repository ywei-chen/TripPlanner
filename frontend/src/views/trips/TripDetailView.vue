<template>
  <div class="td-layout">
    <!-- 左側：行程天數 -->
    <div class="left-panel">
      <div class="trip-header">
        <div class="trip-info">
          <h2 class="trip-title">{{ trip.title }}</h2>
          <p v-if="trip.startDate" class="trip-dates">
            {{ trip.startDate }} → {{ trip.endDate ?? '未定' }}
          </p>
        </div>
        <div class="header-btns">
          <RouterLink :to="`/trips/${tripId}/edit`" class="btn btn-outline btn-sm">編輯</RouterLink>
          <button class="btn btn-danger btn-sm" @click="handleDelete">刪除</button>
        </div>
      </div>

      <div class="days-scroll">
        <div v-if="allDays.length === 0" class="no-days">
          <p>點擊右側地圖上的景點，即可加入行程</p>
        </div>

        <template v-for="day in allDays" :key="day.dayNumber">
          <div class="day-block">
            <div class="day-header">
              <span class="day-badge">第 {{ day.dayNumber }} 天</span>
              <span v-if="getDayDate(day.dayNumber)" class="day-date">
                {{ getDayDate(day.dayNumber) }}
              </span>
            </div>

            <div
              class="day-body"
              :class="{ 'drag-over': dragOverDay === day.dayNumber }"
              @dragover.prevent="dragOverDay = day.dayNumber"
              @dragleave.self="dragOverDay = null"
              @drop.prevent="onDrop(day.dayNumber)"
            >
              <template v-for="(item, idx) in day.items" :key="item.id">
                <div
                  class="item-row"
                  draggable="true"
                  :class="{ dragging: draggingItemId === item.id }"
                  @dragstart="onDragStart(item)"
                  @dragend="onDragEnd"
                >
                  <span class="item-seq">{{ idx + 1 }}</span>
                  <div class="item-main">
                    <span class="item-name">{{ item.attractionName || item.customName || '自訂景點' }}</span>
                    <span v-if="item.startTime" class="item-time">{{ item.startTime }}</span>
                  </div>
                  <button class="item-remove" title="移除" @click.stop="doRemoveItem(item.id)">×</button>
                </div>

                <!-- 車程 -->
                <div v-if="idx < day.items.length - 1" class="travel-row">
                  <template v-if="getTravelTime(day.items[idx], day.items[idx + 1])">
                    <span class="travel-pill">🚗 {{ getTravelTime(day.items[idx], day.items[idx + 1]) }}</span>
                  </template>
                  <template v-else-if="canCalcTravel(day.items[idx], day.items[idx + 1])">
                    <span class="travel-pill travel-pending">計算中...</span>
                  </template>
                </div>
              </template>

              <div v-if="day.items.length === 0" class="day-empty">
                從地圖點選景點，或將景點拖曳到此
              </div>
            </div>
          </div>
        </template>

        <button class="add-day-btn" @click="addEmptyDay">
          + 新增第 {{ maxDay + 1 }} 天
        </button>
      </div>
    </div>

    <!-- 右側：Google Maps -->
    <div class="map-panel">
      <div v-if="mapError" class="map-err">⚠️ {{ mapError }}</div>
      <div ref="mapEl" class="map-container" />

      <!-- 加入行程浮窗 -->
      <Transition name="popup">
        <div v-if="pendingAttr" class="add-popup">
          <div class="popup-box">
            <button class="popup-close" @click="pendingAttr = null">✕</button>
            <p class="popup-name">{{ pendingAttr.name }}</p>
            <p v-if="pendingAttr.city" class="popup-sub">
              {{ [pendingAttr.city, pendingAttr.country].filter(Boolean).join(' · ') }}
            </p>
            <p class="popup-hint">加入哪一天？</p>
            <div class="popup-days">
              <button
                v-for="d in allDays"
                :key="d.dayNumber"
                class="btn btn-sm btn-outline"
                :disabled="addingDay !== null"
                @click="addToDay(pendingAttr!, d.dayNumber)"
              >第 {{ d.dayNumber }} 天</button>
              <button
                class="btn btn-sm btn-primary"
                :disabled="addingDay !== null"
                @click="addToDay(pendingAttr!, maxDay + 1)"
              >
                <span v-if="addingDay === maxDay + 1">加入中...</span>
                <span v-else>+ 第 {{ maxDay + 1 }} 天（新增）</span>
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import { useTripsStore } from '@/stores/trips.store'
import { useAttractionsStore } from '@/stores/attractions.store'
import type { TripItem } from '@/types/trip.types'
import type { Attraction } from '@/types/attraction.types'

const route = useRoute()
const router = useRouter()
const tripsStore = useTripsStore()
const attractionsStore = useAttractionsStore()
const tripId = route.params.id as string

// ── 地圖 refs ─────────────────────────────────────────────────────────────────
const mapEl = ref<HTMLElement | null>(null)
const mapError = ref('')
let map: google.maps.Map | null = null
let activeInfoWindow: google.maps.InfoWindow | null = null

// markers for DB attractions (gray)
const attrMarkers = new Map<string, google.maps.Marker>()
// markers for trip items (numbered blue)
const itemMarkers = new Map<string, google.maps.Marker>()

// ── 狀態 ──────────────────────────────────────────────────────────────────────
const pendingAttr = ref<Attraction | null>(null)
const addingDay = ref<number | null>(null)
const emptyDays = ref<Set<number>>(new Set())
const draggingItem = ref<TripItem | null>(null)
const draggingItemId = ref<string | null>(null)
const dragOverDay = ref<number | null>(null)
const travelTimes = ref<Record<string, string>>({})

// ── 行程資料 ──────────────────────────────────────────────────────────────────
const trip = computed(() => tripsStore.currentTrip!)

const groupedByDay = computed(() => {
  const t = tripsStore.currentTrip
  if (!t) return []
  const map = new Map<number, TripItem[]>()
  for (const item of t.items) {
    if (!map.has(item.dayNumber)) map.set(item.dayNumber, [])
    map.get(item.dayNumber)!.push(item)
  }
  for (const [, items] of map) {
    items.sort((a, b) => a.sortOrder - b.sortOrder)
  }
  return [...map.entries()].sort((a, b) => a[0] - b[0])
    .map(([dayNumber, items]) => ({ dayNumber, items }))
})

const allDays = computed(() => {
  const days = new Map<number, TripItem[]>()
  for (const d of groupedByDay.value) days.set(d.dayNumber, d.items)
  for (const n of emptyDays.value) if (!days.has(n)) days.set(n, [])
  return [...days.entries()].sort((a, b) => a[0] - b[0])
    .map(([dayNumber, items]) => ({ dayNumber, items }))
})

const maxDay = computed(() => {
  const nums = allDays.value.map(d => d.dayNumber)
  return nums.length > 0 ? Math.max(...nums) : 0
})

function getDayDate(dayNumber: number): string {
  const t = tripsStore.currentTrip
  if (!t?.startDate) return ''
  const start = new Date(t.startDate)
  start.setDate(start.getDate() + dayNumber - 1)
  return start.toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric', weekday: 'short' })
}

function addEmptyDay() {
  emptyDays.value = new Set([...emptyDays.value, maxDay.value + 1])
}

// ── 刪除行程 ──────────────────────────────────────────────────────────────────
async function handleDelete() {
  if (!confirm('確定要刪除此行程？')) return
  await tripsStore.remove(tripId)
  router.push('/trips')
}

async function doRemoveItem(itemId: string) {
  await tripsStore.removeItem(tripId, itemId)
  updateItemMarkers()
  computeTravelTimes()
}

// ── 從地圖加入景點 ────────────────────────────────────────────────────────────
async function addToDay(attr: Attraction, dayNumber: number) {
  addingDay.value = dayNumber
  const targetDayItems = allDays.value.find(d => d.dayNumber === dayNumber)?.items ?? []
  try {
    await tripsStore.addItem(tripId, {
      attractionId: attr.id,
      dayNumber,
      sortOrder: targetDayItems.length + 1
    })
    // Remove empty day placeholder if now has items
    if (emptyDays.value.has(dayNumber)) {
      const next = new Set(emptyDays.value)
      next.delete(dayNumber)
      emptyDays.value = next
    }
    pendingAttr.value = null
    await nextTick()
    updateItemMarkers()
    computeTravelTimes()
  } finally {
    addingDay.value = null
  }
}

// ── 拖曳排序 ──────────────────────────────────────────────────────────────────
function onDragStart(item: TripItem) {
  draggingItem.value = item
  draggingItemId.value = item.id
}

function onDragEnd() {
  draggingItem.value = null
  draggingItemId.value = null
  dragOverDay.value = null
}

async function onDrop(targetDay: number) {
  dragOverDay.value = null
  const item = draggingItem.value
  if (!item || item.dayNumber === targetDay) return

  const targetItems = allDays.value.find(d => d.dayNumber === targetDay)?.items ?? []
  const allItems = tripsStore.currentTrip!.items.map(i =>
    i.id === item.id
      ? { itemId: i.id, dayNumber: targetDay, sortOrder: targetItems.length + 1 }
      : { itemId: i.id, dayNumber: i.dayNumber, sortOrder: i.sortOrder }
  )
  await tripsStore.reorderItems(tripId, { items: allItems })
  updateItemMarkers()
  computeTravelTimes()
}

// ── 車程計算 ──────────────────────────────────────────────────────────────────
function travelKey(a: TripItem, b: TripItem): string {
  return `${a.attractionLatitude},${a.attractionLongitude}>${b.attractionLatitude},${b.attractionLongitude}`
}

function canCalcTravel(a: TripItem, b: TripItem): boolean {
  return !!(a.attractionLatitude && a.attractionLongitude && b.attractionLatitude && b.attractionLongitude)
}

function getTravelTime(a: TripItem, b: TripItem): string | null {
  if (!canCalcTravel(a, b)) return null
  return travelTimes.value[travelKey(a, b)] ?? null
}

let travelBusy = false
async function computeTravelTimes() {
  if (!window.google?.maps || travelBusy) return
  travelBusy = true
  const ds = new google.maps.DirectionsService()
  for (const day of allDays.value) {
    for (let i = 0; i < day.items.length - 1; i++) {
      const a = day.items[i]
      const b = day.items[i + 1]
      if (!canCalcTravel(a, b)) continue
      const key = travelKey(a, b)
      if (travelTimes.value[key]) continue
      await new Promise<void>(resolve => {
        ds.route({
          origin: { lat: Number(a.attractionLatitude), lng: Number(a.attractionLongitude) },
          destination: { lat: Number(b.attractionLatitude), lng: Number(b.attractionLongitude) },
          travelMode: google.maps.TravelMode.DRIVING
        }, (result, status) => {
          if (status === 'OK' && result) {
            const text = result.routes[0]?.legs[0]?.duration?.text
            if (text) travelTimes.value[key] = text
          }
          resolve()
        })
      })
    }
  }
  travelBusy = false
}

// ── Google Maps ───────────────────────────────────────────────────────────────
function loadGoogleMaps(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window.google?.maps) { resolve(); return }
    const cbName = `__gmTD${Date.now()}`
    ;(window as Record<string, unknown>)[cbName] = () => {
      resolve()
      delete (window as Record<string, unknown>)[cbName]
    }
    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?key=${import.meta.env.VITE_GOOGLE_MAPS_KEY}&callback=${cbName}&language=zh-TW&region=TW`
    script.async = true
    script.onerror = () => reject(new Error('Google Maps 載入失敗'))
    document.head.appendChild(script)
  })
}

function updateAttrMarkers(attractions: Attraction[]) {
  if (!map || !window.google?.maps) return
  attrMarkers.forEach(m => m.setMap(null))
  attrMarkers.clear()

  const tripAttrIds = new Set(
    (tripsStore.currentTrip?.items ?? []).map(i => i.attractionId).filter(Boolean)
  )

  attractions.filter(a => a.latitude && a.longitude && !tripAttrIds.has(a.id)).forEach(a => {
    const marker = new google.maps.Marker({
      position: { lat: Number(a.latitude!), lng: Number(a.longitude!) },
      map: map!,
      title: a.name,
      icon: {
        path: google.maps.SymbolPath.CIRCLE,
        scale: 7,
        fillColor: '#6b7280',
        fillOpacity: 0.85,
        strokeColor: '#fff',
        strokeWeight: 1.5,
      },
      zIndex: 1,
    })
    marker.addListener('click', () => {
      activeInfoWindow?.close()
      pendingAttr.value = a
      map!.panTo({ lat: Number(a.latitude!), lng: Number(a.longitude!) })
    })
    attrMarkers.set(a.id, marker)
  })
}

function updateItemMarkers() {
  if (!map || !window.google?.maps) return
  itemMarkers.forEach(m => m.setMap(null))
  itemMarkers.clear()

  const items = tripsStore.currentTrip?.items ?? []
  const sorted = [...items].sort((a, b) =>
    a.dayNumber !== b.dayNumber ? a.dayNumber - b.dayNumber : a.sortOrder - b.sortOrder
  )

  sorted.forEach((item, globalIdx) => {
    if (!item.attractionLatitude || !item.attractionLongitude) return
    const label = String(globalIdx + 1)
    const marker = new google.maps.Marker({
      position: { lat: Number(item.attractionLatitude), lng: Number(item.attractionLongitude) },
      map: map!,
      title: item.attractionName || item.customName || '',
      label: { text: label, color: '#fff', fontSize: '11px', fontWeight: 'bold' },
      icon: {
        path: google.maps.SymbolPath.CIRCLE,
        scale: 13,
        fillColor: '#2563eb',
        fillOpacity: 1,
        strokeColor: '#fff',
        strokeWeight: 2,
      },
      zIndex: 10,
    })
    marker.addListener('click', () => {
      activeInfoWindow?.close()
      const iw = new google.maps.InfoWindow({
        content: `<div style="font-family:system-ui;line-height:1.5;min-width:150px;">
          <strong style="font-size:.875rem;">第 ${item.dayNumber} 天 · ${label}號</strong><br>
          <span style="font-size:.8rem;color:#374151;">${item.attractionName || item.customName || '自訂景點'}</span>
        </div>`
      })
      iw.open({ map: map!, anchor: marker })
      activeInfoWindow = iw
    })
    itemMarkers.set(item.id, marker)
  })

  // Re-filter attr markers to hide already-added attractions
  if (attractionsStore.result) {
    updateAttrMarkers(attractionsStore.result.items)
  }
}

async function initMap() {
  if (!mapEl.value) return
  try {
    await loadGoogleMaps()
    map = new google.maps.Map(mapEl.value, {
      center: { lat: 23.5, lng: 121 },
      zoom: 5,
      mapTypeControl: false,
      fullscreenControl: true,
      streetViewControl: false,
      zoomControl: true,
    })

    // 載入景點
    await attractionsStore.search({ pageSize: 100, page: 1 })
    if (attractionsStore.result) {
      updateAttrMarkers(attractionsStore.result.items)
    }

    updateItemMarkers()

    // 如果行程有景點，聚焦到行程範圍
    const items = tripsStore.currentTrip?.items.filter(i => i.attractionLatitude) ?? []
    if (items.length > 0) {
      const bounds = new google.maps.LatLngBounds()
      items.forEach(i => bounds.extend({ lat: Number(i.attractionLatitude), lng: Number(i.attractionLongitude) }))
      map.fitBounds(bounds, 80)
    }

    computeTravelTimes()
  } catch (e: unknown) {
    mapError.value = e instanceof Error ? e.message : '未知錯誤'
  }
}

// ── 監聽行程變化 ──────────────────────────────────────────────────────────────
watch(() => tripsStore.currentTrip?.items, () => {
  updateItemMarkers()
}, { deep: true })

// ── 生命週期 ──────────────────────────────────────────────────────────────────
onMounted(async () => {
  await tripsStore.fetchById(tripId)
  await initMap()
})

onUnmounted(() => {
  attrMarkers.forEach(m => m.setMap(null))
  itemMarkers.forEach(m => m.setMap(null))
  attrMarkers.clear()
  itemMarkers.clear()
  activeInfoWindow?.close()
  map = null
})
</script>

<style scoped>
.td-layout {
  display: grid;
  grid-template-columns: 360px 1fr;
  flex: 1;
  overflow: hidden;
}

/* ── 左側 ── */
.left-panel {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-right: 1px solid var(--gray-200);
  background: #fff;
}

.trip-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: .875rem;
  border-bottom: 1px solid var(--gray-200);
  flex-shrink: 0;
  gap: .5rem;
}
.trip-title {
  font-size: 1rem;
  font-weight: 600;
  line-height: 1.3;
  margin: 0;
}
.trip-dates {
  font-size: .75rem;
  color: var(--gray-500);
  margin: .2rem 0 0;
}
.header-btns {
  display: flex;
  gap: .375rem;
  flex-shrink: 0;
}
.btn-sm { padding: .25rem .625rem; font-size: .8rem; }
.btn-danger {
  background: #fee2e2; color: #b91c1c;
  border: 1px solid #fca5a5;
}
.btn-danger:hover { background: #fecaca; }

.days-scroll {
  flex: 1;
  overflow-y: auto;
  padding: .625rem;
  display: flex;
  flex-direction: column;
  gap: .5rem;
}

.no-days {
  text-align: center;
  padding: 2rem 1rem;
  color: var(--gray-500);
  font-size: .875rem;
}

/* ── 天數區塊 ── */
.day-block {
  border: 1px solid var(--gray-200);
  border-radius: var(--radius);
  overflow: hidden;
}

.day-header {
  display: flex;
  align-items: center;
  gap: .5rem;
  padding: .4rem .625rem;
  background: var(--gray-50);
  border-bottom: 1px solid var(--gray-200);
}
.day-badge {
  font-size: .75rem;
  font-weight: 600;
  color: var(--primary);
  background: #eff6ff;
  padding: .15rem .45rem;
  border-radius: 4px;
}
.day-date {
  font-size: .75rem;
  color: var(--gray-500);
}

.day-body {
  padding: .375rem;
  display: flex;
  flex-direction: column;
  gap: .25rem;
  min-height: 48px;
  transition: background .15s;
}
.day-body.drag-over {
  background: #eff6ff;
  outline: 2px dashed var(--primary);
  outline-offset: -2px;
}
.day-empty {
  font-size: .75rem;
  color: var(--gray-400);
  text-align: center;
  padding: .5rem;
}

/* ── 景點列 ── */
.item-row {
  display: flex;
  align-items: center;
  gap: .375rem;
  padding: .375rem .5rem;
  background: #fff;
  border: 1px solid var(--gray-200);
  border-radius: 6px;
  cursor: grab;
  transition: opacity .15s, box-shadow .12s;
}
.item-row:hover { box-shadow: 0 1px 4px rgba(0,0,0,.08); }
.item-row.dragging { opacity: .4; }

.item-seq {
  width: 20px;
  height: 20px;
  background: var(--primary);
  color: #fff;
  font-size: .7rem;
  font-weight: 700;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.item-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}
.item-name {
  font-size: .8rem;
  font-weight: 500;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.item-time {
  font-size: .7rem;
  color: var(--gray-500);
}
.item-remove {
  width: 20px; height: 20px;
  background: none; border: none;
  font-size: .9rem; line-height: 1;
  color: var(--gray-400);
  cursor: pointer;
  border-radius: 4px;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.item-remove:hover { background: #fee2e2; color: #b91c1c; }

/* ── 車程 ── */
.travel-row {
  display: flex;
  justify-content: center;
  padding: .1rem 0;
}
.travel-pill {
  font-size: .7rem;
  color: var(--gray-500);
  background: var(--gray-50);
  border: 1px solid var(--gray-200);
  padding: .1rem .5rem;
  border-radius: 999px;
}
.travel-pending { color: var(--gray-400); }

/* ── 新增天 ── */
.add-day-btn {
  background: none;
  border: 1px dashed var(--gray-300);
  border-radius: var(--radius);
  padding: .5rem;
  font-size: .8rem;
  color: var(--gray-500);
  cursor: pointer;
  text-align: center;
  transition: all .12s;
}
.add-day-btn:hover {
  border-color: var(--primary);
  color: var(--primary);
  background: #eff6ff;
}

/* ── 右側地圖 ── */
.map-panel {
  position: relative;
  overflow: hidden;
}
.map-container { width: 100%; height: 100%; }
.map-err {
  position: absolute;
  top: 1rem; left: 50%;
  transform: translateX(-50%);
  background: #fff;
  border: 1px solid #fca5a5;
  color: #b91c1c;
  padding: .5rem 1rem;
  border-radius: var(--radius);
  font-size: .875rem;
  z-index: 20;
}

/* ── 加入行程浮窗 ── */
.add-popup {
  position: absolute;
  bottom: 1.5rem;
  left: 50%;
  transform: translateX(-50%);
  z-index: 30;
  width: 320px;
  max-width: calc(100% - 2rem);
}
.popup-box {
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0,0,0,.18);
  padding: 1rem;
  position: relative;
}
.popup-close {
  position: absolute;
  top: .625rem; right: .625rem;
  background: none; border: none;
  font-size: .9rem; cursor: pointer;
  color: var(--gray-400); width: 24px; height: 24px;
  border-radius: 50%;
}
.popup-close:hover { background: var(--gray-100); color: var(--gray-700); }
.popup-name {
  font-size: .9rem;
  font-weight: 600;
  margin: 0 1.5rem 0 0;
  line-height: 1.3;
}
.popup-sub {
  font-size: .75rem;
  color: var(--gray-500);
  margin: .15rem 0 0;
}
.popup-hint {
  font-size: .75rem;
  color: var(--gray-500);
  margin: .625rem 0 .375rem;
}
.popup-days {
  display: flex;
  flex-wrap: wrap;
  gap: .375rem;
}

/* ── 過場動畫 ── */
.popup-enter-active, .popup-leave-active { transition: opacity .2s, transform .2s; }
.popup-enter-from, .popup-leave-to { opacity: 0; transform: translateX(-50%) translateY(8px); }

/* ── RWD ── */
@media (max-width: 900px) {
  .td-layout {
    grid-template-columns: 1fr;
    grid-template-rows: auto 1fr;
    overflow: auto;
  }
  .left-panel {
    border-right: none;
    border-bottom: 1px solid var(--gray-200);
    max-height: 55vh;
  }
  .map-panel { height: 45vh; min-height: 280px; }
}
</style>
