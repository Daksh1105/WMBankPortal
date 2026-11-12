import { HubConnectionBuilder, LogLevel, HubConnection, HubConnectionState } from '@microsoft/signalr'
import { getInMemoryAccessToken } from './client'
import type { UserNotification } from '../types/notifications'

type NotificationListener = (notification: UserNotification) => void
type StatusChangeListener = (applicationId: string, status: string, comments: string) => void

class SignalRNotificationService {
  private connection: HubConnection | null = null
  private notificationListeners: Set<NotificationListener> = new Set()
  private statusChangeListeners: Set<StatusChangeListener> = new Set()
  private isConnecting = false

  public async start(): Promise<void> {
    const token = getInMemoryAccessToken()
    if (!token) {
      return
    }

    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      return
    }

    if (this.isConnecting) {
      return
    }

    this.isConnecting = true

    try {
      this.connection = new HubConnectionBuilder()
        .withUrl('/hubs/notifications', {
          accessTokenFactory: () => getInMemoryAccessToken() ?? '',
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(LogLevel.Warning)
        .build()

      this.connection.on('ReceiveNotification', (notification: UserNotification) => {
        this.notificationListeners.forEach((listener) => listener(notification))
      })

      this.connection.on('ApplicationStatusChanged', (applicationId: string, status: string, comments: string) => {
        this.statusChangeListeners.forEach((listener) => listener(applicationId, status, comments))
      })

      await this.connection.start()
    } catch (err) {
      // In dev or sandbox environments where WebSockets might be unavailable, fail gracefully
      console.warn('SignalR notification connection attempt:', err)
    } finally {
      this.isConnecting = false
    }
  }

  public async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop()
      } catch {
        // Ignore disconnection errors
      }
      this.connection = null
    }
  }

  public onNotification(listener: NotificationListener): () => void {
    this.notificationListeners.add(listener)
    return () => this.notificationListeners.delete(listener)
  }

  public onStatusChange(listener: StatusChangeListener): () => void {
    this.statusChangeListeners.add(listener)
    return () => this.statusChangeListeners.delete(listener)
  }
}

export const signalRService = new SignalRNotificationService()
