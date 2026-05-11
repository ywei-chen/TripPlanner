import api from './api'
import type {
  Trip, CreateTripRequest, UpdateTripRequest,
  AddTripItemRequest, TripItem, ReorderItemsRequest
} from '@/types/trip.types'

export const tripsService = {
  getAll: () =>
    api.get<Trip[]>('/trips').then(r => r.data),

  getById: (id: string) =>
    api.get<Trip>(`/trips/${id}`).then(r => r.data),

  create: (data: CreateTripRequest) =>
    api.post<Trip>('/trips', data).then(r => r.data),

  update: (id: string, data: UpdateTripRequest) =>
    api.put<Trip>(`/trips/${id}`, data).then(r => r.data),

  delete: (id: string) =>
    api.delete(`/trips/${id}`),

  addItem: (tripId: string, data: AddTripItemRequest) =>
    api.post<TripItem>(`/trips/${tripId}/items`, data).then(r => r.data),

  deleteItem: (tripId: string, itemId: string) =>
    api.delete(`/trips/${tripId}/items/${itemId}`),

  reorderItems: (tripId: string, data: ReorderItemsRequest) =>
    api.put(`/trips/${tripId}/items/reorder`, data)
}
