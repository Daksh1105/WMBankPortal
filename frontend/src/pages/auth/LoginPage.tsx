import React, { useState } from 'react'
import { Building2, ShieldCheck, Lock, Mail, AlertCircle, ArrowRight, UserCheck } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import type { AxiosError } from 'axios'
import type { ProblemDetails } from '../../types/auth'

interface LoginPageProps {
  onSwitchToRegister: () => void
}

export const LoginPage: React.FC<LoginPageProps> = ({ onSwitchToRegister }) => {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErrorMessage(null)
    setIsLoading(true)

    try {
      await login(email, password)
    } catch (err) {
      const axiosErr = err as AxiosError<ProblemDetails>
      const detail = axiosErr.response?.data?.detail || 'Invalid email or password.'
      setErrorMessage(detail)
    } finally {
      setIsLoading(false)
    }
  }

  const fillDemoAccount = (demoEmail: string) => {
    setEmail(demoEmail)
    setPassword('BankDemo123!Secure#')
    setErrorMessage(null)
  }

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
        {/* Header */}
        <div className="bg-gradient-to-r from-slate-900 to-indigo-950 p-6 text-white text-center">
          <div className="inline-flex p-3 bg-white/10 rounded-2xl backdrop-blur-sm mb-3">
            <Building2 className="w-8 h-8 text-indigo-400" />
          </div>
          <h2 className="text-xl font-bold tracking-tight">WM Customer Portal</h2>
          <p className="text-xs text-indigo-200/80 mt-1">
            Secure Wealth Management & Banking Services
          </p>
        </div>

        {/* Form Body */}
        <div className="p-6 sm:p-8 space-y-6">
          {errorMessage && (
            <div className="flex items-start space-x-2.5 p-3.5 bg-rose-50 border border-rose-200 rounded-xl text-rose-700 text-xs">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{errorMessage}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Work / Customer Email
              </label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="name@wmportal.com"
                  className="w-full pl-10 pr-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white transition"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Account Password
              </label>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••••••"
                  className="w-full pl-10 pr-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white transition"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-60 text-white font-medium text-sm rounded-xl shadow-md shadow-indigo-600/20 flex items-center justify-center space-x-2 transition cursor-pointer"
            >
              {isLoading ? (
                <span>Authenticating...</span>
              ) : (
                <>
                  <span>Sign In to Portal</span>
                  <ArrowRight className="w-4 h-4" />
                </>
              )}
            </button>
          </form>

          {/* Demo Accounts Quick-Select */}
          <div className="pt-2 border-t border-slate-100">
            <div className="flex items-center space-x-1 text-slate-500 text-xs font-medium mb-2.5">
              <UserCheck className="w-3.5 h-3.5 text-indigo-500" />
              <span>Assessment Demo Accounts (Click to Autofill):</span>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => fillDemoAccount('customer@wmportal.com')}
                className="text-left p-2 rounded-lg bg-slate-50 hover:bg-indigo-50/60 border border-slate-200/80 hover:border-indigo-200 text-xs transition cursor-pointer"
              >
                <div className="font-semibold text-slate-800">Customer</div>
                <div className="text-[10px] text-slate-500 truncate">customer@wmportal.com</div>
              </button>

              <button
                type="button"
                onClick={() => fillDemoAccount('officer@wmportal.com')}
                className="text-left p-2 rounded-lg bg-slate-50 hover:bg-emerald-50/60 border border-slate-200/80 hover:border-emerald-200 text-xs transition cursor-pointer"
              >
                <div className="font-semibold text-slate-800">Bank Officer</div>
                <div className="text-[10px] text-slate-500 truncate">officer@wmportal.com</div>
              </button>

              <button
                type="button"
                onClick={() => fillDemoAccount('agent@wmportal.com')}
                className="text-left p-2 rounded-lg bg-slate-50 hover:bg-amber-50/60 border border-slate-200/80 hover:border-amber-200 text-xs transition cursor-pointer"
              >
                <div className="font-semibold text-slate-800">Support Agent</div>
                <div className="text-[10px] text-slate-500 truncate">agent@wmportal.com</div>
              </button>

              <button
                type="button"
                onClick={() => fillDemoAccount('admin@wmportal.com')}
                className="text-left p-2 rounded-lg bg-slate-50 hover:bg-purple-50/60 border border-slate-200/80 hover:border-purple-200 text-xs transition cursor-pointer"
              >
                <div className="font-semibold text-slate-800">Administrator</div>
                <div className="text-[10px] text-slate-500 truncate">admin@wmportal.com</div>
              </button>
            </div>
          </div>

          {/* Switch to Register */}
          <div className="text-center pt-2 text-xs text-slate-500">
            Don't have an active account yet?{' '}
            <button
              type="button"
              onClick={onSwitchToRegister}
              className="text-indigo-600 font-semibold hover:underline cursor-pointer"
            >
              Register here
            </button>
          </div>
        </div>

        {/* Security Footer */}
        <div className="bg-slate-50 px-6 py-3 border-t border-slate-100 flex items-center justify-center space-x-1.5 text-[11px] text-slate-400">
          <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
          <span>In-Memory JWT & HttpOnly SameSite=Strict Protected Session</span>
        </div>
      </div>
    </div>
  )
}
