export type KycDocType = 'IdProof' | 'AddressProof' | 'Photo'
export type KycStatus = 'Pending' | 'Verified' | 'Rejected'
export type ScanStatus = 'Pending' | 'Clean' | 'Infected' | 'Failed'

export interface KycDocument {
  id: string
  applicationId: string
  documentType: KycDocType
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  status: KycStatus
  scanStatus: ScanStatus
  uploadedAt: string
  rejectionReason?: string
  version: number
}

export interface KycDownloadToken {
  downloadUrl: string
  expiresAtUtc: string
}
