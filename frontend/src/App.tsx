import { useState } from 'react'
import { 
  Building2, 
  Shield, 
  LogOut, 
  User, 
  Key, 
  Lock, 
  RefreshCw, 
  LayoutDashboard, 
  CreditCard, 
  UserCheck, 
  Cpu, 
  CheckCircle2 
} from 'lucide-react'
import { AuthProvider, useAuth } from './context/AuthContext'
import { LoginPage } from './pages/auth/LoginPage'
import { RegisterPage } from './pages/auth/RegisterPage'
import { CustomerDashboardPage } from './pages/customer/CustomerDashboardPage'
import { ProductCatalogPage } from './pages/customer/ProductCatalogPage'
import { ProfilePage } from './pages/customer/ProfilePage'
import { NotificationBell } from './components/notifications/NotificationBell'
import { apiClient } from './api/client'

type ActiveTab = 'dashboard' | 'products' | 'profile' | 'security'

function MainContent() {
  const { user, isAuthenticated, isLoading, logout } = useAuth()
  const [authView, setAuthView] = useState<'login' | 'register'>('login')
  const [activeTab, setActiveTab] = useState<ActiveTab>('dashboard')
  const [apiStatus, setApiStatus] = useState<string | null>(null)
  const [testingApi, setTestingApi] = useState(false)

  const handleTestApi = async () => {
    setTestingApi(true)
    try {
      const res = await apiClient.get('/api/v1/system/info')
      setApiStatus(
        `API Response: ${res.data.applicationName} (${res.data.status}) at ${new Date(
          res.data.serverTimeUtc
        ).toLocaleTimeString()}`
      )
    } catch {
      setApiStatus('API call failed.')
    } finally {
      setTestingApi(false)
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-900 flex items-center justify-center text-white">
        <div className="flex items-center space-x-3">
          <RefreshCw className="w-6 h-6 animate-spin text-blue-400" />
          <span className="text-sm font-medium">Restoring secure session...</span>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-blue-950 flex flex-col items-center justify-center p-4">
        {authView === 'login' ? (
          <LoginPage onSwitchToRegister={() => setAuthView('register')} />
        ) : (
          <RegisterPage onSwitchToLogin={() => setAuthView('login')} />
        )}
      </div>
    )
  }

  const roleBadgeColors: Record<string, string> = {
    Customer: 'bg-blue-50 text-blue-700 border-blue-200',
    Officer: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    SupportAgent: 'bg-amber-50 text-amber-700 border-amber-200',
    Admin: 'bg-purple-50 text-purple-700 border-purple-200',
  }

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col text-slate-800">
      {/* Top Banking Navigation Bar */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-30 shadow-xs">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          <div className="flex items-center space-x-3 text-blue-600">
            <div className="p-2 bg-blue-50 rounded-xl">
              <Building2 className="w-6 h-6" />
            </div>
            <div>
              <span className="font-bold text-slate-900 text-base sm:text-lg tracking-tight">
                WM Bank Portal
              </span>
              <span className="hidden sm:inline-block ml-2 text-xs text-slate-400 font-normal">
                Wealth Management Platform
              </span>
            </div>
          </div>

          <div className="flex items-center space-x-3">
            <div className="hidden sm:flex flex-col items-end text-right">
              <span className="text-xs font-semibold text-slate-900">{user?.email}</span>
              <span className="text-[10px] text-slate-400 font-mono">ID: {user?.id.slice(0, 8)}...</span>
            </div>

            <NotificationBell onNavigateToApplication={() => setActiveTab('dashboard')} />

            <span
              className={`text-xs font-semibold px-2.5 py-1 rounded-full border ${
                roleBadgeColors[user?.role || 'Customer'] || 'bg-slate-100 text-slate-700 border-slate-200'
              }`}
            >
              {user?.role}
            </span>

            <button
              onClick={logout}
              title="Sign Out"
              className="p-2 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition cursor-pointer"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Primary Navigation Tabs */}
        <div className="max-w-6xl mx-auto px-4 sm:px-6 flex space-x-6 border-t border-slate-100 text-xs font-semibold">
          <button
            onClick={() => setActiveTab('dashboard')}
            className={`py-3 inline-flex items-center space-x-2 border-b-2 transition-colors cursor-pointer ${
              activeTab === 'dashboard'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <LayoutDashboard className="w-4 h-4" />
            <span>Dashboard</span>
          </button>

          <button
            onClick={() => setActiveTab('products')}
            className={`py-3 inline-flex items-center space-x-2 border-b-2 transition-colors cursor-pointer ${
              activeTab === 'products'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <CreditCard className="w-4 h-4" />
            <span>Banking Products</span>
          </button>

          <button
            onClick={() => setActiveTab('profile')}
            className={`py-3 inline-flex items-center space-x-2 border-b-2 transition-colors cursor-pointer ${
              activeTab === 'profile'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <UserCheck className="w-4 h-4" />
            <span>My Profile</span>
          </button>

          <button
            onClick={() => setActiveTab('security')}
            className={`py-3 inline-flex items-center space-x-2 border-b-2 transition-colors cursor-pointer ${
              activeTab === 'security'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <Cpu className="w-4 h-4" />
            <span>Architecture & Security</span>
          </button>
        </div>
      </header>

      {/* Main Tab Content */}
      <main className="flex-1 max-w-6xl w-full mx-auto px-4 sm:px-6 py-8">
        {activeTab === 'dashboard' && (
          <CustomerDashboardPage onNavigateToProducts={() => setActiveTab('products')} />
        )}

        {activeTab === 'products' && (
          <ProductCatalogPage onApplicationSubmitted={() => setActiveTab('dashboard')} />
        )}

        {activeTab === 'profile' && <ProfilePage />}

        {activeTab === 'security' && (
          <div className="space-y-6">
            {/* Architecture Banner */}
            <div className="bg-gradient-to-r from-slate-900 via-blue-950 to-slate-900 rounded-3xl p-6 sm:p-8 text-white shadow-xl relative overflow-hidden">
              <div className="relative z-10 space-y-2">
                <span className="inline-flex items-center space-x-1.5 px-3 py-1 bg-white/10 rounded-full text-xs font-medium text-blue-300">
                  <Shield className="w-3.5 h-3.5" />
                  <span>Phase 2 Active: Clean Architecture Application Engine</span>
                </span>
                <h1 className="text-2xl sm:text-3xl font-bold tracking-tight">
                  Security & Architecture Invariants
                </h1>
                <p className="text-slate-300 text-sm max-w-2xl leading-relaxed">
                  Every product application features optimistic concurrency (<code className="text-blue-300">RowVersion</code>),
                  high-entropy application number collision retries, and strict resource-based IDOR authorization handlers.
                </p>
              </div>
            </div>

            {/* Architecture Cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
              <div className="bg-white rounded-2xl border border-slate-200/80 p-5 shadow-xs space-y-3">
                <div className="flex items-center space-x-2.5 text-blue-600">
                  <Key className="w-5 h-5" />
                  <h3 className="font-semibold text-sm text-slate-900">In-Memory JWT Access</h3>
                </div>
                <p className="text-xs text-slate-500 leading-relaxed">
                  Access tokens are stored strictly in JavaScript runtime memory. No tokens are written to localStorage, immunizing the session against XSS credential exfiltration.
                </p>
                <div className="text-[11px] font-mono text-emerald-600 flex items-center space-x-1">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>15-Minute Expiration</span>
                </div>
              </div>

              <div className="bg-white rounded-2xl border border-slate-200/80 p-5 shadow-xs space-y-3">
                <div className="flex items-center space-x-2.5 text-emerald-600">
                  <RefreshCw className="w-5 h-5" />
                  <h3 className="font-semibold text-sm text-slate-900">Hashed Refresh Tokens</h3>
                </div>
                <p className="text-xs text-slate-500 leading-relaxed">
                  Refresh tokens are stored as SHA-256 hashes in SQL Server with token family tracking. Replayed tokens trigger instant cascading session revocation.
                </p>
                <div className="text-[11px] font-mono text-emerald-600 flex items-center space-x-1">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>SameSite=Strict HttpOnly Cookie</span>
                </div>
              </div>

              <div className="bg-white rounded-2xl border border-slate-200/80 p-5 shadow-xs space-y-3">
                <div className="flex items-center space-x-2.5 text-purple-600">
                  <Lock className="w-5 h-5" />
                  <h3 className="font-semibold text-sm text-slate-900">IDOR Defense Boundary</h3>
                </div>
                <p className="text-xs text-slate-500 leading-relaxed">
                  Applications verify customer ownership in the domain layer. Customer B attempting to query Customer A's application by ID receives an immediate HTTP 403 Forbidden.
                </p>
                <div className="text-[11px] font-mono text-emerald-600 flex items-center space-x-1">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>Ownership Handlers Enforced</span>
                </div>
              </div>
            </div>

            {/* Live Authenticated API Verification Panel */}
            <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-xs space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-sm font-bold text-slate-900">Live API Authentication Probe</h2>
                  <p className="text-xs text-slate-500">
                    Execute an authenticated HTTP request using your in-memory Bearer token.
                  </p>
                </div>
                <button
                  onClick={handleTestApi}
                  disabled={testingApi}
                  className="py-2 px-4 bg-slate-900 hover:bg-slate-800 disabled:opacity-50 text-white rounded-xl text-xs font-semibold shadow-xs transition flex items-center space-x-1.5 cursor-pointer"
                >
                  {testingApi ? (
                    <>
                      <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                      <span>Probing API...</span>
                    </>
                  ) : (
                    <span>Send Authenticated Request</span>
                  )}
                </button>
              </div>

              {apiStatus && (
                <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-slate-700">
                  {apiStatus}
                </div>
              )}
            </div>

            {/* User Claims Detail */}
            <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-xs space-y-3">
              <div className="flex items-center space-x-2 text-slate-700">
                <User className="w-4 h-4 text-blue-600" />
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-600">
                  Active Security Claims
                </h2>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs">
                <div className="p-3 bg-slate-50 rounded-xl border border-slate-100">
                  <span className="text-[10px] text-slate-400 uppercase font-semibold block">User Identifier</span>
                  <span className="font-mono text-slate-800 font-medium break-all">{user?.id}</span>
                </div>
                <div className="p-3 bg-slate-50 rounded-xl border border-slate-100">
                  <span className="text-[10px] text-slate-400 uppercase font-semibold block">Verified Email</span>
                  <span className="font-mono text-slate-800 font-medium break-all">{user?.email}</span>
                </div>
                <div className="p-3 bg-slate-50 rounded-xl border border-slate-100">
                  <span className="text-[10px] text-slate-400 uppercase font-semibold block">Authorized Role</span>
                  <span className="font-mono text-slate-800 font-medium break-all">{user?.role}</span>
                </div>
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <MainContent />
    </AuthProvider>
  )
}
