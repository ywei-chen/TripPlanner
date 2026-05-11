<template>
  <div class="home">
    <section class="hero">
      <h1>規劃你的完美旅程</h1>
      <p>搜尋景點、建立行程、與朋友分享</p>
      <div class="hero-actions">
        <RouterLink to="/attractions" class="btn btn-primary">探索景點</RouterLink>
        <RouterLink v-if="!auth.isAuthenticated" to="/register" class="btn btn-outline">免費開始</RouterLink>
        <RouterLink v-else to="/trips" class="btn btn-outline">我的行程</RouterLink>
      </div>
    </section>

    <section class="features">
      <div class="feature-card card" v-for="f in features" :key="f.title">
        <div class="feature-icon">{{ f.icon }}</div>
        <h3>{{ f.title }}</h3>
        <p>{{ f.desc }}</p>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store'

const auth = useAuthStore()
const features = [
  { icon: '🔍', title: '搜尋景點', desc: '從數千個精選景點中找到最適合你的目的地' },
  { icon: '📋', title: '規劃行程', desc: '輕鬆拖拉排序，按天規劃你的旅遊行程' },
  { icon: '🔗', title: '分享行程', desc: '一鍵產生分享連結，讓朋友也能查看你的行程' }
]
</script>

<style scoped>
.hero {
  text-align: center;
  padding: 5rem 1rem 4rem;
}
.hero h1 {
  font-size: clamp(2rem, 5vw, 3rem);
  line-height: 1.2;
  margin-bottom: .75rem;
}
.hero p {
  color: var(--gray-600);
  margin-bottom: 2rem;
  font-size: clamp(1rem, 2.5vw, 1.125rem);
}
.hero-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}
.hero-actions .btn {
  min-width: 120px;
  justify-content: center;
}

.features {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1.25rem;
  padding: 2rem 0 3rem;
}
.feature-icon { font-size: 2rem; margin-bottom: .75rem; }
.feature-card h3 { margin-bottom: .5rem; }
.feature-card p { color: var(--gray-600); font-size: .875rem; line-height: 1.6; }

@media (max-width: 640px) {
  .hero { padding: 3rem 1rem 2.5rem; }
  .features { grid-template-columns: 1fr; }
}
</style>
