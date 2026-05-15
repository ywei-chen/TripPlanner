<template>
  <div class="attractions-layout">
    <!-- 左側：搜尋 + 景點列表 -->
    <div class="list-panel">
      <div class="search-bar">
        <input
          v-model="params.q"
          placeholder="搜尋景點名稱..."
          class="form-input"
          @keyup.enter="doSearch"
        />
        <div class="filters">
          <input
            v-model="params.city"
            placeholder="城市（如：東京、台北）"
            class="form-input filter-input"
            @keyup.enter="doSearch"
          />
          <select v-model="params.category" class="form-input filter-input">
            <option value="">所有類別</option>
            <option>觀光</option>
            <option>文化</option>
            <option>自然</option>
            <option>購物</option>
            <option>美食</option>
          </select>
          <button class="btn btn-primary" @click="doSearch">搜尋</button>
        </div>
      </div>

      <p v-if="store.result" class="result-count">
        共 {{ store.result.totalCount }} 筆結果
        <span v-if="geocoding" class="geocoding-badge">📍 定位中...</span>
      </p>

      <div v-if="store.loading" class="loading-state">
        <div class="spinner" /><span>搜尋中...</span>
      </div>

      <div v-else-if="store.result?.items.length === 0" class="no-result">
        找不到符合的景點，試試其他關鍵字
      </div>

      <div v-else-if="store.result" class="card-list">
        <div
          v-for="a in store.result.items"
          :key="a.id"
          class="attraction-card"
          :class="{ active: selectedId === a.id }"
          @click="selectAttraction(a)"
        >
          <div class="cover" :style="a.coverImage ? `background-image:url(${a.coverImage})` : ''" />
          <div class="card-body">
            <div class="card-top">
              <h3>{{ a.name }}</h3>
              <button
                class="fav-btn"
                :class="{ active: a.isFavorited }"
                :title="a.isFavorited ? '取消收藏' : '收藏'"
                @click.stop="toggleFav(a.id, a.isFavorited)"
              >♥</button>
            </div>
            <div class="tags">
              <span v-if="a.category" class="badge">{{ a.category }}</span>
              <span v-if="a.city" class="badge">{{ a.city }}</span>
              <span class="badge">★ {{ a.rating }}</span>
            </div>
            <div class="card-bottom">
              <p class="desc">{{ a.description }}</p>
            </div>
          </div>
        </div>

        <div v-if="store.result.totalPages > 1" class="pagination">
          <button
            v-for="p in store.result.totalPages"
            :key="p"
            class="btn btn-sm"
            :class="p === params.page ? 'btn-primary' : 'btn-outline'"
            @click="goPage(p)"
          >{{ p }}</button>
        </div>
      </div>
    </div>

    <!-- 右側：Google Maps -->
    <div class="map-panel">
      <div v-if="mapError" class="map-error">
        <p>⚠️ 地圖載入失敗</p>
        <small>{{ mapError }}</small>
      </div>
      <div ref="mapEl" class="map-container" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { useAttractionsStore } from '@/stores/attractions.store'
import { useAuthStore } from '@/stores/auth.store'
import type { Attraction } from '@/types/attraction.types'

const store = useAttractionsStore()
const auth = useAuthStore()

const mapEl = ref<HTMLElement | null>(null)
const params = reactive({ q: '', city: '', category: '', page: 1, pageSize: 20 })
const selectedId = ref<string | null>(null)
const geocoding = ref(false)
const mapError = ref('')

let map: google.maps.Map | null = null
const markerMap = new Map<string, google.maps.Marker>()
let activeInfoWindow: google.maps.InfoWindow | null = null

// ── 載入 Google Maps SDK ─────────────────────────────────────────────────────
function loadGoogleMaps(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window.google?.maps) { resolve(); return }
    const cbName = `__gmInit${Date.now()}`
    ;(window as Record<string, unknown>)[cbName] = () => {
      resolve()
      delete (window as Record<string, unknown>)[cbName]
    }
    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?key=${import.meta.env.VITE_GOOGLE_MAPS_KEY}&callback=${cbName}&language=zh-TW&region=TW`
    script.async = true
    script.onerror = () => reject(new Error('Google Maps 載入失敗，請確認 API Key 是否有效'))
    document.head.appendChild(script)
  })
}

// ── Nominatim geocoding（免費，無需 API key）──────────────────────────────────
async function geocodeQuery(query: string): Promise<[number, number] | null> {
  try {
    const res = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1`,
      { headers: { 'Accept-Language': 'zh-TW,zh;q=0.9' } }
    )
    const data = await res.json()
    if (data.length > 0) return [parseFloat(data[0].lat), parseFloat(data[0].lon)]
  } catch { /* ignore */ }
  return null
}

// ── 搜尋 ──────────────────────────────────────────────────────────────────────
async function doSearch() {
  params.page = 1
  store.search({ ...params })

  const queryForGeo = params.city || params.q
  if (queryForGeo && map) {
    geocoding.value = true
    const coords = await geocodeQuery(queryForGeo)
    geocoding.value = false
    if (coords && map) {
      map.panTo({ lat: coords[0], lng: coords[1] })
      map.setZoom(params.city ? 11 : 8)
    }
  }
}

function goPage(p: number) {
  params.page = p
  store.search({ ...params })
}

async function toggleFav(id: string, isFav: boolean) {
  if (!auth.isAuthenticated) return
  await store.toggleFavorite(id, isFav)
}

// ── 景點選取 ──────────────────────────────────────────────────────────────────
function selectAttraction(a: Attraction) {
  selectedId.value = a.id
  if (map && a.latitude && a.longitude) {
    map.panTo({ lat: Number(a.latitude), lng: Number(a.longitude) })
    map.setZoom(15)
    const marker = markerMap.get(a.id)
    if (marker) openInfoWindow(a, marker)
  }
}

// ── InfoWindow ────────────────────────────────────────────────────────────────
function openInfoWindow(a: Attraction, marker: google.maps.Marker) {
  activeInfoWindow?.close()
  const iw = new google.maps.InfoWindow({
    content: `
      <div style="min-width:190px;font-family:system-ui;line-height:1.5;">
        <strong style="font-size:.9rem;">${a.name}</strong><br>
        <small style="color:#666;">${[a.city, a.country].filter(Boolean).join(' · ')}</small><br>
        <span style="color:#f59e0b;">★ ${a.rating}</span>
        ${a.category ? `<span style="margin-left:.5rem;background:#f3f4f6;padding:.1rem .4rem;border-radius:4px;font-size:.75rem;">${a.category}</span>` : ''}
      </div>
    `
  })
  iw.open({ map: map!, anchor: marker })
  activeInfoWindow = iw
}

// ── 地圖初始化 ────────────────────────────────────────────────────────────────
async function initMap() {
  if (!mapEl.value) return
  try {
    await loadGoogleMaps()
    map = new google.maps.Map(mapEl.value, {
      center: { lat: 23.5, lng: 121 },
      zoom: 5,
      mapTypeControl: true,
      mapTypeControlOptions: {
        style: google.maps.MapTypeControlStyle.DROPDOWN_MENU,
        position: google.maps.ControlPosition.TOP_RIGHT,
      },
      fullscreenControl: true,
      streetViewControl: true,
      zoomControl: true,
    })
  } catch (e: unknown) {
    mapError.value = e instanceof Error ? e.message : '未知錯誤'
  }
}

// ── 更新 Markers ──────────────────────────────────────────────────────────────
function updateMarkers(attractions: Attraction[]) {
  if (!map || !window.google?.maps) return

  markerMap.forEach(m => m.setMap(null))
  markerMap.clear()
  activeInfoWindow?.close()
  activeInfoWindow = null

  const valid = attractions.filter(a => a.latitude && a.longitude)
  if (valid.length === 0) return

  const bounds = new google.maps.LatLngBounds()

  valid.forEach(a => {
    const isSelected = selectedId.value === a.id
    const marker = new google.maps.Marker({
      position: { lat: Number(a.latitude!), lng: Number(a.longitude!) },
      map: map!,
      title: a.name,
      icon: isSelected ? {
        path: google.maps.SymbolPath.CIRCLE,
        scale: 11,
        fillColor: '#2563eb',
        fillOpacity: 1,
        strokeColor: '#ffffff',
        strokeWeight: 2.5,
      } : undefined,
    })

    marker.addListener('click', () => {
      selectedId.value = a.id
      openInfoWindow(a, marker)
      nextTick(() => {
        document.querySelector('.attraction-card.active')
          ?.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
      })
    })

    markerMap.set(a.id, marker)
    bounds.extend({ lat: Number(a.latitude!), lng: Number(a.longitude!) })
  })

  if (!params.city && !params.q) {
    map!.fitBounds(bounds, 50)
  } else if (valid.length <= 3) {
    map!.fitBounds(bounds, 80)
  }
}

watch(() => store.result?.items, (items) => {
  if (items) updateMarkers(items)
}, { deep: true })

onMounted(async () => {
  await initMap()
  doSearch()
})

onUnmounted(() => {
  markerMap.forEach(m => m.setMap(null))
  markerMap.clear()
  activeInfoWindow?.close()
  map = null
})
</script>

<style scoped>
.attractions-layout {
  display: grid;
  grid-template-columns: 400px 1fr;
  flex: 1;
  overflow: hidden;
}

/* ── 左側 ── */
.list-panel {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-right: 1px solid var(--gray-200);
  background: var(--surface);
}

.search-bar {
  padding: .875rem;
  border-bottom: 1px solid var(--gray-200);
  display: flex;
  flex-direction: column;
  gap: .5rem;
  background: var(--surface);
  flex-shrink: 0;
}
.filters {
  display: flex;
  gap: .5rem;
  align-items: center;
}
.filter-input { flex: 1; min-width: 0; }

.result-count {
  padding: .375rem .875rem;
  font-size: .8125rem;
  color: var(--gray-600);
  border-bottom: 1px solid var(--gray-200);
  background: var(--gray-50);
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: .5rem;
}
.geocoding-badge {
  font-size: .75rem;
  background: var(--primary-muted);
  color: var(--primary);
  padding: .1rem .45rem;
  border-radius: 999px;
  border: 1px solid var(--primary-border);
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: .625rem;
  padding: 2rem;
  color: var(--gray-600);
  font-size: .875rem;
}
.spinner {
  width: 18px; height: 18px;
  border: 2px solid var(--gray-200);
  border-top-color: var(--primary);
  border-radius: 50%;
  animation: spin .7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.no-result {
  padding: 2rem;
  text-align: center;
  color: var(--gray-600);
  font-size: .875rem;
}

.card-list {
  flex: 1;
  overflow-y: auto;
  padding: .625rem;
  display: flex;
  flex-direction: column;
  gap: .5rem;
}

.attraction-card {
  display: flex;
  flex-direction: row;
  height: 90px;
  background: var(--surface-0);
  border-radius: var(--radius);
  border: 2px solid transparent;
  box-shadow: var(--shadow-xs);
  overflow: hidden;
  cursor: pointer;
  transition: border-color .12s, box-shadow .12s;
}
.attraction-card:hover {
  border-color: var(--surface-3);
  box-shadow: var(--shadow-sm);
}
.attraction-card.active {
  border-color: var(--primary);
  box-shadow: 0 0 0 3px var(--primary-muted);
}

.cover {
  width: 90px;
  flex-shrink: 0;
  background: var(--gray-100);
  background-size: cover;
  background-position: center;
}
.card-body {
  padding: .5rem .625rem;
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: .2rem;
}
.card-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}
.card-top h3 {
  font-size: .875rem;
  line-height: 1.3;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
}
.fav-btn {
  background: none; border: none;
  font-size: 1rem; cursor: pointer;
  color: var(--gray-200); flex-shrink: 0; padding: 0;
}
.fav-btn.active { color: var(--danger); }

.tags { display: flex; gap: .2rem; flex-wrap: wrap; }
.tags .badge { font-size: .7rem; }

.card-bottom {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: .25rem;
  flex: 1;
}
.desc {
  font-size: .775rem;
  color: var(--gray-600);
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.4;
  flex: 1;
}

.pagination {
  display: flex;
  gap: .3rem;
  justify-content: center;
  padding: .625rem 0;
  flex-wrap: wrap;
}
.btn-sm { padding: .25rem .625rem; font-size: .8rem; }

/* ── 右側地圖 ── */
.map-panel { position: relative; overflow: hidden; }
.map-container { width: 100%; height: 100%; }
.map-error {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: var(--gray-50);
  color: var(--gray-600);
  gap: .5rem;
  z-index: 10;
}

/* ── RWD ── */
@media (max-width: 900px) {
  .attractions-layout {
    grid-template-columns: 1fr;
    grid-template-rows: auto 1fr;
    overflow: auto;
  }
  .list-panel {
    border-right: none;
    border-bottom: 1px solid var(--gray-200);
    max-height: 55vh;
  }
  .map-panel { height: 45vh; min-height: 280px; }
}
@media (max-width: 640px) {
  .filters { flex-wrap: wrap; }
  .list-panel { max-height: 50vh; }
  .map-panel { height: 40vh; }
}
</style>
