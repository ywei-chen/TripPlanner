import api from './api'
import type { Attraction, PagedResult, SearchAttractionsParams } from '@/types/attraction.types'

export const attractionsService = {
  search: (params: SearchAttractionsParams) =>
    api.get<PagedResult<Attraction>>('/attractions', { params }).then(r => r.data),

  getById: (id: string) =>
    api.get<Attraction>(`/attractions/${id}`).then(r => r.data),

  getFavorites: () =>
    api.get<Attraction[]>('/attractions/favorites').then(r => r.data),

  addFavorite: (id: string) =>
    api.post(`/attractions/${id}/favorite`),

  removeFavorite: (id: string) =>
    api.delete(`/attractions/${id}/favorite`)
}
