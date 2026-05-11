import api from './api'
import type { ShareLink, CreateShareRequest } from '@/types/share.types'

export const shareService = {
  create: (tripId: string, data: CreateShareRequest = {}) =>
    api.post<ShareLink>(`/share/trips/${tripId}`, data).then(r => r.data),

  getLinks: (tripId: string) =>
    api.get<ShareLink[]>(`/share/trips/${tripId}/links`).then(r => r.data),

  deactivate: (linkId: string) =>
    api.delete(`/share/links/${linkId}`),

  getSharedTrip: (token: string) =>
    api.get(`/public/trips/${token}`).then(r => r.data)
}
