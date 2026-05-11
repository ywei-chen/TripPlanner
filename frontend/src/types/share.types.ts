export interface ShareLink {
  id: string
  shareToken: string
  shareUrl: string
  permission: 'View' | 'Comment'
  expiresAt?: string
  isActive: boolean
  viewCount: number
  createdAt: string
}

export interface CreateShareRequest {
  permission?: 'view' | 'comment'
  expiresAt?: string
}
