<template>
  <div v-if="pageLoading" class="page-state">載入中...</div>
  <div v-else-if="!tripsStore.currentTrip" class="page-state card">
    <p>找不到行程或尚未登入</p>
    <RouterLink to="/trips" class="btn btn-primary" style="margin-top:.75rem;">返回行程列表</RouterLink>
  </div>
  <div v-else class="td-layout">

    <!-- ── 左側 ── -->
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

      <!-- 暫存景點列 -->
      <div v-if="savedPlaces.length > 0" class="saved-panel">
        <div class="saved-header">
          <span class="saved-title">暫存景點 <span class="saved-count">{{ savedPlaces.length }}</span></span>
          <button class="saved-clear-all" @click="savedPlaces = []">清除全部</button>
        </div>
        <draggable
          :list="savedPlaces"
          :group="{ name: 'trip', pull: 'clone', put: false }"
          :clone="cloneSavedPlace"
          :sort="false"
          item-key="key"
          ghost-class="ghost-item"
        >
          <template #item="{ element: place }">
            <div class="saved-chip">
              <span class="drag-handle">⠿</span>
              <div class="saved-info">
                <span class="saved-name">{{ place.name }}</span>
                <span v-if="place.city" class="saved-city">{{ place.city }}</span>
              </div>
              <div class="saved-day-btns">
                <button
                  v-for="d in allDays"
                  :key="d.dayNumber"
                  class="btn btn-xs btn-outline"
                  :disabled="addingDay !== null"
                  @click.stop="addSavedPlaceToDay(place, d.dayNumber)"
                >{{ d.dayNumber }}</button>
                <button
                  class="btn btn-xs btn-primary"
                  :disabled="addingDay !== null"
                  @click.stop="addSavedPlaceToDay(place, maxDay + 1)"
                >+</button>
                <button class="saved-remove" @click.stop="removeSavedPlace(place.key)">✕</button>
              </div>
            </div>
          </template>
        </draggable>
      </div>
      <div v-else class="saved-empty-hint">
        點擊右側地圖上的景點或 Google 地標加入暫存，再拖曳到天數
      </div>

      <!-- 天數 -->
      <div class="days-scroll">
        <template v-for="day in allDays" :key="day.dayNumber">
          <div class="day-block">
            <div class="day-header">
              <div class="day-header-left">
                <span v-if="getDayDate(day.dayNumber)" class="day-date-main">{{ getDayDate(day.dayNumber) }}</span>
                <span class="day-badge">第 {{ day.dayNumber }} 天</span>
              </div>
            </div>

            <draggable
              class="day-body"
              :list="getDayItems(day.dayNumber)"
              :group="{ name: 'trip', pull: true, put: true }"
              item-key="id"
              ghost-class="ghost-item"
              @change="onDayChange($event, day.dayNumber)"
            >
              <template #item="{ element: item, index: idx }">
                <div>
                  <div class="item-row">
                    <span class="item-seq">{{ idx + 1 }}</span>
                    <div class="item-main">
                      <span class="item-name">{{ item.attractionName || item.customName || '自訂景點' }}</span>
                      <span v-if="item.startTime" class="item-time">{{ item.startTime }}</span>
                    </div>
                    <button class="item-remove" title="移除" @click.stop="doRemoveItem(item.id)">×</button>
                  </div>
                  <div v-if="idx < getDayItems(day.dayNumber).length - 1" class="travel-row">
                    <div class="travel-line-col"></div>
                    <span v-if="getTravelTime(item, getDayItems(day.dayNumber)[idx + 1])" class="travel-pill">
                      {{ getTravelTime(item, getDayItems(day.dayNumber)[idx + 1]) }}
                    </span>
                    <span v-else-if="canCalcTravel(item, getDayItems(day.dayNumber)[idx + 1])" class="travel-pill travel-pending">
                      計算中...
                    </span>
                  </div>
                </div>
              </template>
              <template #footer>
                <div v-if="getDayItems(day.dayNumber).length === 0" class="day-empty">
                  從暫存列表拖曳景點到此
                </div>
              </template>
            </draggable>
          </div>
        </template>

        <button
          class="add-day-btn"
          :disabled="maxDay >= maxAllowedDays"
          :title="maxDay >= maxAllowedDays ? `行程日期只有 ${maxAllowedDays} 天` : ''"
          @click="addEmptyDay"
        >
          <template v-if="maxDay >= maxAllowedDays">
            已達行程上限（{{ maxAllowedDays }} 天）
          </template>
          <template v-else>
            + 新增第 {{ maxDay + 1 }} 天
          </template>
        </button>
      </div>
    </div>

    <!-- ── 右側：Google Maps ── -->
    <div class="map-panel">
      <div class="map-search-bar">
        <div class="map-search-field">
          <svg class="map-search-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd" />
          </svg>
          <input
            v-model="mapSearchText"
            class="map-search-input"
            placeholder="搜尋地址或地點..."
            @keyup.enter="doMapSearch"
          />
        </div>
        <button class="map-search-btn" :disabled="mapSearching" @click="doMapSearch">
          {{ mapSearching ? '定位中' : '前往' }}
        </button>
      </div>

      <div v-if="mapSearchNoResult" class="map-err">找不到「{{ mapSearchText }}」，請換個關鍵字</div>
      <div v-else-if="mapError" class="map-err">⚠️ {{ mapError }}</div>
      <button class="route-toggle-btn" @click="showRoute = !showRoute">
        {{ showRoute ? '隱藏路線' : '顯示路線' }}
      </button>
      <div class="map-hint">點擊地圖景點或 Google 地標加入左側暫存列表</div>
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
import draggable from 'vuedraggable'
import { useTripsStore } from '@/stores/trips.store'
import { useAttractionsStore } from '@/stores/attractions.store'
import { shareService } from '@/services/share.service'
import type { TripItem } from '@/types/trip.types'
import type { Attraction } from '@/types/attraction.types'
import type { ShareLink } from '@/types/share.types'

// ── 暫存景點型別 ──────────────────────────────────────────────────────────────
interface SavedPlace {
  key: string           // 唯一識別（DB UUID 或 Google Place ID）
  name: string
  city?: string
  address?: string
  latitude?: number
  longitude?: number
  attractionId?: string // 僅 DB 景點設定（UUID）
}

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
const attrMarkers = new Map<string, google.maps.Marker>()
const itemMarkers = new Map<string, google.maps.Marker>()
const routeRenderers: google.maps.DirectionsRenderer[] = []
const DAY_COLORS = ['#2563eb','#16a34a','#dc2626','#9333ea','#ea580c','#0891b2','#b45309']
const showRoute = ref(true)

// ── 狀態 ──────────────────────────────────────────────────────────────────────
const pageLoading = ref(true)
const savedPlaces = ref<SavedPlace[]>([])
const addingDay = ref<number | null>(null)
const emptyDays = ref<Set<number>>(new Set())
const travelTimes = ref<Record<string, string>>({})

// ── 本地拖曳狀態（vuedraggable）──────────────────────────────────────────────
type LocalDay = { dayNumber: number; items: TripItem[] }
const localDays = ref<LocalDay[]>([])
let syncing = false
let reorderTimer: ReturnType<typeof setTimeout> | null = null

// ── 行程資料 ──────────────────────────────────────────────────────────────────
const trip = computed(() => tripsStore.currentTrip!)

const groupedByDay = computed(() => {
  const t = tripsStore.currentTrip
  if (!t) return []
  const dayMap = new Map<number, TripItem[]>()
  for (const item of t.items) {
    if (!dayMap.has(item.dayNumber)) dayMap.set(item.dayNumber, [])
    dayMap.get(item.dayNumber)!.push(item)
  }
  for (const [, items] of dayMap) {
    items.sort((a, b) => a.sortOrder - b.sortOrder)
  }
  return [...dayMap.entries()].sort((a, b) => a[0] - b[0])
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

const maxAllowedDays = computed(() => {
  const t = tripsStore.currentTrip
  if (!t?.startDate || !t?.endDate) return Infinity
  const start = new Date(t.startDate)
  const end = new Date(t.endDate)
  return Math.round((end.getTime() - start.getTime()) / 86400000) + 1
})

function syncLocalDays() {
  localDays.value = allDays.value.map(d => ({
    dayNumber: d.dayNumber,
    items: [...d.items]
  }))
}

function getDayItems(dayNumber: number): TripItem[] {
  return localDays.value.find(d => d.dayNumber === dayNumber)?.items ?? []
}

watch(allDays, () => {
  if (!syncing) syncLocalDays()
}, { deep: true, immediate: true })

function getDayDate(dayNumber: number): string {
  const t = tripsStore.currentTrip
  if (!t?.startDate) return ''
  const start = new Date(t.startDate)
  start.setDate(start.getDate() + dayNumber - 1)
  return start.toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric', weekday: 'short' })
}

function addEmptyDay() {
  if (maxDay.value >= maxAllowedDays.value) return
  emptyDays.value = new Set([...emptyDays.value, maxDay.value + 1])
}

// ── 暫存景點操作 ──────────────────────────────────────────────────────────────
function addToSavedPlaces(place: SavedPlace) {
  if (!savedPlaces.value.find(p => p.key === place.key)) {
    savedPlaces.value.push(place)
  }
}

function removeSavedPlace(key: string) {
  savedPlaces.value = savedPlaces.value.filter(p => p.key !== key)
}

function cloneSavedPlace(item: SavedPlace): SavedPlace & { _fromSaved: true } {
  return { ...item, _fromSaved: true }
}

// ── 拖曳事件（vuedraggable @change）─────────────────────────────────────────
// eslint-disable-next-line @typescript-eslint/no-explicit-any
async function onDayChange(event: any, dayNumber: number) {
  const added = event.added?.element
  if (added?._fromSaved) {
    // 從暫存清單克隆投入：移除假項目，呼叫 API
    const day = localDays.value.find(d => d.dayNumber === dayNumber)
    if (day) {
      const fakeIdx = day.items.findIndex((i: unknown) => (i as { _fromSaved?: boolean })._fromSaved)
      if (fakeIdx !== -1) day.items.splice(fakeIdx, 1)
    }
    await addSavedPlaceToDay(added as SavedPlace & { _fromSaved: true }, dayNumber)
    return
  }
  scheduleReorder()
}

function scheduleReorder() {
  if (reorderTimer) clearTimeout(reorderTimer)
  reorderTimer = setTimeout(async () => {
    const allItems = localDays.value.flatMap(d =>
      d.items.map((item, idx) => ({
        itemId: item.id,
        dayNumber: d.dayNumber,
        sortOrder: idx + 1
      }))
    )
    syncing = true
    try {
      await tripsStore.reorderItems(tripId, { items: allItems })
      updateItemMarkers()
      computeTravelTimes()
    } finally {
      syncing = false
      syncLocalDays()
    }
  }, 500)
}

// ── 加入天數（從暫存清單）────────────────────────────────────────────────────
async function addSavedPlaceToDay(place: SavedPlace, dayNumber: number) {
  addingDay.value = dayNumber
  const targetDayItems = allDays.value.find(d => d.dayNumber === dayNumber)?.items ?? []
  try {
    const isDbAttr = !!place.attractionId
    const newItem = await tripsStore.addItem(tripId, {
      attractionId: place.attractionId,
      customName: !isDbAttr ? place.name : undefined,
      dayNumber,
      sortOrder: targetDayItems.length + 1,
      customLatitude: !isDbAttr && place.latitude != null ? place.latitude : undefined,
      customLongitude: !isDbAttr && place.longitude != null ? place.longitude : undefined,
    })
    // 立刻補上座標讓車程可計算（後端 AddItemAsync 不 Include Attraction）
    const stored = tripsStore.currentTrip?.items.find(i => i.id === newItem.id)
    if (stored && place.latitude != null && place.longitude != null) {
      stored.attractionLatitude = place.latitude
      stored.attractionLongitude = place.longitude
      stored.attractionName = place.name
    }
    if (emptyDays.value.has(dayNumber)) {
      emptyDays.value = new Set([...emptyDays.value].filter(n => n !== dayNumber))
    }
    removeSavedPlace(place.key)
    await nextTick()
    updateItemMarkers()
    computeTravelTimes()
  } finally {
    addingDay.value = null
  }
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

// ── 地圖地址搜尋 ──────────────────────────────────────────────────────────────
const mapSearchText = ref('')
const mapSearching = ref(false)
const mapSearchNoResult = ref(false)
let searchMarker: google.maps.Marker | null = null

async function doMapSearch() {
  const q = mapSearchText.value.trim()
  if (!q || !map) return
  mapSearching.value = true
  mapSearchNoResult.value = false
  try {
    const res = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(q)}&format=json&limit=1`,
      { headers: { 'Accept-Language': 'zh-TW,zh;q=0.9' } }
    )
    const data = await res.json()
    if (data.length > 0) {
      const lat = parseFloat(data[0].lat)
      const lng = parseFloat(data[0].lon)
      const name = data[0].display_name?.split(',')[0]?.trim() || q
      const address = data[0].display_name || ''

      map.panTo({ lat, lng })
      map.setZoom(15)

      // 移除上一個搜尋標記
      searchMarker?.setMap(null)

      // 放一個橘色搜尋標記，點擊後加入暫存
      const svg = encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="22" height="22"><circle cx="11" cy="11" r="9" fill="#f97316" fill-opacity="0.95" stroke="white" stroke-width="2.5"/><text x="11" y="15" text-anchor="middle" font-size="11" fill="white" font-family="system-ui" font-weight="bold">+</text></svg>')
      searchMarker = new google.maps.Marker({
        position: { lat, lng },
        map: map!,
        title: name,
        icon: { url: `data:image/svg+xml,${svg}`, scaledSize: new google.maps.Size(22, 22), anchor: new google.maps.Point(11, 11) },
        zIndex: 30,
      })
      searchMarker.addListener('click', () => {
        addToSavedPlaces({ key: `search:${lat},${lng}`, name, address, latitude: lat, longitude: lng })
        searchMarker?.setMap(null)
        searchMarker = null
      })

      // 自動加入暫存
      addToSavedPlaces({ key: `search:${lat},${lng}`, name, address, latitude: lat, longitude: lng })
      mapSearchText.value = ''
    } else {
      mapSearchNoResult.value = true
    }
  } catch { /* ignore */ } finally {
    mapSearching.value = false
  }
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
    ;(window as Record<string, unknown>)['gm_authFailure'] = () => {
      mapError.value = 'Google Maps API Key 無效或未啟用帳單，地圖標記功能無法使用'
    }
    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?key=${import.meta.env.VITE_GOOGLE_MAPS_KEY}&callback=${cbName}&language=zh-TW&region=TW&libraries=places`
    script.async = true
    script.onerror = () => reject(new Error('Google Maps 載入失敗'))
    document.head.appendChild(script)
  })
}

// SVG 資料圖示（比 SymbolPath 更可靠）
function makeAttrIcon(): google.maps.Icon {
  const svg = encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18"><circle cx="9" cy="9" r="7" fill="#4b5563" fill-opacity="0.9" stroke="white" stroke-width="2.5"/></svg>')
  return {
    url: `data:image/svg+xml,${svg}`,
    scaledSize: new google.maps.Size(18, 18),
    anchor: new google.maps.Point(9, 9),
  }
}

function updateAttrMarkers(attractions: Attraction[]) {
  if (!map || !window.google?.maps) return
  attrMarkers.forEach(m => m.setMap(null))
  attrMarkers.clear()

  const tripAttrIds = new Set(
    (tripsStore.currentTrip?.items ?? []).map(i => i.attractionId).filter(Boolean)
  )
  const icon = makeAttrIcon()

  attractions
    .filter(a => a.latitude && a.longitude && !tripAttrIds.has(a.id))
    .forEach(a => {
      const marker = new google.maps.Marker({
        position: { lat: Number(a.latitude!), lng: Number(a.longitude!) },
        map: map!,
        title: a.name,
        icon,
        zIndex: 10,
      })
      marker.addListener('click', () => {
        activeInfoWindow?.close()
        addToSavedPlaces({
          key: a.id,
          name: a.name,
          city: a.city,
          address: a.address,
          latitude: a.latitude != null ? Number(a.latitude) : undefined,
          longitude: a.longitude != null ? Number(a.longitude) : undefined,
          attractionId: a.id,
        })
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
      zIndex: 20,
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

  if (attractionsStore.result) {
    updateAttrMarkers(attractionsStore.result.items)
  }
  updateRoutePaths()
}

async function updateRoutePaths() {
  routeRenderers.forEach(r => r.setMap(null))
  routeRenderers.length = 0
  if (!map || !window.google?.maps) return

  const ds = new google.maps.DirectionsService()

  for (const day of allDays.value) {
    const pts = day.items
      .filter(i => i.attractionLatitude && i.attractionLongitude)
      .sort((a, b) => a.sortOrder - b.sortOrder)
    if (pts.length < 2) continue

    const color = DAY_COLORS[(day.dayNumber - 1) % DAY_COLORS.length]
    const renderer = new google.maps.DirectionsRenderer({
      map: showRoute.value ? map! : null,
      suppressMarkers: true,
      polylineOptions: { strokeColor: color, strokeWeight: 4, strokeOpacity: 0.75 },
    })
    routeRenderers.push(renderer)

    const toLatLng = (i: TripItem) => ({ lat: Number(i.attractionLatitude), lng: Number(i.attractionLongitude) })
    await new Promise<void>(resolve => {
      ds.route({
        origin: toLatLng(pts[0]),
        destination: toLatLng(pts[pts.length - 1]),
        waypoints: pts.slice(1, -1).map(p => ({ location: toLatLng(p), stopover: true })),
        travelMode: google.maps.TravelMode.DRIVING,
      }, (result, status) => {
        if (status === 'OK' && result) renderer.setDirections(result)
        resolve()
      })
    })
  }
}

watch(showRoute, () => {
  routeRenderers.forEach(r => r.setMap(showRoute.value ? map! : null))
})

async function initMap() {
  if (!mapEl.value) {
    console.error('[TripDetail] mapEl is null, skipping initMap')
    return
  }
  try {
    await loadGoogleMaps()
    map = new google.maps.Map(mapEl.value, {
      center: { lat: 23.5, lng: 121 },
      zoom: 7,
      mapTypeControl: false,
      fullscreenControl: true,
      streetViewControl: false,
      zoomControl: true,
    })

    // 偵測 Google Maps POI 點擊（餐廳、景點、地標等）
    map.addListener('click', (e: google.maps.IconMouseEvent) => {
      if (!e.placeId) return
      e.stop() // 阻止預設的 InfoWindow
      const svc = new google.maps.places.PlacesService(map!)
      svc.getDetails(
        { placeId: e.placeId, fields: ['name', 'geometry', 'formatted_address', 'vicinity'] },
        (place, status) => {
          if (status !== google.maps.places.PlacesServiceStatus.OK || !place?.geometry?.location) return
          addToSavedPlaces({
            key: e.placeId!,
            name: place.name ?? '未知地點',
            city: place.vicinity,
            address: place.formatted_address,
            latitude: place.geometry.location.lat(),
            longitude: place.geometry.location.lng(),
            attractionId: undefined,
          })
        }
      )
    })

    // 載入景點標記
    await attractionsStore.search({ pageSize: 100, page: 1 })
    if (attractionsStore.result) {
      updateAttrMarkers(attractionsStore.result.items)
    }

    updateItemMarkers()

    // 聚焦到行程已有景點的範圍
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

watch(() => tripsStore.currentTrip?.items, () => {
  updateItemMarkers()
}, { deep: true })

onMounted(async () => {
  try {
    await tripsStore.fetchById(tripId)
  } finally {
    pageLoading.value = false
  }
  if (!tripsStore.currentTrip) return
  await nextTick()
  await initMap()
})

onUnmounted(() => {
  if (reorderTimer) clearTimeout(reorderTimer)
  attrMarkers.forEach(m => m.setMap(null))
  itemMarkers.forEach(m => m.setMap(null))
  attrMarkers.clear()
  itemMarkers.clear()
  routeRenderers.forEach(r => r.setMap(null))
  routeRenderers.length = 0
  activeInfoWindow?.close()
  searchMarker?.setMap(null)
  map = null
})
</script>

<style scoped>
.page-state {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  flex: 1; padding: 3rem 1rem; font-size: .9rem; color: var(--gray-500); text-align: center;
}

.td-layout { display: grid; grid-template-columns: 390px 1fr; flex: 1; overflow: hidden; }

/* ── 左側 ── */
.left-panel { display: flex; flex-direction: column; overflow: hidden; border-right: 1px solid var(--gray-200); background: var(--gray-50); }

.trip-header {
  display: flex; align-items: flex-start; justify-content: space-between;
  padding: 1rem; border-bottom: 1px solid var(--gray-200); flex-shrink: 0; gap: .5rem;
  background: var(--surface);
}
.trip-title { font-size: 1.0625rem; font-weight: 700; line-height: 1.3; margin: 0; color: var(--gray-900); }
.trip-dates {
  font-size: .775rem; color: var(--gray-400); margin: .3rem 0 0;
  display: flex; align-items: center; gap: .25rem;
}
.header-btns { display: flex; gap: .375rem; flex-shrink: 0; }
.btn-sm { padding: .3rem .7rem; font-size: .78rem; border-radius: 8px; }

/* ── 暫存景點列 ── */
.saved-panel {
  border-bottom: 1px solid var(--gray-200);
  background: var(--surface);
  padding: .625rem .75rem;
  flex-shrink: 0; display: flex; flex-direction: column; gap: .375rem;
  max-height: 240px; overflow-y: auto;
}
.saved-header { display: flex; align-items: center; justify-content: space-between; flex-shrink: 0; }
.saved-title { font-size: .75rem; font-weight: 700; color: var(--gray-600); letter-spacing: .02em; text-transform: uppercase; }
.saved-count {
  display: inline-flex; align-items: center; justify-content: center;
  background: var(--primary); color: #1a0e02;
  font-size: .62rem; font-weight: 700; min-width: 17px; height: 17px;
  border-radius: 999px; padding: 0 5px; margin-left: .3rem;
}
.saved-clear-all {
  background: none; border: none; font-size: .72rem; color: var(--gray-400); cursor: pointer;
  padding: .15rem .4rem; border-radius: 6px; transition: all .12s;
}
.saved-clear-all:hover { background: rgba(248,113,113,.12); color: var(--danger); }

.saved-chip {
  display: flex; align-items: center; gap: .375rem;
  padding: .4rem .5rem; background: var(--gray-50);
  border: 1px solid var(--gray-200); border-radius: 8px; cursor: grab;
  transition: all .12s;
}
.saved-chip:hover { box-shadow: 0 2px 8px rgba(232,160,69,.15); border-color: var(--primary); background: var(--surface-2); }
.saved-chip:active { cursor: grabbing; }

.drag-handle { font-size: 1rem; color: var(--gray-300); cursor: grab; flex-shrink: 0; user-select: none; }
.saved-info { flex: 1; min-width: 0; display: flex; flex-direction: column; }
.saved-name { font-size: .8rem; font-weight: 600; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; color: var(--gray-800); }
.saved-city { font-size: .7rem; color: var(--gray-400); margin-top: .05rem; }

.saved-day-btns { display: flex; align-items: center; gap: .2rem; flex-shrink: 0; }
.btn-xs { padding: .15rem .4rem; font-size: .7rem; border-radius: 5px; min-width: 24px; text-align: center; font-weight: 600; }
.saved-remove {
  background: none; border: none; font-size: .85rem; color: var(--gray-300); cursor: pointer;
  padding: .1rem .25rem; border-radius: 4px; line-height: 1; transition: all .12s;
}
.saved-remove:hover { background: rgba(248,113,113,.12); color: var(--danger); }

.saved-empty-hint {
  padding: .75rem 1rem; font-size: .78rem; color: var(--gray-400); text-align: center;
  border-bottom: 1px solid var(--gray-200); flex-shrink: 0; background: var(--surface); line-height: 1.5;
}

/* ── 天數 ── */
.days-scroll { flex: 1; overflow-y: auto; padding: .75rem; display: flex; flex-direction: column; gap: .625rem; }

.day-block { border: 1px solid var(--gray-200); border-radius: var(--radius); overflow: hidden; background: var(--surface); }
.day-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: .5rem .75rem; background: var(--gray-50); border-bottom: 1px solid var(--gray-200);
}
.day-header-left { display: flex; align-items: center; gap: .5rem; }
.day-date-main { font-size: .875rem; font-weight: 700; color: var(--gray-800); }
.day-badge {
  font-size: .68rem; font-weight: 700; color: var(--primary);
  background: var(--primary-light); padding: .125rem .45rem; border-radius: 999px;
  letter-spacing: .01em;
}
.day-body { padding: .375rem; display: flex; flex-direction: column; gap: .3rem; min-height: 52px; transition: background .15s; }
.day-empty { font-size: .75rem; color: var(--gray-300); text-align: center; padding: .625rem; letter-spacing: .01em; }

.ghost-item { opacity: .25; background: var(--primary-light); border: 1px dashed var(--primary-border); border-radius: 8px; }

/* ── 景點列 ── */
.item-row {
  display: flex; align-items: center; gap: .4rem; padding: .4rem .5rem;
  background: var(--surface-0); border: 1px solid var(--gray-200); border-radius: 8px; cursor: grab;
  transition: all .12s;
}
.item-row:hover { box-shadow: 0 2px 8px rgba(0,0,0,.07); border-color: var(--gray-300); }
.item-row:active { cursor: grabbing; }
.item-seq {
  width: 22px; height: 22px;
  background: var(--primary); color: #fff;
  font-size: .68rem; font-weight: 700; border-radius: 50%;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
  flex-shrink: 0;
}
.item-main { flex: 1; min-width: 0; display: flex; flex-direction: column; }
.item-name { font-size: .8125rem; font-weight: 600; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; color: var(--gray-800); }
.item-time { font-size: .7rem; color: var(--gray-400); margin-top: .05rem; }
.item-remove {
  width: 22px; height: 22px; background: none; border: none;
  font-size: .9rem; line-height: 1; color: var(--gray-300); cursor: pointer;
  border-radius: 5px; display: flex; align-items: center; justify-content: center; flex-shrink: 0;
  transition: all .12s;
}
.item-remove:hover { background: rgba(248,113,113,.12); color: var(--danger); }

/* ── 車程 ── */
.travel-row {
  display: flex; align-items: center;
  padding: 0 .5rem; gap: .4rem; min-height: 24px;
}
.travel-line-col {
  width: 22px; flex-shrink: 0; align-self: stretch;
  position: relative;
}
.travel-line-col::before {
  content: '';
  position: absolute; left: 50%; top: 0; bottom: 0;
  border-left: 1.5px dashed var(--gray-200);
}
.travel-pill {
  font-size: .68rem; color: var(--gray-500);
  letter-spacing: .01em; white-space: nowrap;
}
.travel-pending { color: var(--gray-400); font-style: italic; }

/* ── 新增天 ── */
.add-day-btn {
  background: none; border: 1.5px dashed var(--gray-200); border-radius: var(--radius);
  padding: .5rem; font-size: .8rem; color: var(--gray-400); cursor: pointer; text-align: center;
  transition: all .15s; font-weight: 500;
}
.add-day-btn:hover:not(:disabled) { border-color: var(--primary); color: var(--primary); background: var(--primary-light); }
.add-day-btn:disabled { opacity: .55; cursor: not-allowed; border-style: solid; border-color: var(--gray-200); }

/* ── 地圖 ── */
.map-panel { position: relative; overflow: hidden; display: flex; flex-direction: column; }
.map-search-bar {
  position: absolute; top: .875rem; left: 50%; transform: translateX(-50%);
  z-index: 20; display: flex; align-items: center;
  width: min(420px, calc(100% - 2rem));
  background: rgba(255,255,255,.94);
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  border: 1.5px solid rgba(255,255,255,.8);
  border-radius: 14px;
  box-shadow: 0 4px 20px rgba(80,45,10,.14), 0 1px 0 rgba(255,255,255,.6) inset;
  overflow: hidden;
  transition: background .2s, border-color .2s, box-shadow .2s var(--ease-out);
}
.map-search-bar:hover {
  background: rgba(255,248,238,.98);
  border-color: rgba(201,120,32,.3);
  box-shadow: 0 6px 28px rgba(80,45,10,.2), 0 1px 0 rgba(255,255,255,.7) inset;
}
.map-search-bar:focus-within {
  background: rgba(255,249,240,.99);
  border-color: var(--primary);
  box-shadow: 0 6px 28px rgba(201,120,32,.2), 0 0 0 3px rgba(201,120,32,.12), 0 1px 0 rgba(255,255,255,.7) inset;
}
.map-search-field {
  flex: 1; position: relative; display: flex; align-items: center;
}
.map-search-icon {
  position: absolute; left: .875rem; width: 15px; height: 15px;
  color: var(--gray-300); pointer-events: none; flex-shrink: 0;
  transition: color .2s;
}
.map-search-bar:hover .map-search-icon { color: var(--gray-500); }
.map-search-bar:focus-within .map-search-icon { color: var(--primary); }
.map-search-input {
  width: 100%; padding: .6875rem .75rem .6875rem 2.5rem;
  border: none; outline: none;
  background: transparent; color: var(--gray-900); font-size: .875rem;
  transition: color .2s;
}
.map-search-input::placeholder { color: var(--gray-300); transition: color .2s; }
.map-search-bar:hover .map-search-input::placeholder { color: var(--gray-400); }
.map-search-bar:focus-within .map-search-input::placeholder { color: var(--gray-400); }
.map-search-btn {
  flex-shrink: 0; margin: .3rem .3rem .3rem 0;
  padding: .45rem 1rem;
  background: var(--primary); color: #1a0e02;
  border: none; border-radius: 10px; font-size: .8rem; font-weight: 700;
  cursor: pointer; white-space: nowrap;
  transition: background .14s, transform .09s var(--ease-out);
}
.map-search-btn:hover { background: var(--primary-hover); }
.map-search-btn:active { transform: scale(.95); }
.map-search-btn:disabled { opacity: .5; cursor: not-allowed; transform: none; }
.map-hint {
  position: absolute; bottom: .875rem; left: 50%; transform: translateX(-50%);
  z-index: 20; background: rgba(0,0,0,.5); backdrop-filter: blur(4px);
  color: #fff; font-size: .72rem; padding: .3rem .875rem; border-radius: 999px;
  white-space: nowrap; pointer-events: none; letter-spacing: .01em;
}
.map-container { flex: 1; }

.route-toggle-btn {
  position: absolute; top: .875rem; right: .875rem; z-index: 20;
  background: rgba(255,255,255,.92); backdrop-filter: blur(8px);
  border: 1px solid var(--gray-200); border-radius: 10px;
  padding: .4rem .875rem; font-size: .78rem; font-weight: 600; color: var(--gray-700);
  cursor: pointer; box-shadow: 0 2px 8px rgba(80,45,10,.15); transition: all .12s;
}
.route-toggle-btn:hover { background: var(--surface); color: var(--gray-900); box-shadow: 0 3px 12px rgba(80,45,10,.2); }

.map-err {
  position: absolute; top: 4rem; left: 50%; transform: translateX(-50%);
  background: var(--surface); border: 1px solid rgba(248,113,113,.4); color: var(--danger);
  padding: .5rem 1rem; border-radius: var(--radius); font-size: .875rem; z-index: 20;
  box-shadow: var(--shadow); white-space: nowrap;
}

/* ── 分享 Modal ── */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.48); backdrop-filter: blur(3px);
  display: flex; align-items: center; justify-content: center; z-index: 200; padding: 1rem;
}
.share-modal {
  background: var(--surface); border-radius: var(--radius-lg);
  width: 100%; max-width: 480px;
  box-shadow: var(--shadow-lg); overflow: hidden;
  border: 1px solid var(--surface-3);
}
.share-modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 1.125rem 1.25rem; border-bottom: 1px solid var(--gray-200);
}
.share-modal-header h3 { margin: 0; font-size: 1rem; font-weight: 700; }
.modal-close {
  background: none; border: none; font-size: .9rem; cursor: pointer; color: var(--gray-400);
  width: 30px; height: 30px; border-radius: 8px; display: flex; align-items: center; justify-content: center; transition: all .12s;
}
.modal-close:hover { background: var(--gray-100); color: var(--gray-700); }
.share-modal-body { padding: 1.25rem; display: flex; flex-direction: column; gap: 1rem; }
.share-section-label {
  font-size: .72rem; font-weight: 700; color: var(--gray-400);
  text-transform: uppercase; letter-spacing: .06em; margin: 0 0 .5rem;
}
.share-links-list { display: flex; flex-direction: column; gap: .625rem; }
.share-link-row { display: flex; flex-direction: column; gap: .375rem; padding: .75rem; border: 1px solid var(--gray-200); border-radius: var(--radius); }
.share-link-info { display: flex; align-items: center; gap: .5rem; }
.share-url-input {
  flex: 1; font-size: .8rem; color: var(--gray-700); border: 1px solid var(--gray-200);
  border-radius: 6px; padding: .25rem .5rem; background: var(--gray-50); cursor: text; min-width: 0;
}
.share-meta { font-size: .75rem; color: var(--gray-400); white-space: nowrap; }
.share-link-actions { display: flex; gap: .375rem; }
.share-empty { font-size: .875rem; color: var(--gray-500); margin: 0; }
.share-create { display: flex; flex-direction: column; }
.copy-msg { font-size: .8rem; color: var(--success); text-align: center; margin: 0; font-weight: 600; }

/* ── RWD ── */
@media (max-width: 900px) {
  .td-layout { grid-template-columns: 1fr; grid-template-rows: auto 1fr; overflow: auto; }
  .left-panel { border-right: none; border-bottom: 1px solid var(--gray-200); max-height: 60vh; }
  .map-panel { height: 40vh; min-height: 280px; }
}
</style>
