<template>
  <div id="app-wrapper">
    <AppHeader />
    <main :class="isFullHeight ? 'main-full' : 'main-padded container'">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppHeader from '@/components/common/AppHeader.vue'

const route = useRoute()
// 景點頁需要全高全寬，不加 container/padding
const isFullHeight = computed(() => route.name === 'attractions' || route.name === 'trip-detail')
</script>

<style>
#app-wrapper {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.main-padded {
  padding-top: 1.5rem;
  padding-bottom: 2rem;
  overflow-y: auto;
  flex: 1;
}

.main-full {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
</style>
