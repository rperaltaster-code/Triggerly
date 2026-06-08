import { clsx } from 'clsx'
import type { WorkflowStatus, ExecutionStatus } from '../../types'

const statusColors: Record<string, string> = {
  Draft: 'bg-gray-100 text-gray-700',
  Active: 'bg-green-100 text-green-700',
  Inactive: 'bg-yellow-100 text-yellow-700',
  Archived: 'bg-red-100 text-red-700',
  Pending: 'bg-gray-100 text-gray-700',
  Running: 'bg-blue-100 text-blue-700',
  WaitingApproval: 'bg-orange-100 text-orange-700',
  Approved: 'bg-teal-100 text-teal-700',
  Rejected: 'bg-red-100 text-red-700',
  Completed: 'bg-green-100 text-green-700',
  Failed: 'bg-red-100 text-red-700',
  Cancelled: 'bg-gray-100 text-gray-600',
  TimedOut: 'bg-purple-100 text-purple-700',
}

interface BadgeProps {
  status: WorkflowStatus | ExecutionStatus | string
  className?: string
}

export function Badge({ status, className }: BadgeProps) {
  return (
    <span
      className={clsx(
        'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
        statusColors[status] ?? 'bg-gray-100 text-gray-700',
        className
      )}
    >
      {status}
    </span>
  )
}
