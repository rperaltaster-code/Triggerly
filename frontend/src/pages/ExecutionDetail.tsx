import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, CheckCircle, XCircle, Clock, AlertTriangle } from 'lucide-react'
import { useExecution, useApproveExecution, useRejectExecution } from '../hooks/useExecutions'
import { Badge } from '../components/ui/Badge'
import { format } from 'date-fns'
import type { ExecutionStatus } from '../types'

const stepStatusIcon: Record<ExecutionStatus, React.ReactNode> = {
  Completed: <CheckCircle size={16} className="text-green-500" />,
  Failed: <AlertTriangle size={16} className="text-red-500" />,
  Running: <div className="w-4 h-4 rounded-full border-2 border-blue-500 border-t-transparent animate-spin" />,
  WaitingApproval: <Clock size={16} className="text-orange-500" />,
  Pending: <div className="w-4 h-4 rounded-full border-2 border-gray-300" />,
  Approved: <CheckCircle size={16} className="text-teal-500" />,
  Rejected: <XCircle size={16} className="text-red-500" />,
  Cancelled: <XCircle size={16} className="text-gray-400" />,
  TimedOut: <Clock size={16} className="text-purple-500" />,
}

export function ExecutionDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: execution, isLoading } = useExecution(id!)
  const approve = useApproveExecution()
  const reject = useRejectExecution()

  if (isLoading) return <div className="flex justify-center py-12"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" /></div>
  if (!execution) return <div className="text-center py-12 text-gray-500">Execution not found</div>

  return (
    <div className="space-y-6 max-w-4xl">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate(-1)} className="p-2 hover:bg-gray-100 rounded-lg">
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">{execution.workflowName}</h1>
            <Badge status={execution.status} />
          </div>
          <p className="text-gray-400 text-sm font-mono mt-0.5">{execution.temporalWorkflowId}</p>
        </div>
        {execution.status === 'WaitingApproval' && (
          <div className="flex gap-2">
            <button
              onClick={() => approve.mutate(execution.id)}
              className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm"
            >
              <CheckCircle size={15} /> Approve
            </button>
            <button
              onClick={() => {
                const reason = prompt('Reason for rejection:')
                if (reason) reject.mutate({ id: execution.id, reason })
              }}
              className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 text-sm"
            >
              <XCircle size={15} /> Reject
            </button>
          </div>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        {[
          { label: 'Started', value: format(new Date(execution.startedAt), 'MMM d, yyyy HH:mm:ss') },
          { label: 'Completed', value: execution.completedAt ? format(new Date(execution.completedAt), 'MMM d, yyyy HH:mm:ss') : '—' },
          { label: 'Triggered By', value: execution.triggeredBy ?? 'System' },
          { label: 'Current Step', value: execution.currentStepName ?? '—' },
        ].map(({ label, value }) => (
          <div key={label} className="bg-white border border-gray-200 rounded-lg p-4">
            <p className="text-xs text-gray-500">{label}</p>
            <p className="font-medium text-gray-900 mt-0.5">{value}</p>
          </div>
        ))}
      </div>

      {execution.errorMessage && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-sm font-medium text-red-700">Error</p>
          <p className="text-sm text-red-600 mt-1">{execution.errorMessage}</p>
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <h2 className="font-semibold text-gray-800 mb-4">Execution Steps</h2>
        {execution.steps.length === 0 ? (
          <p className="text-gray-400 text-sm">No step details available.</p>
        ) : (
          <div className="space-y-2">
            {execution.steps.map((step) => (
              <div key={step.id} className="flex items-center gap-4 p-3 bg-gray-50 rounded-lg">
                <div className="flex-shrink-0">{stepStatusIcon[step.status] ?? null}</div>
                <div className="flex-1">
                  <span className="font-medium text-sm text-gray-900">{step.stepName}</span>
                  {step.errorMessage && <p className="text-xs text-red-500 mt-0.5">{step.errorMessage}</p>}
                </div>
                <Badge status={step.status} />
              </div>
            ))}
          </div>
        )}
      </div>

      {Object.keys(execution.inputData).length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
          <h2 className="font-semibold text-gray-800 mb-3">Input Data</h2>
          <pre className="text-xs bg-gray-50 p-4 rounded-lg overflow-auto">
            {JSON.stringify(execution.inputData, null, 2)}
          </pre>
        </div>
      )}
    </div>
  )
}
