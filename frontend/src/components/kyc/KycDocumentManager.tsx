import React, { useState, useEffect, useRef } from 'react'
import { apiClient } from '../../api/client'
import type { KycDocument, KycDocType, KycDownloadToken } from '../../types/kyc'
import {
  UploadCloud,
  FileCheck,
  AlertCircle,
  Clock,
  CheckCircle2,
  XCircle,
  ExternalLink,
  Loader2,
  ShieldAlert,
  RotateCcw
} from 'lucide-react'

interface KycDocumentManagerProps {
  applicationId: string
  applicationNumber: string
  applicationStatus: string
  onUploadComplete?: () => void
}

export const KycDocumentManager: React.FC<KycDocumentManagerProps> = ({
  applicationId,
  applicationNumber,
  applicationStatus,
  onUploadComplete,
}) => {
  const [documents, setDocuments] = useState<KycDocument[]>([])
  const [loading, setLoading] = useState(true)
  const [uploadingType, setUploadingType] = useState<KycDocType | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [previewingId, setPreviewingId] = useState<string | null>(null)

  const fileInputRef = useRef<HTMLInputElement>(null)
  const [currentSelectedType, setCurrentSelectedType] = useState<KycDocType>('IdProof')

  useEffect(() => {
    fetchDocuments()
  }, [applicationId])

  const fetchDocuments = async () => {
    try {
      setLoading(true)
      const res = await apiClient.get<KycDocument[]>(`/api/v1/kyc/application/${applicationId}`)
      setDocuments(res.data)
    } catch {
      setError('Unable to load KYC documentation.')
    } finally {
      setLoading(false)
    }
  }

  const handleSelectFileClick = (type: KycDocType) => {
    setCurrentSelectedType(type)
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
      fileInputRef.current.click()
    }
  }

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setError(null)
    setSuccess(null)

    // Client-side validations
    const allowed = ['application/pdf', 'image/jpeg', 'image/png']
    if (!allowed.includes(file.type)) {
      setError('Invalid file type. Only PDF, JPG, and PNG documents are allowed.')
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setError('File size exceeds the 5MB limit.')
      return
    }

    try {
      setUploadingType(currentSelectedType)
      const formData = new FormData()
      formData.append('file', file)
      formData.append('applicationId', applicationId)
      formData.append('documentType', currentSelectedType)

      await apiClient.post('/api/v1/kyc/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })

      setSuccess(`Successfully uploaded ${getDocumentTypeLabel(currentSelectedType)}!`)
      await fetchDocuments()
      if (onUploadComplete) {
        onUploadComplete()
      }
    } catch (err: any) {
      const msg = err.response?.data?.detail || err.response?.data?.title || 'Failed to upload KYC document.'
      setError(msg)
    } finally {
      setUploadingType(null)
    }
  }

  const handleSecurePreview = async (docId: string) => {
    try {
      setPreviewingId(docId)
      const res = await apiClient.get<KycDownloadToken>(`/api/v1/kyc/${docId}/download-token`)
      window.open(res.data.downloadUrl, '_blank', 'noopener,noreferrer')
    } catch (err: any) {
      alert(err.response?.data?.detail || 'Unable to generate secure download SAS token.')
    } finally {
      setPreviewingId(null)
    }
  }

  const getDocumentTypeLabel = (type: KycDocType) => {
    switch (type) {
      case 'IdProof':
        return 'Government ID Proof (Passport, Aadhaar, Voter ID)'
      case 'AddressProof':
        return 'Proof of Address (Utility Bill, Rental Agreement)'
      case 'Photo':
        return 'Passport Size Photograph'
    }
  }

  const getStatusBadge = (doc: KycDocument) => {
    switch (doc.status) {
      case 'Verified':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800">
            <CheckCircle2 className="w-3.5 h-3.5 mr-1 text-emerald-600" />
            Verified
          </span>
        )
      case 'Rejected':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-100 text-rose-800">
            <XCircle className="w-3.5 h-3.5 mr-1 text-rose-600" />
            Rejected
          </span>
        )
      default:
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800">
            <Clock className="w-3.5 h-3.5 mr-1 text-amber-600" />
            Pending Verification
          </span>
        )
    }
  }

  const documentSlots: KycDocType[] = ['IdProof', 'AddressProof', 'Photo']

  const isClosed = ['Approved', 'Rejected', 'Withdrawn'].includes(applicationStatus)

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <Loader2 className="w-6 h-6 text-blue-600 animate-spin" />
        <span className="ml-2 text-xs text-slate-500">Loading KYC records...</span>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <input
        ref={fileInputRef}
        type="file"
        accept=".pdf,.jpg,.jpeg,.png"
        className="hidden"
        onChange={handleFileChange}
      />

      <div className="border-b border-slate-100 pb-4">
        <h4 className="text-base font-bold text-slate-900">Required KYC Documentation</h4>
        <p className="text-xs text-slate-500 mt-0.5">
          Attach identity and verification documents for Application{' '}
          <span className="font-mono font-semibold text-blue-600">{applicationNumber}</span>.
        </p>
      </div>

      {applicationStatus === 'KycPending' && (
        <div className="p-4 bg-purple-50 border border-purple-200 rounded-xl flex items-start space-x-3 text-purple-900 text-xs">
          <ShieldAlert className="w-5 h-5 text-purple-600 flex-shrink-0 mt-0.5" />
          <div>
            <div className="font-bold">Underwriting Officer Requested Document Re-Upload</div>
            <div className="mt-1 text-purple-700">
              Please re-upload the rejected or requested documents below. Once uploaded, your application will
              automatically transition back to Under Review.
            </div>
          </div>
        </div>
      )}

      {success && (
        <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-lg flex items-center space-x-2 text-emerald-800 text-xs">
          <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0" />
          <span>{success}</span>
        </div>
      )}

      {error && (
        <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg flex items-center space-x-2 text-rose-800 text-xs">
          <AlertCircle className="w-4 h-4 text-rose-600 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <div className="grid grid-cols-1 gap-4">
        {documentSlots.map((slotType) => {
          const doc = documents.find((d) => d.documentType === slotType)
          const isUploading = uploadingType === slotType

          return (
            <div
              key={slotType}
              className={`p-4 rounded-xl border transition-all ${
                doc?.status === 'Verified'
                  ? 'border-emerald-200 bg-emerald-50/20'
                  : doc?.status === 'Rejected'
                  ? 'border-rose-200 bg-rose-50/20'
                  : doc
                  ? 'border-slate-200 bg-slate-50/50'
                  : 'border-dashed border-slate-300 bg-white hover:border-blue-400'
              }`}
            >
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                <div className="flex items-start space-x-3">
                  <div
                    className={`p-2.5 rounded-lg ${
                      doc?.status === 'Verified'
                        ? 'bg-emerald-100 text-emerald-700'
                        : doc?.status === 'Rejected'
                        ? 'bg-rose-100 text-rose-700'
                        : doc
                        ? 'bg-blue-100 text-blue-700'
                        : 'bg-slate-100 text-slate-500'
                    }`}
                  >
                    {doc ? <FileCheck className="w-5 h-5" /> : <UploadCloud className="w-5 h-5" />}
                  </div>

                  <div>
                    <div className="text-sm font-bold text-slate-900">{getDocumentTypeLabel(slotType)}</div>

                    {doc ? (
                      <div className="mt-1 space-y-1">
                        <div className="flex items-center space-x-2 text-xs text-slate-500">
                          <span className="font-mono text-slate-700">{doc.originalFileName}</span>
                          <span>•</span>
                          <span>{(doc.fileSizeBytes / 1024).toFixed(0)} KB</span>
                          <span>•</span>
                          <span>Version {doc.version}</span>
                        </div>

                        {doc.rejectionReason && (
                          <div className="text-xs text-rose-700 font-medium">
                            Remarks: "{doc.rejectionReason}"
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="text-xs text-slate-400 mt-0.5">
                        Required • PDF, JPG, PNG up to 5MB (magic-byte scanned)
                      </div>
                    )}
                  </div>
                </div>

                <div className="flex items-center space-x-2 self-end sm:self-center">
                  {doc && getStatusBadge(doc)}

                  {doc && (
                    <button
                      onClick={() => handleSecurePreview(doc.id)}
                      disabled={previewingId === doc.id}
                      className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-blue-700 bg-blue-50 border border-blue-200 rounded-lg hover:bg-blue-100 transition-colors disabled:opacity-50"
                      title="Download with 5-minute SAS Token"
                    >
                      {previewingId === doc.id ? (
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                      ) : (
                        <>
                          <ExternalLink className="w-3.5 h-3.5 mr-1" />
                          Secure Preview
                        </>
                      )}
                    </button>
                  )}

                  {!isClosed && (!doc || doc.status === 'Rejected') && (
                    <button
                      onClick={() => handleSelectFileClick(slotType)}
                      disabled={isUploading}
                      className={`inline-flex items-center px-3.5 py-1.5 rounded-lg text-xs font-semibold shadow-xs transition-colors cursor-pointer ${
                        doc?.status === 'Rejected'
                          ? 'bg-rose-600 hover:bg-rose-700 text-white'
                          : 'bg-blue-600 hover:bg-blue-700 text-white'
                      } disabled:opacity-50`}
                    >
                      {isUploading ? (
                        <>
                          <Loader2 className="w-3.5 h-3.5 mr-1 animate-spin" />
                          Uploading...
                        </>
                      ) : doc?.status === 'Rejected' ? (
                        <>
                          <RotateCcw className="w-3.5 h-3.5 mr-1" />
                          Re-Upload
                        </>
                      ) : (
                        <>
                          <UploadCloud className="w-3.5 h-3.5 mr-1" />
                          Upload File
                        </>
                      )}
                    </button>
                  )}
                </div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
