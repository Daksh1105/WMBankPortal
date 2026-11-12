import React, { useState, useEffect, useRef } from 'react'
import { apiClient } from '../../api/client'
import { signalRService } from '../../api/signalr'
import type { UserNotification, UnreadNotificationCount } from '../../types/notifications'
import {
  Bell,
  CheckCheck,
  CheckCircle2,
  AlertTriangle,
  AlertCircle,
  Info,
  Clock,
  ExternalLink,
  X
} from 'lucide-react'

interface NotificationBellProps {
  onNavigateToApplication?: (appId: string) => void
}

export const NotificationBell: React.FC<NotificationBellProps> = ({ onNavigateToApplication }) => {
  const [isOpen, setIsOpen] = useState(false)
  const [unreadCount, setUnreadCount] = useState(0)
  const [notifications, setNotifications] = useState<UserNotification[]>([])
  const [loading, setLoading] = useState(false)
  const [toastNotification, setToastNotification] = useState<UserNotification | null>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    fetchUnreadCount()
    signalRService.start()

    // Real-time listener for incoming SignalR notifications
    const unsubscribe = signalRService.onNotification((notification) => {
      setNotifications((prev) => [notification, ...prev])
      setUnreadCount((prev) => prev + 1)
      setToastNotification(notification)

      // Auto-hide toast after 5 seconds
      setTimeout(() => {
        setToastNotification((curr) => (curr?.id === notification.id ? null : curr))
      }, 5000)
    })

    // Close on outside click
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)

    return () => {
      unsubscribe()
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [])

  const fetchUnreadCount = async () => {
    try {
      const res = await apiClient.get<UnreadNotificationCount>('/api/v1/notifications/unread-count')
      setUnreadCount(res.data.count)
    } catch {
      // Ignore background counter errors
    }
  }

  const fetchNotifications = async () => {
    try {
      setLoading(true)
      const res = await apiClient.get<{ items: UserNotification[] }>('/api/v1/notifications?pageNumber=1&pageSize=20')
      setNotifications(res.data.items || [])
    } catch {
      // Ignore
    } finally {
      setLoading(false)
    }
  }

  const handleToggle = () => {
    if (!isOpen) {
      fetchNotifications()
    }
    setIsOpen(!isOpen)
  }

  const handleMarkAsRead = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation()
    try {
      await apiClient.put(`/api/v1/notifications/${id}/read`)
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n))
      )
      setUnreadCount((prev) => Math.max(0, prev - 1))
    } catch {
      // Ignore
    }
  }

  const handleMarkAllAsRead = async () => {
    try {
      await apiClient.put('/api/v1/notifications/read-all')
      setNotifications((prev) =>
        prev.map((n) => ({ ...n, isRead: true, readAt: new Date().toISOString() }))
      )
      setUnreadCount(0)
    } catch {
      // Ignore
    }
  }

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'Success':
        return <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0" />
      case 'Warning':
        return <AlertTriangle className="w-4 h-4 text-amber-500 flex-shrink-0" />
      case 'ActionRequired':
        return <AlertCircle className="w-4 h-4 text-purple-600 flex-shrink-0" />
      default:
        return <Info className="w-4 h-4 text-blue-500 flex-shrink-0" />
    }
  }

  const formatTimestamp = (dateStr: string) => {
    const diff = (Date.now() - new Date(dateStr).getTime()) / 1000
    if (diff < 60) return 'Just now'
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`
    return new Date(dateStr).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
  }

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Real-time Toast Pop-up */}
      {toastNotification && (
        <div className="fixed top-5 right-5 z-50 max-w-sm w-full bg-white rounded-xl shadow-2xl border border-slate-200 p-4 animate-in slide-in-from-top-3 fade-in duration-300 flex items-start space-x-3">
          <div className="p-2 rounded-lg bg-blue-50">
            {getNotificationIcon(toastNotification.type)}
          </div>
          <div className="flex-1 min-w-0">
            <h4 className="text-xs font-bold text-slate-900">{toastNotification.title}</h4>
            <p className="text-[11px] text-slate-600 mt-0.5 line-clamp-2">{toastNotification.message}</p>
          </div>
          <button
            onClick={() => setToastNotification(null)}
            className="text-slate-400 hover:text-slate-600 p-1"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        </div>
      )}

      {/* Bell Trigger Button */}
      <button
        onClick={handleToggle}
        className="relative p-2 text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-lg transition-colors focus:outline-none"
        title="View Notifications"
        aria-label="Notifications"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-bold text-white bg-rose-600 rounded-full animate-pulse shadow-sm">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 bg-white rounded-2xl shadow-2xl border border-slate-200 z-50 overflow-hidden animate-in fade-in zoom-in-95 duration-150">
          <div className="p-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
            <div className="flex items-center space-x-2">
              <h3 className="text-sm font-bold text-slate-900">Notifications</h3>
              {unreadCount > 0 && (
                <span className="px-2 py-0.5 bg-rose-100 text-rose-700 text-[10px] font-bold rounded-full">
                  {unreadCount} new
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                onClick={handleMarkAllAsRead}
                className="text-[11px] text-blue-600 hover:text-blue-800 font-semibold inline-flex items-center"
              >
                <CheckCheck className="w-3.5 h-3.5 mr-1" />
                Mark all read
              </button>
            )}
          </div>

          <div className="max-h-96 overflow-y-auto divide-y divide-slate-100">
            {loading && notifications.length === 0 ? (
              <div className="py-8 text-center text-xs text-slate-400">Loading alerts...</div>
            ) : notifications.length === 0 ? (
              <div className="py-10 text-center px-4">
                <Bell className="w-8 h-8 text-slate-300 mx-auto mb-2" />
                <p className="text-xs font-semibold text-slate-700">All caught up!</p>
                <p className="text-[11px] text-slate-400 mt-0.5">No notifications right now.</p>
              </div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  onClick={() => {
                    if (!n.isRead) {
                      apiClient.put(`/api/v1/notifications/${n.id}/read`)
                      setNotifications((prev) =>
                        prev.map((item) => (item.id === n.id ? { ...item, isRead: true } : item))
                      )
                      setUnreadCount((prev) => Math.max(0, prev - 1))
                    }
                    if (n.applicationId && onNavigateToApplication) {
                      onNavigateToApplication(n.applicationId)
                      setIsOpen(false)
                    }
                  }}
                  className={`p-3.5 flex items-start space-x-3 transition-colors cursor-pointer hover:bg-slate-50 ${
                    !n.isRead ? 'bg-blue-50/40' : 'bg-white'
                  }`}
                >
                  <div className="mt-0.5 p-1.5 rounded-lg bg-slate-100/70">
                    {getNotificationIcon(n.type)}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <h4 className={`text-xs ${!n.isRead ? 'font-bold text-slate-900' : 'font-medium text-slate-700'}`}>
                        {n.title}
                      </h4>
                      <span className="text-[10px] text-slate-400 whitespace-nowrap flex items-center">
                        <Clock className="w-2.5 h-2.5 mr-0.5" />
                        {formatTimestamp(n.createdAt)}
                      </span>
                    </div>

                    <p className="text-[11px] text-slate-500 mt-0.5 line-clamp-2">{n.message}</p>

                    {n.applicationId && (
                      <span className="inline-flex items-center text-[10px] font-semibold text-blue-600 mt-1 hover:underline">
                        View Application
                        <ExternalLink className="w-2.5 h-2.5 ml-1" />
                      </span>
                    )}
                  </div>

                  {!n.isRead && (
                    <button
                      onClick={(e) => handleMarkAsRead(n.id, e)}
                      className="w-2 h-2 rounded-full bg-blue-600 mt-1.5 flex-shrink-0"
                      title="Mark as read"
                    />
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  )
}
