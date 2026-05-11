import { defineStore } from 'pinia'
import { ref } from 'vue'
import { attractionsService } from '@/services/attractions.service'
import type { Attraction, PagedResult, SearchAttractionsParams } from '@/types/attraction.types'

export const useAttractionsStore = defineStore('attractions', () => {
  const result = ref<PagedResult<Attraction> | null>(null)
  const favorites = ref<Attraction[]>([])
  const loading = ref(false)

  async function search(params: SearchAttractionsParams) {
    loading.value = true
    try {
      result.value = await attractionsService.search(params)
    } finally {
      loading.value = false
    }
  }

  async function fetchFavorites() {
    favorites.value = await attractionsService.getFavorites()
  }

  async function toggleFavorite(id: string, isFav: boolean) {
    if (isFav) {
      await attractionsService.removeFavorite(id)
      favorites.value = favorites.value.filter(a => a.id !== id)
    } else {
      await attractionsService.addFavorite(id)
    }
    if (result.value) {
      const item = result.value.items.find(a => a.id === id)
      if (item) item.isFavorited = !isFav
    }
  }

  return { result, favorites, loading, search, fetchFavorites, toggleFavorite }
})
