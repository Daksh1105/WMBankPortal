import React, { useState, useEffect } from 'react'
import { apiClient } from '../../api/client'
import type { BankingProduct, FormFieldSchema, BankingApplication } from '../../types/banking'
import { 
  Building2, 
  CreditCard, 
  TrendingUp, 
  ShieldCheck, 
  ArrowRight, 
  CheckCircle2, 
  AlertCircle, 
  Loader2, 
  X,
  FileText
} from 'lucide-react'

interface ProductCatalogPageProps {
  onApplicationSubmitted?: (app: BankingApplication) => void
}

export const ProductCatalogPage: React.FC<ProductCatalogPageProps> = ({ onApplicationSubmitted }) => {
  const [products, setProducts] = useState<BankingProduct[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Application Modal state
  const [selectedProduct, setSelectedProduct] = useState<BankingProduct | null>(null)
  const [requestedAmount, setRequestedAmount] = useState<number>(0)
  const [dynamicFormValues, setDynamicFormValues] = useState<Record<string, any>>({})
  const [submitting, setSubmitting] = useState(false)
  const [modalError, setModalError] = useState<string | null>(null)
  const [submittedApp, setSubmittedApp] = useState<BankingApplication | null>(null)

  useEffect(() => {
    fetchProducts()
  }, [])

  const fetchProducts = async () => {
    try {
      setLoading(true)
      const res = await apiClient.get<BankingProduct[]>('/api/v1/products')
      setProducts(res.data)
    } catch (err: any) {
      setError('Unable to retrieve banking product offerings. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const handleOpenApplyModal = (product: BankingProduct) => {
    setSelectedProduct(product)
    setRequestedAmount(product.minAmount)
    setModalError(null)
    setSubmittedApp(null)

    // Parse dynamic schema to initialize default form values
    try {
      const schema: FormFieldSchema[] = JSON.parse(product.dynamicFormSchemaJson || '[]')
      const initialValues: Record<string, any> = {}
      schema.forEach((field) => {
        if (field.type === 'select' && field.options && field.options.length > 0) {
          initialValues[field.id] = field.options[0]
        } else if (field.type === 'number') {
          initialValues[field.id] = field.min ?? 0
        } else {
          initialValues[field.id] = ''
        }
      })
      setDynamicFormValues(initialValues)
    } catch {
      setDynamicFormValues({})
    }
  }

  const handleDynamicFieldChange = (fieldId: string, value: any) => {
    setDynamicFormValues((prev) => ({
      ...prev,
      [fieldId]: value,
    }))
  }

  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedProduct) return

    setModalError(null)

    // Validate amount
    if (requestedAmount < selectedProduct.minAmount || requestedAmount > selectedProduct.maxAmount) {
      setModalError(
        `Requested amount must be between ₹${selectedProduct.minAmount.toLocaleString()} and ₹${selectedProduct.maxAmount.toLocaleString()}.`
      )
      return
    }

    try {
      setSubmitting(true)
      const payload = {
        productId: selectedProduct.id,
        requestedAmount: Number(requestedAmount),
        formValuesJson: JSON.stringify(dynamicFormValues),
      }

      const res = await apiClient.post<BankingApplication>('/api/v1/applications', payload)
      setSubmittedApp(res.data)
      if (onApplicationSubmitted) {
        onApplicationSubmitted(res.data)
      }
    } catch (err: any) {
      const msg = err.response?.data?.detail || err.response?.data?.title || 'Application submission failed.'
      setModalError(msg)
    } finally {
      setSubmitting(false)
    }
  }

  const getProductIcon = (code: string) => {
    if (code.startsWith('SAV')) return <Building2 className="w-6 h-6 text-emerald-600" />
    if (code.startsWith('CC')) return <CreditCard className="w-6 h-6 text-purple-600" />
    if (code.startsWith('LOAN')) return <TrendingUp className="w-6 h-6 text-blue-600" />
    return <ShieldCheck className="w-6 h-6 text-amber-600" />
  }

  const parsedSchema: FormFieldSchema[] = selectedProduct
    ? (() => {
        try {
          return JSON.parse(selectedProduct.dynamicFormSchemaJson)
        } catch {
          return []
        }
      })()
    : []

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
        <span className="ml-3 text-slate-600">Loading banking products...</span>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Wealth Banking Products</h2>
        <p className="text-sm text-slate-500">
          Select an exclusive banking product tailored to your financial and wealth management goals.
        </p>
      </div>

      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 rounded-lg flex items-center space-x-3 text-rose-800 text-sm">
          <AlertCircle className="w-5 h-5 flex-shrink-0 text-rose-600" />
          <span>{error}</span>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {products.map((product) => (
          <div
            key={product.id}
            className="bg-white rounded-xl shadow-sm border border-slate-200 hover:border-blue-400 hover:shadow-md transition-all p-6 flex flex-col justify-between"
          >
            <div>
              <div className="flex items-center justify-between mb-4">
                <div className="p-3 bg-slate-50 border border-slate-100 rounded-lg">
                  {getProductIcon(product.code)}
                </div>
                <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-blue-50 text-blue-700 uppercase tracking-wider">
                  {product.category}
                </span>
              </div>

              <h3 className="text-lg font-bold text-slate-900 mb-1">{product.name}</h3>
              <p className="text-xs font-mono text-slate-400 mb-4">Code: {product.code}</p>

              <div className="grid grid-cols-2 gap-3 py-3 border-y border-slate-100 mb-4 text-xs">
                <div>
                  <span className="text-slate-500 block">Interest Rate</span>
                  <span className="font-semibold text-slate-900 text-sm">{product.interestRate}% p.a.</span>
                </div>
                <div>
                  <span className="text-slate-500 block">Eligible Range</span>
                  <span className="font-semibold text-slate-900">
                    ₹{product.minAmount.toLocaleString()} - ₹{product.maxAmount.toLocaleString()}
                  </span>
                </div>
              </div>
            </div>

            <button
              onClick={() => handleOpenApplyModal(product)}
              className="w-full inline-flex items-center justify-center px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg text-sm transition-colors group"
            >
              Apply Online
              <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 transition-transform" />
            </button>
          </div>
        ))}
      </div>

      {/* DYNAMIC APPLICATION FORM MODAL */}
      {selectedProduct && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 max-w-xl w-full max-h-[90vh] overflow-y-auto p-6 sm:p-8">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100 mb-6">
              <div className="flex items-center space-x-3">
                <div className="p-2.5 bg-blue-50 rounded-lg text-blue-600">
                  <FileText className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="text-lg font-bold text-slate-900">Apply: {selectedProduct.name}</h3>
                  <p className="text-xs text-slate-500">Tier: {selectedProduct.category} ({selectedProduct.code})</p>
                </div>
              </div>
              <button
                onClick={() => setSelectedProduct(null)}
                className="p-1 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {submittedApp ? (
              <div className="text-center py-6 space-y-4">
                <div className="w-14 h-14 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto">
                  <CheckCircle2 className="w-8 h-8" />
                </div>
                <h4 className="text-xl font-bold text-slate-900">Application Submitted!</h4>
                <p className="text-sm text-slate-600 max-w-md mx-auto">
                  Your application has been received and routed to our wealth underwriting desk.
                </p>

                <div className="bg-slate-50 border border-slate-200 rounded-xl p-4 max-w-xs mx-auto text-left space-y-2">
                  <div className="text-xs text-slate-500">Application Tracking Number:</div>
                  <div className="text-base font-mono font-bold text-blue-700">{submittedApp.applicationNumber}</div>
                  <div className="text-xs text-slate-500 pt-1">
                    Status: <span className="font-semibold text-emerald-600">{submittedApp.status}</span>
                  </div>
                </div>

                <div className="pt-4">
                  <button
                    onClick={() => setSelectedProduct(null)}
                    className="px-6 py-2.5 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg text-sm"
                  >
                    Close & View Dashboard
                  </button>
                </div>
              </div>
            ) : (
              <form onSubmit={handleFormSubmit} className="space-y-5">
                {modalError && (
                  <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-lg flex items-center space-x-2 text-rose-800 text-xs">
                    <AlertCircle className="w-4 h-4 flex-shrink-0 text-rose-600" />
                    <span>{modalError}</span>
                  </div>
                )}

                {/* Base Amount field */}
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-slate-700 mb-1">
                    Requested Amount / Deposit (INR)
                  </label>
                  <input
                    type="number"
                    required
                    min={selectedProduct.minAmount}
                    max={selectedProduct.maxAmount}
                    value={requestedAmount}
                    onChange={(e) => setRequestedAmount(Number(e.target.value))}
                    className="w-full px-3.5 py-2.5 border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-600 text-sm"
                  />
                  <span className="text-[11px] text-slate-500 mt-1 block">
                    Eligible range: ₹{selectedProduct.minAmount.toLocaleString()} to ₹
                    {selectedProduct.maxAmount.toLocaleString()}
                  </span>
                </div>

                {/* DYNAMIC SCHEMA-DRIVEN FIELDS */}
                <div className="pt-2 border-t border-slate-100 space-y-4">
                  <h4 className="text-xs font-bold uppercase tracking-wider text-slate-500">
                    Product-Specific Information
                  </h4>

                  {parsedSchema.map((field) => (
                    <div key={field.id}>
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        {field.label} {field.required && <span className="text-rose-500">*</span>}
                      </label>

                      {field.type === 'select' ? (
                        <select
                          required={field.required}
                          value={dynamicFormValues[field.id] || ''}
                          onChange={(e) => handleDynamicFieldChange(field.id, e.target.value)}
                          className="w-full px-3.5 py-2.5 border border-slate-300 rounded-lg text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-blue-600 text-sm"
                        >
                          {field.options?.map((opt) => (
                            <option key={opt} value={opt}>
                              {opt}
                            </option>
                          ))}
                        </select>
                      ) : field.type === 'number' ? (
                        <input
                          type="number"
                          required={field.required}
                          min={field.min}
                          max={field.max}
                          value={dynamicFormValues[field.id] ?? ''}
                          onChange={(e) => handleDynamicFieldChange(field.id, Number(e.target.value))}
                          className="w-full px-3.5 py-2.5 border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-600 text-sm"
                        />
                      ) : (
                        <input
                          type="text"
                          required={field.required}
                          value={dynamicFormValues[field.id] || ''}
                          onChange={(e) => handleDynamicFieldChange(field.id, e.target.value)}
                          className="w-full px-3.5 py-2.5 border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-blue-600 text-sm"
                        />
                      )}
                    </div>
                  ))}
                </div>

                <div className="pt-4 border-t border-slate-100 flex justify-end space-x-3">
                  <button
                    type="button"
                    onClick={() => setSelectedProduct(null)}
                    className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 text-sm font-medium"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={submitting}
                    className="inline-flex items-center px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium shadow-sm disabled:opacity-50"
                  >
                    {submitting ? (
                      <>
                        <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                        Submitting...
                      </>
                    ) : (
                      'Confirm & Submit Application'
                    )}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
