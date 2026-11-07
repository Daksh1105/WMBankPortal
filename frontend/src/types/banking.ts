export interface CustomerProfile {
  id: string
  userId: string
  firstName: string
  lastName: string
  phoneNumber: string
  dateOfBirth: string
  addressLine1: string
  city: string
  postalCode: string
  country: string
  updatedAt: string
}

export interface UpdateProfileRequest {
  firstName: string
  lastName: string
  phoneNumber: string
  dateOfBirth: string
  addressLine1: string
  city: string
  postalCode: string
  country: string
}

export interface FormFieldSchema {
  id: string
  label: string
  type: 'text' | 'number' | 'select'
  required: boolean
  min?: number
  max?: number
  options?: string[]
}

export interface BankingProduct {
  id: string
  code: string
  name: string
  category: string
  interestRate: number
  minAmount: number
  maxAmount: number
  dynamicFormSchemaJson: string
  isActive: boolean
}

export interface BankingApplication {
  id: string
  applicationNumber: string
  customerId: string
  productId: string
  productName: string
  productCategory: string
  requestedAmount: number
  status: 'Submitted' | 'UnderReview' | 'KycPending' | 'Approved' | 'Rejected' | 'Withdrawn'
  formValuesJson: string
  officerRemarks?: string
  submittedAt: string
  decidedAt?: string
  rowVersion: string
}

export interface ApplicationTimeline {
  id: string
  fromStatus: string
  toStatus: string
  changedByRole: string
  comments?: string
  timestamp: string
}

export interface CustomerDashboard {
  totalApplications: number
  pendingApplications: number
  approvedApplications: number
  rejectedApplications: number
  recentApplications: BankingApplication[]
}
