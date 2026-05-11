export interface Attraction {
  id: string
  name: string
  description?: string
  category?: string
  address?: string
  city?: string
  country?: string
  latitude?: number
  longitude?: number
  coverImage?: string
  rating: number
  tags: string[]
  isFavorited: boolean
}

export interface SearchAttractionsParams {
  q?: string
  category?: string
  city?: string
  page?: number
  pageSize?: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
