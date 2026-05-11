import { defineStore } from 'pinia'
import { ref } from 'vue'
import { tripsService } from '@/services/trips.service'
import type { Trip, CreateTripRequest, UpdateTripRequest, AddTripItemRequest } from '@/types/trip.types'

export const useTripsStore = defineStore('trips', () => {
  const trips = ref<Trip[]>([])
  const currentTrip = ref<Trip | null>(null)
  const loading = ref(false)

  async function fetchAll() {
    loading.value = true
    try {
      trips.value = await tripsService.getAll()
    } finally {
      loading.value = false
    }
  }

  async function fetchById(id: string) {
    loading.value = true
    try {
      currentTrip.value = await tripsService.getById(id)
    } finally {
      loading.value = false
    }
  }

  async function create(data: CreateTripRequest) {
    const trip = await tripsService.create(data)
    trips.value.unshift(trip)
    return trip
  }

  async function update(id: string, data: UpdateTripRequest) {
    const updated = await tripsService.update(id, data)
    const idx = trips.value.findIndex(t => t.id === id)
    if (idx !== -1) trips.value[idx] = updated
    if (currentTrip.value?.id === id) currentTrip.value = updated
    return updated
  }

  async function remove(id: string) {
    await tripsService.delete(id)
    trips.value = trips.value.filter(t => t.id !== id)
  }

  async function addItem(tripId: string, data: AddTripItemRequest) {
    const item = await tripsService.addItem(tripId, data)
    if (currentTrip.value?.id === tripId) {
      currentTrip.value.items.push(item)
    }
    return item
  }

  async function removeItem(tripId: string, itemId: string) {
    await tripsService.deleteItem(tripId, itemId)
    if (currentTrip.value?.id === tripId) {
      currentTrip.value.items = currentTrip.value.items.filter(i => i.id !== itemId)
    }
  }

  async function reorderItems(tripId: string, data: import('@/types/trip.types').ReorderItemsRequest) {
    await tripsService.reorderItems(tripId, data)
    await fetchById(tripId)
  }

  return { trips, currentTrip, loading, fetchAll, fetchById, create, update, remove, addItem, removeItem, reorderItems }
})
