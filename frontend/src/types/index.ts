export type UserRole = 'Preparer' | 'Reviewer' | 'Manager'

export interface AuthUser {
  id: string
  name: string
  email: string
  tenantId: string
  role: UserRole
}

export interface TeamMember {
  userId: string
  name: string
  email: string
  role: UserRole
}

export interface TeamInvite {
  id: string
  email: string
  role: UserRole
  expiresAt: string
  createdAt: string
}

export type FormFieldType = 'Text' | 'Number' | 'Date' | 'Dropdown' | 'Checkbox'

export interface FormField {
  id: string
  label: string
  type: FormFieldType
  required: boolean
  placeholder?: string
  options?: string[]
}

export type WorkflowStatus = 'Draft' | 'Active' | 'Inactive' | 'Archived'
export type ExecutionStatus =
  | 'Pending' | 'Running' | 'WaitingApproval' | 'Approved'
  | 'Rejected' | 'Completed' | 'Failed' | 'Cancelled' | 'TimedOut'
export type StepType = 'Action' | 'Approval' | 'Condition' | 'Delay' | 'Notification' | 'DataTransform' | 'Webhook'
export type TriggerType = 'Manual' | 'Schedule' | 'Webhook' | 'Event' | 'Condition'

export interface WorkflowStep {
  id: string
  name: string
  type: StepType
  order: number
  config: Record<string, unknown>
  nextStepId: string | null
}

export interface Workflow {
  id: string
  name: string
  description: string
  status: WorkflowStatus
  tenantId: string
  version: number
  steps: WorkflowStep[]
  createdAt: string
  updatedAt: string
  formSchema: FormField[]
}

export interface WorkflowSummary {
  id: string
  name: string
  status: WorkflowStatus
  version: number
  stepCount: number
  executionCount: number
  updatedAt: string
  hasForm: boolean
}

export interface AutomationRule {
  id: string
  name: string
  description: string
  triggerType: TriggerType
  triggerConfig: string
  workflowId: string
  workflowName: string
  isEnabled: boolean
  tenantId: string
  executionCount: number
  lastTriggeredAt: string | null
  createdAt: string
  webhookToken: string | null
}

export interface ExecutionStep {
  id: string
  stepId: string
  stepName: string
  status: ExecutionStatus
  order: number
  output: string | null
  errorMessage: string | null
  startedAt: string | null
  completedAt: string | null
}

export interface ExecutionComment {
  id: string
  executionId: string
  authorId: string
  authorName: string
  content: string
  createdAt: string
}

export interface WorkflowExecution {
  id: string
  workflowId: string
  workflowName: string
  temporalWorkflowId: string
  temporalRunId: string
  status: ExecutionStatus
  tenantId: string
  triggeredBy: string | null
  inputData: Record<string, unknown>
  outputData: Record<string, unknown>
  errorMessage: string | null
  currentStepOrder: number
  currentStepName: string | null
  startedAt: string
  completedAt: string | null
  slaBreachedAt: string | null
  steps: ExecutionStep[]
  comments: ExecutionComment[]
  workflowVersionNumber: number
}

export interface WorkflowVersion {
  id: string
  versionNumber: number
  createdAt: string
  createdBy: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface DashboardStats {
  totalWorkflows: number
  activeWorkflows: number
  totalExecutions: number
  runningExecutions: number
  pendingApprovals: number
  failedExecutions: number
  completedToday: number
  recentTrend: Array<{ date: string; completed: number; failed: number }>
}
