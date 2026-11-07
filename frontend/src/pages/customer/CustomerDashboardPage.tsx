import React, { useState, useEffect } from 'react'
import { apiClient } from '../../api/client'
import type { CustomerDashboard, BankingApplication, ApplicationTimeline } from '../../types/banking'
import {
  FileText,
  Clock,
  CheckCircle2,
  XCircle,
  History,
  AlertCircle,
  Loader2,
  X,
  Ban,
  ArrowUpRight
} from 'lucide-react'

interface CustomerDashboardPageProps {
  onNavigateToProducts: () => void
}

export const CustomerDashboardPage: React.FC<CustomerDashboardPageProps> = ({ onNavigateToProducts }) => {
  const [dashboard, setDashboard] = useState<CustomerDashboard | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Timeline modal
  const [selectedApp, setSelectedApp] = useState<BankingApplication | null>(null)
  const [timelines, setTimelines] = useState<ApplicationTimeline[]>([])
  const [loadingTimeline, setLoadingTimeline] = useState(false)

  // Withdraw state
  const [withdrawingAppId, setWithdrawingAppId] = useState<string | null>(null)
  const [withdrawRemarks, setWithdrawRemarks] = useState('')
  const [withdrawing, setWithdrawing] = useState(false)

  useEffect(() => {
    fetchDashboard()
  }, [])

  const fetchDashboard = async () => {
    try {
      setLoading(true)
      const res = await apiClient.get<CustomerDashboard>('/api/v1/customer/dashboard')
      setDashboard(res.data)
    } catch (err: any) {
      setError('Unable to load customer dashboard overview.')
    } finally {
      setLoading(false)
    }
  }

  const handleOpenTimeline = async (app: BankingApplication) => {
    setSelectedApp(app)
    setLoadingTimeline(true)
    try {
      const res = await apiClient.get<ApplicationTimeline[]>(`/api/v1/applications/${app.id}/timeline`)
      setTimelines(res.data)
    } catch {
      setTimelines([])
    } finally {
      setLoadingTimeline(false)
    }
  }

  const handleWithdrawSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!withdrawingAppId) return

    try {
      setWithdrawing(true)
      await apiClient.post(`/api/v1/applications/${withdrawingAppId}/withdraw`, {
        remarks: withdrawRemarks || 'Withdrawn by customer request'
      })
      setWithdrawingAppId(null)
      setWithdrawRemarks('')
      await fetchDashboard()
    } catch (err: any) {
      alert(err.response?.data?.detail || 'Failed to withdraw application.')
    } finally {
      setWithdrawing(false)
    }
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Submitted':
        return <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-blue-100 text-blue-800">Submitted</span>
      case 'UnderReview':
        return <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-amber-100 text-amber-800">Under Review</span>
      case 'KycPending':
        return <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-purple-100 text-purple-800">KYC Required</span>
      case 'Approved':
        return <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-emerald-100 text-emerald-800">Approved</span>
      case 'Rejected':
        return <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-rose-100 text-rose-800">Rejected</span>
      case 'Withdrawn':
        return <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-slate-100 text-slate-700">Withdrawn</span>
      default:
        return <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-slate-100 text-slate-800">{status}</span>
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
        <span className="ml-3 text-slate-600">Loading your banking overview...</span>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      {/* Overview Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <div className="bg-white rounded-xl shadow-sm border border-slate-200 p-5 flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Total Applications</span>
            <div className="text-2xl font-bold text-slate-900 mt-1">{dashboard?.totalApplications ?? 0}</div>
          </div>
          <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
            <FileText className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-slate-200 p-5 flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Under Review</span>
            <div className="text-2xl font-bold text-amber-600 mt-1">{dashboard?.pendingApplications ?? 0}</div>
          </div>
          <div className="p-3 bg-amber-50 text-amber-600 rounded-lg">
            <Clock className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-slate-200 p-5 flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Approved</span>
            <div className="text-2xl font-bold text-emerald-600 mt-1">{dashboard?.approvedApplications ?? 0}</div>
          </div>
          <div className="p-3 bg-emerald-50 text-emerald-600 rounded-lg">
            <CheckCircle2 className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-slate-200 p-5 flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Rejected / Closed</span>
            <div className="text-2xl font-bold text-rose-600 mt-1">{dashboard?.rejectedApplications ?? 0}</div>
          </div>
          <div className="p-3 bg-rose-50 text-rose-600 rounded-lg">
            <XCircle className="w-6 h-6" />
          </div>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 rounded-lg flex items-center space-x-3 text-rose-800 text-sm">
          <AlertCircle className="w-5 h-5 flex-shrink-0 text-rose-600" />
          <span>{error}</span>
        </div>
      )}

      {/* Applications Table */}
      <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div className="p-6 border-b border-slate-200 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h3 className="text-lg font-bold text-slate-900">Your Product Applications</h3>
            <p className="text-xs text-slate-500">Track real-time underwriting status and history timeline.</p>
          </div>
          <button
            onClick={onNavigateToProducts}
            className="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-xs font-medium shadow-sm transition-colors"
          >
            Explore Banking Products
            <ArrowUpRight className="w-4 h-4 ml-1.5" />
          </button>
        </div>

        {(!dashboard?.recentApplications || dashboard.recentApplications.length === 0) ? (
          <div className="text-center py-12 px-4">
            <FileText className="w-12 h-12 text-slate-300 mx-auto mb-3" />
            <h4 className="text-base font-semibold text-slate-700">No Applications Yet</h4>
            <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
              You haven't submitted any banking product applications. Browse our exclusive offerings to get started.
            </p>
            <button
              onClick={onNavigateToProducts}
              className="mt-4 px-4 py-2 bg-blue-600 text-white text-xs font-medium rounded-lg hover:bg-blue-700"
            >
              Browse Products
            </button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-500 tracking-wider border-b border-slate-200">
                <tr>
                  <th className="px-6 py-3.5">Application No.</th>
                  <th className="px-6 py-3.5">Product & Tier</th>
                  <th className="px-6 py-3.5">Amount (INR)</th>
                  <th className="px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5">Submitted On</th>
                  <th className="px-6 py-3.5 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {dashboard.recentApplications.map((app) => (
                  <tr key={app.id} className="hover:bg-slate-50 transition-colors">
                    <td className="px-6 py-4 font-mono font-semibold text-blue-600 text-xs">
                      {app.applicationNumber}
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-medium text-slate-900">{app.productName}</div>
                      <div className="text-xs text-slate-400">{app.productCategory}</div>
                    </td>
                    <td className="px-6 py-4 font-semibold text-slate-900">
                      ₹{app.requestedAmount.toLocaleString()}
                    </td>
                    <td className="px-6 py-4">{getStatusBadge(app.status)}</td>
                    <td className="px-6 py-4 text-xs text-slate-500">
                      {new Date(app.submittedAt).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                      })}
                    </td>
                    <td className="px-6 py-4 text-right space-x-2">
                      <button
                        onClick={() => handleOpenTimeline(app)}
                        className="inline-flex items-center px-2.5 py-1.5 text-xs font-medium text-slate-700 bg-white border border-slate-300 rounded hover:bg-slate-50"
                        title="View Audit Timeline"
                      >
                        <History className="w-3.5 h-3.5 mr-1 text-slate-500" />
                        Timeline
                      </button>

                      {(app.status === 'Submitted' || app.status === 'KycPending') && (
                        <button
                          onClick={() => setWithdrawingAppId(app.id)}
                          className="inline-flex items-center px-2.5 py-1.5 text-xs font-medium text-rose-700 bg-rose-50 border border-rose-200 rounded hover:bg-rose-100"
                          title="Withdraw Application"
                        >
                          <Ban className="w-3.5 h-3.5 mr-1" />
                          Withdraw
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* TIMELINE AUDIT MODAL */}
      {selectedApp && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 max-w-lg w-full max-h-[85vh] overflow-y-auto p-6">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100 mb-6">
              <div>
                <h3 className="text-base font-bold text-slate-900">Application Lifecycle Timeline</h3>
                <p className="text-xs font-mono text-blue-600">{selectedApp.applicationNumber}</p>
              </div>
              <button
                onClick={() => setSelectedApp(null)}
                className="p-1 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {loadingTimeline ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="w-6 h-6 text-blue-600 animate-spin" />
              </div>
            ) : timelines.length === 0 ? (
              <p className="text-xs text-slate-500 text-center py-6">No status transitions recorded.</p>
            ) : (
              <div className="relative border-l-2 border-slate-200 ml-4 pl-6 space-y-6">
                {timelines.map((item, idx) => (
                  <div key={item.id || idx} className="relative">
                    <div className="absolute -left-[31px] top-1 w-4 h-4 rounded-full bg-blue-600 border-2 border-white shadow-sm" />
                    <div className="text-xs font-bold text-slate-900">
                      {item.fromStatus === item.toStatus ? `Status: ${item.toStatus}` : `${item.fromStatus} → ${item.toStatus}`}
                    </div>
                    <div className="text-[11px] text-slate-400">
                      Changed by: <span className="font-semibold text-slate-600">{item.changedByRole}</span> •{' '}
                      {new Date(item.timestamp).toLocaleString()}
                    </div>
                    {item.comments && (
                      <div className="mt-1.5 p-2.5 bg-slate-50 border border-slate-200 rounded text-xs text-slate-700 italic">
                        "{item.comments}"
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* WITHDRAW MODAL */}
      {withdrawingAppId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 max-w-md w-full p-6">
            <h3 className="text-lg font-bold text-slate-900 mb-2">Withdraw Banking Application</h3>
            <p className="text-xs text-slate-500 mb-4">
              Are you sure you want to withdraw this application? This action moves the application to a terminal state.
            </p>

            <form onSubmit={handleWithdrawSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-slate-700 mb-1">
                  Reason for Withdrawal (Optional)
                </label>
                <textarea
                  rows={3}
                  value={withdrawRemarks}
                  onChange={(e) => setWithdrawRemarks(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg text-slate-900 text-xs focus:ring-2 focus:ring-rose-500 focus:outline-none"
                  placeholder="e.g. Terms no longer needed, applying for another product..."
                />
              </div>

              <div className="flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={() => setWithdrawingAppId(null)}
                  className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg text-xs font-medium hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={withdrawing}
                  className="px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-lg text-xs font-medium disabled:opacity-50 inline-flex items-center"
                >
                  {withdrawing && <Loader2 className="w-3.5 h-3.5 mr-1.5 animate-spin" />}
                  Confirm Withdrawal
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
