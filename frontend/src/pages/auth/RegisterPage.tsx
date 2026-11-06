import React, { useState } from 'react'
import { Building2, ShieldCheck, Lock, Mail, AlertCircle, ArrowRight, Check, X } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import type { AxiosError } from 'axios'
import type { ProblemDetails } from '../../types/auth'

interface RegisterPageProps {
  onSwitchToLogin: () => void
}

export const RegisterPage: React.FC<RegisterPageProps> = ({ onSwitchToLogin }) => {
  const { register } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  // Password Policy Checks (Mirrors backend FluentValidation requirements)
  const hasMinLength = password.length >= 10
  const hasUpper = /[A-Z]/.test(password)
  const hasLower = /[a-z]/.test(password)
  const hasDigit = /[0-9]/.test(password)
  const hasSpecial = /[!?*@#$%^&+=_.,\-]/.test(password)
  const passwordsMatch = password.length > 0 && password === confirmPassword
  const isPasswordValid = hasMinLength && hasUpper && hasLower && hasDigit && hasSpecial

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErrorMessage(null)

    if (!isPasswordValid) {
      setErrorMessage('Password does not fulfill all banking security requirements.')
      return
    }

    if (!passwordsMatch) {
      setErrorMessage('Passwords do not match.')
      return
    }

    setIsLoading(true)
    try {
      await register(email, password, confirmPassword)
    } catch (err) {
      const axiosErr = err as AxiosError<ProblemDetails>
      const detail = axiosErr.response?.data?.detail || 'Registration failed. Please check your details.'
      setErrorMessage(detail)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 overflow-hidden">
        {/* Header */}
        <div className="bg-gradient-to-r from-slate-900 to-indigo-950 p-6 text-white text-center">
          <div className="inline-flex p-3 bg-white/10 rounded-2xl backdrop-blur-sm mb-3">
            <Building2 className="w-8 h-8 text-indigo-400" />
          </div>
          <h2 className="text-xl font-bold tracking-tight">Create Customer Account</h2>
          <p className="text-xs text-indigo-200/80 mt-1">
            Open your secure Wealth Management account
          </p>
        </div>

        {/* Form Body */}
        <div className="p-6 sm:p-8 space-y-5">
          {errorMessage && (
            <div className="flex items-start space-x-2.5 p-3.5 bg-rose-50 border border-rose-200 rounded-xl text-rose-700 text-xs">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{errorMessage}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Email Address
              </label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="your.email@example.com"
                  className="w-full pl-10 pr-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white transition"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                New Password
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

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Confirm Password
              </label>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="password"
                  required
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  placeholder="••••••••••••"
                  className="w-full pl-10 pr-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white transition"
                />
              </div>
            </div>

            {/* Password Policy Checklist */}
            <div className="p-3 bg-slate-50 rounded-xl border border-slate-200/80 space-y-1.5 text-xs text-slate-600">
              <div className="font-semibold text-slate-800 text-[11px] uppercase tracking-wider">
                Password Security Policy:
              </div>
              <div className="grid grid-cols-2 gap-1.5 text-[11px]">
                <span className={`flex items-center space-x-1 ${hasMinLength ? 'text-emerald-600 font-medium' : 'text-slate-400'}`}>
                  {hasMinLength ? <Check className="w-3.5 h-3.5" /> : <X className="w-3.5 h-3.5" />}
                  <span>Min 10 characters</span>
                </span>
                <span className={`flex items-center space-x-1 ${hasUpper ? 'text-emerald-600 font-medium' : 'text-slate-400'}`}>
                  {hasUpper ? <Check className="w-3.5 h-3.5" /> : <X className="w-3.5 h-3.5" />}
                  <span>Uppercase (A-Z)</span>
                </span>
                <span className={`flex items-center space-x-1 ${hasLower ? 'text-emerald-600 font-medium' : 'text-slate-400'}`}>
                  {hasLower ? <Check className="w-3.5 h-3.5" /> : <X className="w-3.5 h-3.5" />}
                  <span>Lowercase (a-z)</span>
                </span>
                <span className={`flex items-center space-x-1 ${hasDigit ? 'text-emerald-600 font-medium' : 'text-slate-400'}`}>
                  {hasDigit ? <Check className="w-3.5 h-3.5" /> : <X className="w-3.5 h-3.5" />}
                  <span>Number (0-9)</span>
                </span>
                <span className={`flex items-center space-x-1 ${hasSpecial ? 'text-emerald-600 font-medium' : 'text-slate-400'}`}>
                  {hasSpecial ? <Check className="w-3.5 h-3.5" /> : <X className="w-3.5 h-3.5" />}
                  <span>Special symbol (!?*...)</span>
                </span>
                <span className={`flex items-center space-x-1 ${passwordsMatch ? 'text-emerald-600 font-medium' : 'text-slate-400'}`}>
                  {passwordsMatch ? <Check className="w-3.5 h-3.5" /> : <X className="w-3.5 h-3.5" />}
                  <span>Passwords match</span>
                </span>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading || !isPasswordValid || !passwordsMatch}
              className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-medium text-sm rounded-xl shadow-md shadow-indigo-600/20 flex items-center justify-center space-x-2 transition cursor-pointer"
            >
              {isLoading ? (
                <span>Creating Account...</span>
              ) : (
                <>
                  <span>Create Account</span>
                  <ArrowRight className="w-4 h-4" />
                </>
              )}
            </button>
          </form>

          {/* Switch to Login */}
          <div className="text-center pt-2 text-xs text-slate-500">
            Already registered?{' '}
            <button
              type="button"
              onClick={onSwitchToLogin}
              className="text-indigo-600 font-semibold hover:underline cursor-pointer"
            >
              Sign in to your account
            </button>
          </div>
        </div>

        {/* Security Footer */}
        <div className="bg-slate-50 px-6 py-3 border-t border-slate-100 flex items-center justify-center space-x-1.5 text-[11px] text-slate-400">
          <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
          <span>Compliant with Banking Password & Identity Standards</span>
        </div>
      </div>
    </div>
  )
}
