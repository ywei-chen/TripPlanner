<template>
  <div v-if="pageLoading" class="page-state">載入中...</div>
  <div v-else-if="!tripsStore.currentTrip" class="page-state card">
    <p>找不到行程或尚未登入</p>
    <RouterLink to="/trips" class="btn btn-primary" style="margin-top:.75rem;">返回行程列表</RouterLink>
  </div>
  <div v-else class="td-layout">
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
          <button class="btn btn-outline btn-sm" @click="openShare">分享</button>
          <RouterLink :to="`/trips/${tripId}/edit`" class="btn btn-outline btn-sm">編輯</RouterLink>
          <button class="btn btn-danger btn-sm" @click="handleDelete">刪除</button>
        </div>
      </div>

      <!-- 從地圖點選的待加入景點 -->
      <div v-if="pendingAttr" class="pending-bar">
        <div
          class="pending-chip"
          draggable="true"
          @dragstart="onAttrDragStart"
          @dragend="onAttrDragEnd"
        >
          <span class="pending-icon">📍</span>
          <div class="pending-info">
            <span class="pending-name">{{ pendingAttr.name }}</span>
            <span v-if="pendingAttr.city" class="pending-city">{{ pendingAttr.city }}</span>
          </div>
          <span class="pending-drag-hint">拖至下方天數</span>
        </div>
        <div class="pending-day-btns">
          <button
            v-for="d in allDays"
            :key="d.dayNumber"
            class="btn btn-xs btn-outline"
            :disabled="addingDay !== null"
            @click="addToDay(pendingAttr!, d.dayNumber)"
          >第{{ d.dayNumber }}天</button>
          <button
            class="btn btn-xs btn-primary"
            :disabled="addingDay !== null"
            @click="addToDay(pendingAttr!, maxDay + 1)"
          >+新增天</button>
          <button class="pending-dismiss" @click="pendingAttr = null">✕</button>
        </div>
      </div>

      <div class="days-scroll">
        <div v-if="allDays.length === 0" class="no-days">
          <p>點擊右側地圖上的景點，即可加入行程</p>
        </div>

        <template v-for="day in allDays" :key="day.dayNumber">
          <div class="day-block">
            <div class="day-header">
              <div class="day-header-left">
                <span v-if="getDayDate(day.dayNumber)" class="day-date-main">{{ getDayDate(day.dayNumber) }}</span>
                <span class="day-badge">第 {{ day.dayNumber }} 天</span>
              </div>
            </div>

            <div
              class="day-body"
              :class="{
                'drag-over': dragOverDay === day.dayNumber,
                'drag-over-attr': dragOverAttrDay === day.dayNumber
              }"
              @dragover.prevent="onDragOverDay($event, day.dayNumber)"
              @dragleave.self="onDragLeaveDay"
              @drop.prevent="onDropToDay(day.dayNumber)"
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
      <!-- 地址搜尋列 -->
      <div class="map-search-bar">
        <input
          v-model="mapSearchText"
          class="map-search-input"
          placeholder="🔍 搜尋地址或地點..."
          @keyup.enter="doMapSearch"
        />
        <button class="map-search-btn" :disabled="mapSearching" @click="doMapSearch">
          {{ mapSearching ? '定位中' : '前往' }}
        </button>
      </div>

      <div v-if="mapError" class="map-err">⚠️ {{ mapError }}</div>
      <div ref="mapEl" class="map-container" />
    </div>
  </div>

  <!-- 分享 Modal -->
  <Teleport to="body">
    <div v-if="showShare" class="modal-overlay" @click.self="showShare = false">
      <div class="share-modal">
        <div class="share-modal-header">
          <h3>分享行程</h3>
          <button class="modal-close" @click="showShare = false">✕</button>
        </div>

        <div class="share-modal-body">
          <!-- 現有的分享連結 -->
          <div v-if="shareLinks.length > 0" class="share-links-list">
            <p class="share-section-label">現有分享連結</p>
            <div v-for="link in shareLinks" :key="link.id" class="share-link-row">
              <div class="share-link-info">
                <input :value="link.shareUrl" readonly class="share-url-input" @focus="($event.target as HTMLInputElement).select()" />
                <span class="share-meta">{{ link.viewCount }} 次查看</span>
              </div>
              <div class="share-link-actions">
                <button class="btn btn-sm btn-outline" @click="copyLink(link.shareUrl)">複製</button>
                <button class="btn btn-sm btn-danger" @click="deactivateLink(link.id)">停用</button>
              </div>
            </div>
          </div>
          <p v-else class="share-empty">尚未建立任何分享連結</p>

          <!-- 建立新連結 -->
          <div class="share-create">
            <p class="share-section-label">建立新連結</p>
            <button class="btn btn-primary" :disabled="creatingLink" @click="createLink">
              {{ creatingLink ? '建立中...' : '+ 建立分享連結' }}
            </button>
          </div>

          <p v-if="copyMsg" class="copy-msg">{{ copyMsg }}</p>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import { useTripsStore } from '@/stores/trips.store'
import { useAttractionsStore } from '@/stores/attractions.store'
import { shareService } from '@/services/share.service'
import type { TripItem } from '@/types/trip.types'
import type { Attraction } from '@/types/attraction.types'
import type { ShareLink } from '@/types/share.types'

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
const pageLoading = ref(true)
const pendingAttr = ref<Attraction | null>(null)
const addingDay = ref<number | null>(null)
const emptyDays = ref<Set<number>>(new Set())
const draggingItem = ref<TripItem | null>(null)
const draggingItemId = ref<string | null>(null)
const dragOverDay = ref<number | null>(null)
const isDraggingAttr = ref(false)
const dragOverAttrDay = ref<number | null>(null)
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

// ── 分享 ──────────────────────────────────────────────────────────────────────
const showShare = ref(false)
const shareLinks = ref<ShareLink[]>([])
const creatingLink = ref(false)
const copyMsg = ref('')

async function openShare() {
  showShare.value = true
  shareLinks.value = await shareService.getLinks(tripId)
}

async function createLink() {
  creatingLink.value = true
  try {
    const link = await shareService.create(tripId)
    shareLinks.value.unshift(link)
  } finally {
    creatingLink.value = false
  }
}

async function deactivateLink(linkId: string) {
  await shareService.deactivate(linkId)
  shareLinks.value = shareLinks.value.filter(l => l.id !== linkId)
}

function copyLink(url: string) {
  navigator.clipboard.writeText(url)
  copyMsg.value = '已複製連結！'
  setTimeout(() => { copyMsg.value = '' }, 2000)
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
    const newItem = await tripsStore.addItem(tripId, {
      attractionId: attr.id,
      dayNumber,
      sortOrder: targetDayItems.length + 1
    })
    // 後端 AddItemAsync 不 Include Attraction，立刻補上座標讓車程可計算
    const stored = tripsStore.currentTrip?.items.find(i => i.id === newItem.id)
    if (stored) {
      stored.attractionLatitude = attr.latitude != null ? Number(attr.latitude) : undefined
      stored.attractionLongitude = attr.longitude != null ? Number(attr.longitude) : undefined
      stored.attractionName = attr.name
    }
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

// ── 地圖地址搜尋 ──────────────────────────────────────────────────────────────
const mapSearchText = ref('')
const mapSearching = ref(false)

async function doMapSearch() {
  const q = mapSearchText.value.trim()
  if (!q || !map) return
  mapSearching.value = true
  try {
    const res = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(q)}&format=json&limit=1`,
      { headers: { 'Accept-Language': 'zh-TW,zh;q=0.9' } }
    )
    const data = await res.json()
    if (data.length > 0) {
      map.panTo({ lat: parseFloat(data[0].lat), lng: parseFloat(data[0].lon) })
      map.setZoom(13)
    }
  } catch { /* ignore */ } finally {
    mapSearching.value = false
  }
}

// ── 景點卡片拖曳（pending-bar → 天數區塊，同在左側面板）────────────────────
function onAttrDragStart(e: DragEvent) {
  isDraggingAttr.value = true
  e.dataTransfer?.setData('drag-type', 'attraction')
}

function onAttrDragEnd() {
  isDraggingAttr.value = false
  dragOverAttrDay.value = null
}

// ── 行程項目拖曳排序 ───────────────────────────────────────────────────────────
function onDragStart(item: TripItem) {
  draggingItem.value = item
  draggingItemId.value = item.id
}

function onDragEnd() {
  draggingItem.value = null
  draggingItemId.value = null
  dragOverDay.value = null
}

// ── 天數區塊 dragover / dragleave ─────────────────────────────────────────────
function onDragOverDay(e: DragEvent, dayNumber: number) {
  if (isDraggingAttr.value || e.dataTransfer?.types.includes('drag-type')) {
    dragOverAttrDay.value = dayNumber
    dragOverDay.value = null
  } else {
    dragOverDay.value = dayNumber
    dragOverAttrDay.value = null
  }
}

function onDragLeaveDay() {
  dragOverDay.value = null
  dragOverAttrDay.value = null
}

// ── 統一 drop 處理 ────────────────────────────────────────────────────────────
async function onDropToDay(targetDay: number) {
  dragOverDay.value = null
  dragOverAttrDay.value = null

  if (isDraggingAttr.value && pendingAttr.value) {
    isDraggingAttr.value = false
    await addToDay(pendingAttr.value, targetDay)
    return
  }

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
  try {
    await tripsStore.fetchById(tripId)
  } finally {
    pageLoading.value = false
  }
  if (!tripsStore.currentTrip) return
  await nextTick() // 等 v-else 的 DOM（含 map-container）出現後再初始化地圖
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
.page-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  padding: 3rem 1rem;
  font-size: .9rem;
  color: var(--gray-600);
  text-align: center;
}

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
  justify-content: space-between;
  padding: .5rem .625rem;
  background: var(--gray-50);
  border-bottom: 1px solid var(--gray-200);
}
.day-header-left {
  display: flex;
  align-items: center;
  gap: .5rem;
}
.day-date-main {
  font-size: .875rem;
  font-weight: 600;
  color: var(--gray-800);
}
.day-badge {
  font-size: .7rem;
  font-weight: 500;
  color: var(--primary);
  background: #eff6ff;
  padding: .1rem .4rem;
  border-radius: 4px;
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
.day-body.drag-over-attr {
  background: #f0fdf4;
  outline: 2px dashed #16a34a;
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

/* ── 待加入景點列（左側面板）── */
.pending-bar {
  border-bottom: 1px solid var(--gray-200);
  background: #f0fdf4;
  padding: .5rem .625rem;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: .375rem;
}
.pending-chip {
  display: flex;
  align-items: center;
  gap: .5rem;
  background: #fff;
  border: 2px solid #16a34a;
  border-radius: 8px;
  padding: .375rem .625rem;
  cursor: grab;
  user-select: none;
}
.pending-chip:active { cursor: grabbing; opacity: .75; }
.pending-icon { font-size: 1rem; flex-shrink: 0; }
.pending-info { flex: 1; min-width: 0; display: flex; flex-direction: column; }
.pending-name { font-size: .8rem; font-weight: 600; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }
.pending-city { font-size: .7rem; color: var(--gray-500); }
.pending-drag-hint { font-size: .7rem; color: #16a34a; white-space: nowrap; flex-shrink: 0; }
.pending-day-btns { display: flex; align-items: center; gap: .25rem; flex-wrap: wrap; }
.btn-xs { padding: .15rem .45rem; font-size: .72rem; border-radius: 4px; }
.pending-dismiss {
  margin-left: auto;
  background: none; border: none;
  font-size: .8rem; cursor: pointer;
  color: var(--gray-400); padding: .15rem .3rem;
  border-radius: 4px;
  flex-shrink: 0;
}
.pending-dismiss:hover { background: #fee2e2; color: #b91c1c; }

/* ── 右側地圖 ── */
.map-panel {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.map-search-bar {
  position: absolute;
  top: .75rem;
  left: 50%;
  transform: translateX(-50%);
  z-index: 20;
  display: flex;
  gap: .375rem;
  width: min(380px, calc(100% - 2rem));
}
.map-search-input {
  flex: 1;
  padding: .5rem .75rem;
  border: none;
  border-radius: 8px;
  font-size: .85rem;
  box-shadow: 0 2px 8px rgba(0,0,0,.2);
  outline: none;
}
.map-search-input:focus { box-shadow: 0 2px 12px rgba(37,99,235,.3); }
.map-search-btn {
  padding: .5rem .875rem;
  background: var(--primary);
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: .8rem;
  cursor: pointer;
  white-space: nowrap;
  box-shadow: 0 2px 8px rgba(0,0,0,.2);
}
.map-search-btn:disabled { opacity: .6; cursor: not-allowed; }
.map-container { flex: 1; }
.map-err {
  position: absolute;
  top: 4rem; left: 50%;
  transform: translateX(-50%);
  background: #fff;
  border: 1px solid #fca5a5;
  color: #b91c1c;
  padding: .5rem 1rem;
  border-radius: var(--radius);
  font-size: .875rem;
  z-index: 20;
}

/* ── 分享 Modal ── */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
}
.share-modal {
  background: #fff;
  border-radius: 12px;
  width: 100%;
  max-width: 480px;
  box-shadow: 0 12px 40px rgba(0,0,0,.2);
  overflow: hidden;
}
.share-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--gray-200);
}
.share-modal-header h3 { margin: 0; font-size: 1rem; }
.modal-close {
  background: none;
  border: none;
  font-size: .9rem;
  cursor: pointer;
  color: var(--gray-400);
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.modal-close:hover { background: var(--gray-100); color: var(--gray-700); }
.share-modal-body {
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.share-section-label {
  font-size: .75rem;
  font-weight: 600;
  color: var(--gray-500);
  text-transform: uppercase;
  letter-spacing: .04em;
  margin: 0 0 .5rem;
}
.share-links-list { display: flex; flex-direction: column; gap: .625rem; }
.share-link-row {
  display: flex;
  flex-direction: column;
  gap: .375rem;
  padding: .625rem;
  border: 1px solid var(--gray-200);
  border-radius: var(--radius);
}
.share-link-info { display: flex; align-items: center; gap: .5rem; }
.share-url-input {
  flex: 1;
  font-size: .8rem;
  color: var(--gray-700);
  border: 1px solid var(--gray-200);
  border-radius: 4px;
  padding: .25rem .5rem;
  background: var(--gray-50);
  cursor: text;
  min-width: 0;
}
.share-meta { font-size: .75rem; color: var(--gray-400); white-space: nowrap; }
.share-link-actions { display: flex; gap: .375rem; }
.share-empty { font-size: .875rem; color: var(--gray-500); margin: 0; }
.share-create { display: flex; flex-direction: column; }
.copy-msg {
  font-size: .8rem;
  color: #16a34a;
  text-align: center;
  margin: 0;
}

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
