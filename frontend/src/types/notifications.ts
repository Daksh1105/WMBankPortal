export type NotificationType = 'Info' | 'Success' | 'Warning' | 'ActionRequired'

export interface UserNotification {
  id: string
  title: string
  message: string
  type: NotificationType
  applicationId?: string | null
  isRead: boolean
  createdAt: string
  readAt?: string | null
}

export interface UnreadNotificationCount {
  count: number
}
