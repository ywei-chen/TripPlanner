export interface TripItem {
  id: string
  attractionId?: string
  attractionName?: string
  attractionCoverImage?: string
  dayNumber: number
  sortOrder: number
  customName?: string
  notes?: string
  startTime?: string
  durationMins?: number
  attractionLatitude?: number
  attractionLongitude?: number
}

export interface Trip {
  id: string
  title: string
  description?: string
  coverImage?: string
  startDate?: string
  endDate?: string
  status: 'Draft' | 'Published' | 'Archived'
  isPublic: boolean
  createdAt: string
  items: TripItem[]
}

export interface CreateTripRequest {
  title: string
  description?: string
  startDate?: string
  endDate?: string
}

export interface UpdateTripRequest {
  title: string
  description?: string
  coverImage?: string
  startDate?: string
  endDate?: string
  isPublic: boolean
}

export interface AddTripItemRequest {
  attractionId?: string
  dayNumber: number
  sortOrder: number
  customName?: string
  notes?: string
  startTime?: string
  durationMins?: number
}

export interface ReorderItemsRequest {
  items: { itemId: string; dayNumber: number; sortOrder: number }[]
}
