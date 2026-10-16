import { Shield, Sparkles, Building2, CheckCircle2 } from 'lucide-react'

export default function App() {
  return (
    <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-6 text-slate-800">
      <div className="max-w-2xl w-full bg-white rounded-2xl shadow-xl border border-slate-200 p-8 space-y-6">
        <div className="flex items-center space-x-3 text-indigo-600">
          <Building2 className="w-8 h-8" />
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">
            WM Wealth Management Banking Portal
          </h1>
        </div>

        <p className="text-slate-600 leading-relaxed">
          Welcome to the enterprise customer portal and AI support co-pilot. Phase 0 setup is active and running cleanly on 
          <span className="font-semibold text-slate-900"> .NET 10 Web API</span> + 
          <span className="font-semibold text-slate-900"> React 19 / Vite</span>.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
          <div className="flex items-start space-x-3 p-4 rounded-xl bg-slate-50 border border-slate-100">
            <Shield className="w-5 h-5 text-emerald-600 mt-0.5 shrink-0" />
            <div>
              <h3 className="font-semibold text-sm text-slate-900">Security In-Depth</h3>
              <p className="text-xs text-slate-500 mt-1">
                SameSite=Strict cookie proxy, CSP headers, rate limiting, and PII masking pipeline.
              </p>
            </div>
          </div>

          <div className="flex items-start space-x-3 p-4 rounded-xl bg-slate-50 border border-slate-100">
            <Sparkles className="w-5 h-5 text-indigo-600 mt-0.5 shrink-0" />
            <div>
              <h3 className="font-semibold text-sm text-slate-900">AI Co-Pilot Ready</h3>
              <p className="text-xs text-slate-500 mt-1">
                Heuristic PII sanitization engine, prompt injection guard, and editable agent drafts.
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between border-t border-slate-100 pt-4 text-xs text-slate-500">
          <span className="flex items-center space-x-1.5 text-emerald-600 font-medium">
            <CheckCircle2 className="w-4 h-4" />
            <span>Phase 0: Tooling & Solution Skeleton Verified</span>
          </span>
          <span className="font-mono text-slate-400">v0.1.0-dev</span>
        </div>
      </div>
    </div>
  )
}
