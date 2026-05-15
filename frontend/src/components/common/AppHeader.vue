<template>
  <header class="app-header">
    <div class="container header-inner">
      <RouterLink to="/" class="logo">TripPlanner</RouterLink>

      <!-- Desktop nav -->
      <nav class="nav-desktop">
        <RouterLink to="/attractions">探索景點</RouterLink>
        <template v-if="auth.isAuthenticated">
          <RouterLink to="/trips">我的行程</RouterLink>
          <RouterLink to="/favorites">收藏</RouterLink>
          <button class="btn btn-outline btn-sm" @click="handleLogout">登出</button>
        </template>
        <template v-else>
          <RouterLink to="/login">登入</RouterLink>
          <RouterLink to="/register" class="btn btn-primary btn-sm">註冊</RouterLink>
        </template>
      </nav>

      <!-- Mobile hamburger -->
      <button class="hamburger" :class="{ open: menuOpen }" @click="menuOpen = !menuOpen" aria-label="選單">
        <span /><span /><span />
      </button>
    </div>

    <!-- Mobile drawer -->
    <Transition name="slide">
      <nav v-if="menuOpen" class="nav-mobile" @click="menuOpen = false">
        <RouterLink to="/attractions">探索景點</RouterLink>
        <template v-if="auth.isAuthenticated">
          <RouterLink to="/trips">我的行程</RouterLink>
          <RouterLink to="/favorites">收藏</RouterLink>
          <button class="btn btn-outline" style="width:100%;justify-content:center;" @click="handleLogout">登出</button>
        </template>
        <template v-else>
          <RouterLink to="/login">登入</RouterLink>
          <RouterLink to="/register" class="btn btn-primary" style="width:100%;justify-content:center;">註冊</RouterLink>
        </template>
      </nav>
    </Transition>
  </header>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()
const menuOpen = ref(false)

async function handleLogout() {
  await auth.logout()
  menuOpen.value = false
  router.push('/login')
}
</script>

<style scoped>
.app-header {
  background: var(--surface);
  border-bottom: 1px solid var(--gray-200);
  position: sticky;
  top: 0;
  z-index: 100;
}
.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 56px;
}

.logo {
  font-size: 1.1875rem;
  font-weight: 800;
  color: var(--primary);
  text-decoration: none;
  flex-shrink: 0;
  letter-spacing: -.025em;
  transition: color .14s;
}
.logo:hover { color: var(--primary-hover); text-decoration: none; }

/* Desktop nav */
.nav-desktop {
  display: flex;
  align-items: center;
  gap: .25rem;
}
.nav-desktop a {
  font-size: .875rem;
  font-weight: 500;
  color: var(--gray-500);
  white-space: nowrap;
  padding: .375rem .75rem;
  border-radius: 8px;
  transition: background .12s, color .12s;
}
.nav-desktop a:hover { background: var(--gray-100); color: var(--gray-900); text-decoration: none; }
.nav-desktop a.router-link-active { color: var(--primary); font-weight: 600; background: var(--primary-light); }

/* Hamburger */
.hamburger {
  display: none;
  flex-direction: column;
  justify-content: center;
  gap: 5px;
  width: 36px; height: 36px;
  background: none; border: none; cursor: pointer; padding: 4px; border-radius: 8px;
}
.hamburger:hover { background: var(--gray-100); }
.hamburger span {
  display: block; height: 1.75px;
  background: var(--gray-700); border-radius: 2px;
  transition: transform .2s, opacity .2s;
}
.hamburger.open span:nth-child(1) { transform: translateY(6.75px) rotate(45deg); }
.hamburger.open span:nth-child(2) { opacity: 0; }
.hamburger.open span:nth-child(3) { transform: translateY(-6.75px) rotate(-45deg); }

/* Mobile nav */
.nav-mobile {
  display: flex; flex-direction: column; gap: .25rem;
  padding: .75rem; border-top: 1px solid var(--gray-200);
  background: var(--surface); box-shadow: var(--shadow-md);
}
.nav-mobile a {
  font-size: .9375rem; font-weight: 500; color: var(--gray-600);
  padding: .5rem .75rem; border-radius: 8px; transition: background .12s, color .12s;
}
.nav-mobile a:hover { background: var(--gray-100); color: var(--gray-900); text-decoration: none; }
.nav-mobile a.router-link-active { color: var(--primary); background: var(--primary-light); font-weight: 600; }

/* Transition */
.slide-enter-active, .slide-leave-active { transition: opacity .15s, transform .15s; }
.slide-enter-from, .slide-leave-to { opacity: 0; transform: translateY(-6px); }

@media (max-width: 640px) {
  .nav-desktop { display: none; }
  .hamburger { display: flex; }
}
</style>
