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
  background: #fff;
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
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--primary);
  text-decoration: none;
  flex-shrink: 0;
}

/* Desktop nav */
.nav-desktop {
  display: flex;
  align-items: center;
  gap: 1.25rem;
}
.nav-desktop a {
  font-size: .875rem;
  color: var(--gray-600);
  white-space: nowrap;
}
.nav-desktop a.router-link-active { color: var(--primary); font-weight: 500; }

/* Hamburger */
.hamburger {
  display: none;
  flex-direction: column;
  justify-content: center;
  gap: 5px;
  width: 36px;
  height: 36px;
  background: none;
  border: none;
  cursor: pointer;
  padding: 4px;
}
.hamburger span {
  display: block;
  height: 2px;
  background: var(--gray-900);
  border-radius: 2px;
  transition: transform .2s, opacity .2s;
}
.hamburger.open span:nth-child(1) { transform: translateY(7px) rotate(45deg); }
.hamburger.open span:nth-child(2) { opacity: 0; }
.hamburger.open span:nth-child(3) { transform: translateY(-7px) rotate(-45deg); }

/* Mobile nav */
.nav-mobile {
  display: flex;
  flex-direction: column;
  gap: .75rem;
  padding: 1rem;
  border-top: 1px solid var(--gray-200);
  background: #fff;
  box-shadow: 0 4px 12px rgba(0,0,0,.08);
}
.nav-mobile a {
  font-size: 1rem;
  color: var(--gray-600);
  padding: .25rem 0;
}
.nav-mobile a.router-link-active { color: var(--primary); font-weight: 500; }

/* Transition */
.slide-enter-active, .slide-leave-active { transition: all .2s ease; }
.slide-enter-from, .slide-leave-to { opacity: 0; transform: translateY(-8px); }

/* Mobile breakpoint */
@media (max-width: 640px) {
  .nav-desktop { display: none; }
  .hamburger { display: flex; }
}

.btn-sm { padding: .375rem .875rem; font-size: .8125rem; }
</style>
