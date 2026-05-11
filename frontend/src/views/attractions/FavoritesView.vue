<template>
  <div>
    <h1 style="margin-bottom:1.5rem;">我的收藏</h1>
    <div v-if="loading" class="loading">載入中...</div>
    <div v-else-if="store.favorites.length === 0" class="empty-state card">
      <p>還沒有收藏任何景點</p>
      <RouterLink to="/attractions" class="btn btn-primary">探索景點</RouterLink>
    </div>
    <div v-else class="grid">
      <div v-for="a in store.favorites" :key="a.id" class="card fav-card">
        <div class="cover" :style="a.coverImage ? `background-image:url(${a.coverImage})` : ''"></div>
        <div class="info">
          <h3>{{ a.name }}</h3>
          <div style="display:flex; gap:.5rem; margin:.5rem 0;">
            <span class="badge">{{ a.category }}</span>
            <span class="badge">{{ a.city }}</span>
          </div>
          <button class="btn btn-outline btn-sm" @click="store.toggleFavorite(a.id, true)">取消收藏</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAttractionsStore } from '@/stores/attractions.store'

const store = useAttractionsStore()
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  await store.fetchFavorites()
  loading.value = false
})
</script>

<style scoped>
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1.25rem; }
.fav-card { padding: 0; overflow: hidden; }
.cover { height: 140px; background: var(--gray-100); background-size: cover; background-position: center; }
.info { padding: 1rem; }
.btn-sm { padding: .25rem .625rem; font-size: .8125rem; }
.empty-state { text-align: center; padding: 3rem; display: flex; flex-direction: column; align-items: center; gap: 1rem; }
.loading { text-align: center; padding: 2rem; color: var(--gray-600); }
</style>
